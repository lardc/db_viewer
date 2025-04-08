using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using SCME.InterfaceImplementations;
using SCME.ProfileBuilder.CustomControl;
using SCME.ProfileBuilder.PagesTech;
using SCME.Types.DatabaseServer;

namespace SCME.ProfileBuilder
{
    /// <summary>
    /// Interaction logic for ConnectPage.xaml
    /// </summary>
    public partial class ConnectPage : Page
    {
        private const string DBSettings = "synchronous=Full;journal mode=Truncate;failifmissing=True";

        private DispatcherTimer m_NetPingTimer;
        private CentralDatabaseServiceClient m_Client;

        public ConnectPage()
        {
            InitializeComponent();
        }

        private void ButtonSQLConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tbDBName.Text) && !string.IsNullOrEmpty(tbServerName.Text))
            {
                try
                {
                    var connectionString = (chkDbIntSec.IsChecked != null && (bool) chkDbIntSec.IsChecked) 
                        ? $"Server={tbServerName.Text}; Database={tbDBName.Text}; Integrated Security=true;"
                        : $"Server={tbServerName.Text}; Database={tbDBName.Text}; User Id={tbDBUser.Text}; Password={tbDBPassword.Text};";

                    IProfilesService service = new SQLProfilesService(connectionString);
                    IProfilesConnectionService connectionService = new SQLProfilesConnectionService(connectionString, service);

                    Cache.ProfileEdit = new ProfilePage(service);
                    Cache.ConnectionsPage = new Connections(connectionService);

                    NavigationService?.Navigate(Cache.ProfileEdit);
                }
                catch (Exception ex)
                {
                    var dw = new DialogWindow("Error", ex.Message);
                    dw.ButtonConfig(DialogWindow.EbConfig.OK);
                    dw.ShowDialog();
                }
            }
        }

        private void ButtonSQliteBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "SQLite database|*.sqlite",
                FilterIndex = 1
            };

            if (dlg.ShowDialog() != true)
                return;
            
            // Open document
            var filename = dlg.FileName;
            tbDbPath.Text = filename;
        }

        private void ButtonSQliteConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tbDbPath.Text))
            {
                try
                {
                    var connString = $"data source={tbDbPath.Text};{DBSettings}";

                    IProfilesService service = new SQLiteProfilesService(connString);
                    IProfilesConnectionService connectionService = new SQLiteProfilesConnectionService(connString);

                    Cache.ProfileEdit = new ProfilePage(service);
                    Cache.ConnectionsPage = new Connections(connectionService);

                    NavigationService?.Navigate(Cache.ProfileEdit);
                }
                catch (Exception ex)
                {
                    var dw = new DialogWindow("Error", ex.Message);
                    dw.ButtonConfig(DialogWindow.EbConfig.OK);
                    dw.ShowDialog();
                }
            }
        }

        private void ButtonWcfConnect_OnClick(object sender, RoutedEventArgs e)
        {
            m_Client = new CentralDatabaseServiceClient();

            Cache.ProfileEdit = new ProfilePage(m_Client);
            Cache.ConnectionsPage = new Connections(m_Client);

            NavigationService?.Navigate(Cache.ProfileEdit);

            m_NetPingTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 10) };
            m_NetPingTimer.Tick += NetPingTimerOnTick;
            m_NetPingTimer.Start();
        }

        private void NetPingTimerOnTick(object Sender, EventArgs Args)
        {
            try
            {
                m_Client.Check();
            }
            catch (Exception)
            {
            }
        }
    }
}
