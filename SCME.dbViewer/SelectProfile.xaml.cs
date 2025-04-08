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
using System.Windows.Threading;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for SelectProfile.xaml
    /// </summary>
    public partial class SelectProfile : Window
    {
        public SelectProfile()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        //индекс последней выделенной строки в dgProfiles. потребовался, т.к. после исполнения this.ShowDialog() методы dgProfiles.CurrentCell и dgProfiles.CurrentItem возвращают null 
        private int FSelectedProfileID = -1;
        public int SelectedProfileID
        {
            get { return FSelectedProfileID; }

        }

        private string FSelectedProfile = null;
        public string SelectedProfile
        {
            get { return FSelectedProfile; }
        }

        private string FSelectedProfileGUID = null;
        public string SelectedProfileGUID
        {
            get { return FSelectedProfileGUID; }
        }

        private void LoadData()
        {
            //грузим не удалённые профили последних версий
            string sqlText = @"SELECT x.PROF_ID, x.PROF_NAME, x.PROF_GUID, x.PROF_TS, x.PROF_VERS
                               FROM
                                    (
                                     SELECT PROF_ID, PROF_NAME, PROF_GUID, PROF_TS, PROF_VERS, ROW_NUMBER() OVER(PARTITION BY PROF_NAME ORDER BY PROF_VERS DESC) AS RN
                                     FROM PROFILES WITH (NOLOCK)
                                     WHERE (ISNULL(IS_DELETED, 0)=0)
                                    ) x
                               WHERE (x.RN=1)";

            dgProfiles.ViewSqlResultByThread(sqlText);
        }

        public bool ShowModal(string title, ref int? selectedProfileID, out string selectedProfile, out string selectedProfileGUID)
        {
            this.Title = title;
            this.LoadData();

            //устанавливаем курсор на строку с принятым selectedGroupName
            if (selectedProfileID != null)
            {
                //после исполнения SQL запроса должно быть установлено dgProfiles.ItemsSource
                //ожидаем установку dgProfiles.ItemsSource
                while (dgProfiles.ItemsSource == null)
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

                DataView dv = dgProfiles.ItemsSource as DataView;
                DataTable dt = dv.Table;

                if (dt != null)
                {
                    int profileID = (int)selectedProfileID;

                    DataRowView row = dt.DefaultView.OfType<DataRowView>()
                                      .Where(x => x.Row.Field<int>("PROF_ID") == profileID)
                                      .FirstOrDefault();

                    if (row != null)
                    {
                        dgProfiles.SelectedItem = row;
                        dgProfiles.CurrentItem = row;
                    }
                }
            }

            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                selectedProfileID = this.SelectedProfileID;
                selectedProfile = this.SelectedProfile;
                selectedProfileGUID = this.SelectedProfileGUID;

                return true;
            }
            else
            {
                selectedProfileID = null;
                selectedProfile = null;
                selectedProfileGUID = null;

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

        private void dgProfiles_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            this.SetSelectedProfile();
        }

        private void dgProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetSelectedProfile();
        }

        private void dgProfiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock)
            {
                e.Handled = true;
                this.DialogResult = (this.SelectedProfile == null) ? null : (bool?)true;
            }
        }

        private void dgProfiles_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void SetSelectedProfile()
        {
            //запоминаем индекс последней выбранной пользователем строки
            DataRowView currentItem = dgProfiles.CurrentItem as DataRowView;

            if (currentItem != null)
            {
                object[] itemArray = currentItem.Row.ItemArray;

                int columnIndex = currentItem.Row.Table.Columns.IndexOf("PROF_ID");
                this.FSelectedProfileID = Convert.ToInt32(itemArray[columnIndex]);

                columnIndex = currentItem.Row.Table.Columns.IndexOf(Common.Constants.ProfileName);
                this.FSelectedProfile = itemArray[columnIndex].ToString();

                columnIndex = currentItem.Row.Table.Columns.IndexOf("PROF_GUID");
                this.FSelectedProfileGUID = itemArray[columnIndex].ToString();
            }
        }
    }
}
