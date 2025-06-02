using SCME.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SCME.Linker
{
    /// <summary>
    /// Interaction logic for Jobs.xaml
    /// </summary>
    public partial class Jobs : Window
    {
        public Jobs()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        //индекс последней выделенной строки в dgJobs. потребовался, т.к. после исполнения this.ShowDialog() методы dgJobs.CurrentCell и dgJobs.CurrentItem возвращают null 
        private string FSelectedGroupName = null;
        public string SelectedGroupName
        {
            get { return FSelectedGroupName; }
            set { FSelectedGroupName = value; }
        }

        private void LoadData()
        {
            string sqlText = @"SELECT RTRIM(GROUP_NAME) AS GROUP_NAME, MAX(TS) AS LASTTS, COUNT(*) AS LINKSCOUNT
                               FROM ASSEMBLYS WITH (NOLOCK)
                               GROUP BY GROUP_NAME
                               ORDER BY MAX(TS) DESC";

            dgJobs.ViewSqlResultByThread(sqlText);
        }

        public bool ShowModal(out string selectedGroupName)
        {
            this.LoadData();
            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                selectedGroupName = this.SelectedGroupName;

                return true;
            }
            else
            {
                selectedGroupName = null;

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

                    if (this.SelectedGroupName == null)
                    {
                        this.btManualEntryGroupName.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    }
                    else
                        this.DialogResult = true;

                    break;
            }
        }

        private void dgJobs_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            this.SetSelectedGroupName();
        }

        private void dgJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetSelectedGroupName();
        }

        private void dgJobs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = (this.SelectedGroupName == null) ? null : (bool?)true;
        }

        private void dgJobs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void btManualEntryGroupName_Click(object sender, RoutedEventArgs e)
        {
            string selectedGroupName;
            DialogInputString dialogInputString = new DialogInputString();

            if (dialogInputString.ShowModal(Routines.CheckGroupNameByMask, Properties.Resources.GroupName, null, out selectedGroupName) == true)
            {
                this.SelectedGroupName = selectedGroupName;
                this.DialogResult = true;
            }
            else this.SelectedGroupName = null;
        }

        private void SetSelectedGroupName()
        {
            //запоминаем индекс последней выбранной пользователем строки
            DataRowView currentItem = dgJobs.CurrentItem as DataRowView;

            if (currentItem != null)
            {
                object[] itemArray = currentItem.Row.ItemArray;
                int columnIndex = currentItem.Row.Table.Columns.IndexOf(Common.Constants.GroupName);
                this.SelectedGroupName = itemArray[columnIndex].ToString();
            }
        }
    }
}
