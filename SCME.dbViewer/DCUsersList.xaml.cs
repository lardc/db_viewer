using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;
using SCME.Types;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for DCUsersList.xaml
    /// </summary>
    public partial class DCUsersList : Window
    {
        public DCUsersList()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        public DataView ListDCUsers
        {
            get
            {
                DataTable dt = new DataTable();
                SqlConnection connection = DBConnections.ConnectionDC;
                connection.Open();

                try
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    string sqlText = "SELECT USERLOGIN, ID, LASTNAME, FIRSTNAME, MIDDLENAME" +
                                     " FROM RUSDC_USERS WITH(NOLOCK)" +
                                     " ORDER BY LASTNAME, FIRSTNAME, MIDDLENAME";
                    adapter.SelectCommand = new SqlCommand(sqlText, connection);
                    adapter.Fill(dt);
                }

                finally
                {
                    connection.Close();
                }

                return dt.DefaultView;
            }
        }

        public bool? ShowModal(out string managedTabNum, out long managedUserID, out ulong managedPermissionsLo)
        {
            //демонстрация списка пользователей системы DC, пользователи из этого списка могут быть или не быть пользователями данного приложения
            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                //пользователь системы DC выбран из списка
                managedTabNum = this.dgDCUsers.ValueFromSelectedRow("USERLOGIN").ToString();
                managedUserID = Convert.ToInt64(this.dgDCUsers.ValueFromSelectedRow("ID"));

                //считываем битовую маску прав доступа для выбранного пользователя
                DbRoutines.UserPermissions(managedUserID, out managedPermissionsLo);
            }
            else
            {
                //пользователь системы DC не выбран
                managedTabNum = null;
                managedUserID = -1;
                managedPermissionsLo = 0;
            }

            return result;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.DialogResult = false;
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void dgDCUsers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
