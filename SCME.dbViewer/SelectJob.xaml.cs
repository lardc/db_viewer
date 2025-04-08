using SCME.CustomControls;
using SCME.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for SelectJob.xaml
    /// </summary>
    public partial class SelectJob : Window
    {
        public SelectJob()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        private string FSelectedGroupName = null;
        public string SelectedGroupName
        {
            get { return FSelectedGroupName; }
            set { FSelectedGroupName = value.TrimEnd(); }
        }

        private string FSelectedItem = null;
        public string SelectedItem
        {
            get { return FSelectedItem; }
            set { FSelectedItem = value.TrimEnd(); }
        }


        private void LoadData(string jobPrefix)
        {
            string sqlText = string.Format(@"SELECT CONCAT(JOB, '-', RIGHT(CONCAT('0000', CAST(SUFFIX AS VARCHAR(4))), 4)) AS GROUP_NAME, CREATEDATE, ITEM
                                             FROM JOB_MST WITH (NOLOCK)
                                             WHERE (
                                                    (TYPE='J') AND
                                                    (STAT='C') AND
                                                    (JOB LIKE '{0}-%')
                                                   )
                                             ORDER BY CREATEDATE DESC, JOB, SUFFIX", jobPrefix);

            dgGroupNames.ViewSqlResultByThread(sqlText, DBConnections.ConnectionSL);
        }

        public bool ShowModal(string jobPrefix, ref string selectedGroupName, out int? selectedGroupID, out string selectedItem)
        {
            this.LoadData(jobPrefix);

            //устанавливаем курсор на строку с принятым selectedGroupName
            if (selectedGroupName != null)
            {
                //после исполнения SQL запроса должно быть установлено dgGroupNames.ItemsSource
                //ожидаем установку dgGroupNames.ItemsSource
                while (dgGroupNames.ItemsSource == null)
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

                DataView dv = dgGroupNames.ItemsSource as DataView;
                DataTable dt = dv.Table;

                if (dt != null)
                {
                    string groupName = selectedGroupName;

                    DataRowView row = dt.DefaultView.OfType<DataRowView>()
                                      .Where(x => x.Row.Field<string>(Common.Constants.GroupName).Contains(groupName))
                                      .FirstOrDefault();

                    if (row != null)
                    {
                        dgGroupNames.SelectedItem = row;
                        dgGroupNames.CurrentItem = row;
                    }
                }
            }

            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                selectedGroupName = this.SelectedGroupName;

                //вычисляем идентификатор GROUP_ID в центральной базе данных КИП СПП по выбранному из SL selectedGroupName
                selectedGroupID = DbRoutines.GroupIDByGroupName(selectedGroupName);

                selectedItem = this.SelectedItem;

                return true;
            }
            else
            {
                selectedGroupName = null;
                selectedGroupID = null;
                selectedItem = null;

                return false;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    this.DialogResult = false;
                    break;

                case Key.Enter:
                    e.Handled = true;
                    this.DialogResult = true;

                    break;
            }
        }

        private void dgGroupNames_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            this.SetSelectedData();
        }

        private void dgGroupNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetSelectedData();
        }

        private void dgGroupNames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock)
            {
                e.Handled = true;
                this.DialogResult = (this.SelectedGroupName == null) ? null : (bool?)true;
            }
        }

        private void dgGroupNames_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void SetSelectedData()
        {
            //запоминаем индекс последней выбранной пользователем строки
            if (dgGroupNames.CurrentItem is DataRowView currentItem)
            {
                object[] itemArray = currentItem.Row.ItemArray;
                int columnIndex = currentItem.Row.Table.Columns.IndexOf(Common.Constants.GroupName);
                this.SelectedGroupName = itemArray[columnIndex].ToString();

                columnIndex = currentItem.Row.Table.Columns.IndexOf(Common.Constants.Item);
                this.SelectedItem = itemArray[columnIndex].ToString();
            }
        }

        private DataGridColumnHeader GetColumnHeader(DataGridColumn column, DependencyObject reference)
        {
            //вычисляем DataGridColumnHeader по принятому column
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(reference); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(reference, i);

                if ((child is DataGridColumnHeader columnHeader) && (columnHeader.Column == column))
                    return columnHeader;

                columnHeader = this.GetColumnHeader(column, child);

                if (columnHeader != null)
                    return columnHeader;
            }

            return null;
        }

        private void DgGroupNames_Loaded(object sender, RoutedEventArgs e)
        {
            //событие Loaded используем для того, чтобы dg на момент вызова уже был созданным и VisualTreeHelper.GetChildrenCount(dg) быле не равным нулю
            //из базы данных считывается набор данных отсортированных в обратном порядке по полю "CREATEDATE" - показываем это пользователю (столбец и направление сортировки загруженных в dgGroupNames данных)
            if (sender is DataGrid dg)
            {
                DataGridColumn column = dg.Columns.FirstOrDefault(c => ((Binding)((DataGridBoundColumn)c).Binding).Path.Path == "CREATEDATE");

                if (column != null)
                {
                    DataGridColumnHeader columnHeader = this.GetColumnHeader(column, dg);

                    if (columnHeader != null)
                        dgGroupNames.ShowSortDirectionIndicator(columnHeader, System.ComponentModel.ListSortDirection.Descending);
                }
            }
        }
    }
}