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
    /// Interaction logic for Packages.xaml
    /// </summary>
    public partial class Packages : Window
    {
        public Packages()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        //индекс последней выделенной строки в dgPackages. потребовался, т.к. после исполнения this.ShowDialog() методы dgPackages.CurrentCell и dgPackages.CurrentItem возвращают null 
        private string FSelectedGroupName = null;
        public string SelectedGroupName
        {
            get { return FSelectedGroupName; }
            set { FSelectedGroupName = value; }
        }

        private void LoadData()
        {
            string sqlText = "SELECT P.SERIALNUM," +
                             "       CASE" +
                             "           WHEN A.DEV_ID=1 THEN A.DEVICECODE" +
                             "           ELSE D.CODE" +
                             "       END AS DEVICECODE," +
                             "       CASE" +
                             "           WHEN A.DEV_ID2=1 THEN A.DEVICECODE2" +
                             "           ELSE D2.CODE" +
                             "       END AS DEVICECODE2," +
                             "       RTRIM(A.GROUP_NAME) AS GROUP_NAME, A.TS, CONCAT(DCU.LASTNAME, ' ', LEFT(DCU.FIRSTNAME, 1), '. ', LEFT(DCU.MIDDLENAME, 1), '.') AS USR" +
                             " FROM ASSEMBLYS A" +
                             "  INNER JOIN PACKAGES P ON (A.PACKAGEID=P.PACKAGEID)" +
                             "  LEFT JOIN DEVICES D ON (A.DEV_ID=D.DEV_ID)" +
                             "  LEFT JOIN DEVICES D2 ON (A.DEV_ID2=D2.DEV_ID)" +
                             "  INNER JOIN [sa-011].[SL_PE_DC20002].[dbo].[RUSDC_Users] DCU ON (A.USR=DCU.USERLOGIN)";

            dgPackages.ViewSqlResultByThread(sqlText);
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
                    this.DialogResult = true;
                    break;
            }
        }

        private void dgPackages_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = (this.SelectedGroupName == null) ? null : (bool?)true;
        }

        private void SetSelectedGroupName()
        {
            //запоминаем индекс последней выбранной пользователем строки
            DataRowView currentItem = dgPackages.CurrentItem as DataRowView;

            if (currentItem != null)
            {
                object[] itemArray = currentItem.Row.ItemArray;
                int columnIndex = currentItem.Row.Table.Columns.IndexOf(Common.Constants.GroupName);
                this.SelectedGroupName = itemArray[columnIndex].ToString();
            }
        }

        private void dgPackages_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            this.SetSelectedGroupName();
        }

        private void dgPackages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetSelectedGroupName();
        }
    }
}
