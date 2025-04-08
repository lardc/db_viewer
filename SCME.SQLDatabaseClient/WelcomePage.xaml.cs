using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SCME.SQLDatabaseClient.Annotations;
using SCME.SQLDatabaseClient.EntityAccounts;
using SCME.SQLDatabaseClient.Properties;

namespace SCME.SQLDatabaseClient
{
    public enum ConnectionPageState
    {
        Waiting,
        Success,
        Fail,
        InvalidPassword
    }

    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage : Page, INotifyPropertyChanged
    {
        private bool _connected;
        private IList<ACCOUNT> _accountList;
        private ConnectionPageState _state = ConnectionPageState.Waiting;
        private string _stateText = "Подключение...";
        private bool _viewEnabed;
        private bool _editEnabed;
        private bool _reportEnabed;
        private ACCOUNT _selectedAccount;

        public bool Connected   
        {
            get { return _connected; }
            private set
            {
                _connected = value;
                OnPropertyChanged();
            }
        }

        public IList<ACCOUNT> AccountList
        {
            get { return _accountList; }
            private set
            {
                _accountList = value;
                OnPropertyChanged();
            }
        }

        public string StateText
        {
            get { return _stateText; }
            private set
            {
                _stateText = value;
                OnPropertyChanged();
            }
        }

        public ConnectionPageState State
        {
            get { return _state; }
            private set
            {
                _state = value;
                OnPropertyChanged();
            }
        }

        public ACCOUNT SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                _selectedAccount = value;

                if (value != null)
                {
                    ViewEnabled = true;
                    EditEnabled = value.ACC_SECURITY == Security.Edit;
                    ReportEnabled = (value.ACC_SECURITY == Security.Edit) || (value.ACC_SECURITY == Security.Report);
                }
                else
                {
                    ViewEnabled = false;
                    EditEnabled = false;
                    ReportEnabled = false;
                }
            }
        }

        public bool ViewEnabled
        {
            get { return _viewEnabed; }
            private set
            {
                _viewEnabed = value;
                OnPropertyChanged();
            }
        }

        public bool EditEnabled
        {
            get { return _editEnabed; }
            private set
            {
                _editEnabed = value;
                OnPropertyChanged();
            }
        }

        public bool ReportEnabled
        {
            get { return _reportEnabed; }
            private set
            {
                _reportEnabed = value;
                OnPropertyChanged();
            }
        }

        public WelcomePage()
        {
            InitializeComponent();
        }
        
        private static string BuildEntityConnectionString()
        {
            const string providerName = "System.Data.SqlClient";
            const string metadataEF =
                @"res://*/EntityAccounts.ModelAccounts.csdl|res://*/EntityAccounts.ModelAccounts.ssdl|res://*/EntityAccounts.ModelAccounts.msl";

            var sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Settings.Default.DbPath,
                InitialCatalog = Settings.Default.DBName,
                IntegratedSecurity = Settings.Default.DBIntegratedSecurity
            };

            if (!Settings.Default.DBIntegratedSecurity)
            {
                sqlBuilder.UserID = Settings.Default.DBUser;
                sqlBuilder.Password = Settings.Default.DBPassword;
            }

            var entityBuilder = new EntityConnectionStringBuilder
            {
                Provider = providerName,
                ProviderConnectionString = sqlBuilder.ToString(),
                Metadata = metadataEF
            };

            return entityBuilder.ToString();
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private async void WelcomePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Connected)
            {
                pwdPassword.Password = "";
                Cache.Main.WindowState = WindowState.Normal;
                return;
            }

            try
            {
                await Cache.ViewData.InitDataConnection();

                var db = new EntityAccounts.Entities(BuildEntityConnectionString());

                AccountList = await Task.Factory.StartNew<IList<ACCOUNT>>(() => (from acc in db.ACCOUNTS
                    orderby acc.ACC_NAME
                    select acc).ToList());

                SetState(ConnectionPageState.Success);
                Connected = true;
            }
            catch (Exception ex)
            {
                SetState(ConnectionPageState.Fail, ex);
                Connected = false;
            }
        }

        private void LbbView_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CheckPassword())
            {
                Cache.ViewData.IsEditEnabled = false;
                Cache.ViewData.IsReportEnabled = false;
                Cache.Main.mainFrame.Navigate(Cache.ViewData);
            }
        }

        private void LbbEdit_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(CheckPassword())
            {
                Cache.ViewData.IsEditEnabled = true;
                Cache.ViewData.IsReportEnabled = false;
                Cache.Main.mainFrame.Navigate(Cache.ViewData);
            }
        }
        private void LbbReport_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CheckPassword())
            {
                Cache.ViewData.IsEditEnabled = false;
                Cache.ViewData.IsReportEnabled = true;
                Cache.Main.mainFrame.Navigate(Cache.ViewData);
            }
        }

        private bool CheckPassword()
        {
            if (SelectedAccount != null)
            {
                if (SelectedAccount.ACC_PWD.Trim() == pwdPassword.Password)
                {
                    SetState(ConnectionPageState.Success);
                    return true;
                }

                SetState(ConnectionPageState.InvalidPassword);
            }

            return false;
        }

        private void SetState(ConnectionPageState state, object param = null)
        {
            State = state;

            switch (state)
            {
                case ConnectionPageState.Waiting:
                    StateText = "Подключение...";
                    break;
                case ConnectionPageState.Success:
                    StateText = "Подключено";
                    break;
                case ConnectionPageState.Fail:
                    StateText = $"Ошибка: {(param as Exception)?.Message}";
                    break;
                case ConnectionPageState.InvalidPassword:
                    StateText = "Неверный пароль";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
