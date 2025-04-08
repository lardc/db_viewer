using SCME.Linker.Properties;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using SCME.Types;
using System.Windows.Controls.Primitives;

namespace SCME.Linker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IInputElement LastInputElement = null;

        //табельный номер, идентификатор аутентифицированного в данном приложении пользователя и битовая маска его разрешений
        public string FTabNum = null;
        public long FUserID = -1;
        public ulong FPermissionsLo = 0;

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Localization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Localization error");
            }

            InitializeComponent();

            tb_User.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\')[1].ToString();
        }

        static void DispatcherUnhandledException(object Sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs E)
        {
            MessageBox.Show(E.Exception.ToString(), "Unhandled exception");
            Application.Current.Shutdown();
        }

        private static void CurrentDomainOnUnhandledException(object Sender, UnhandledExceptionEventArgs Args)
        {
            MessageBox.Show(Args.ExceptionObject.ToString(), "Unhandled exception");
            Application.Current.Shutdown();
        }

        private void Work()
        {
            SelectList selectList = new SelectList();
            selectList.ShowModal();
        }

        private void btAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            //проверяем имеет ли пользователь возможность работы с данной системой
            bool? userOK = this.IsUserOK(tb_User.Text, pbPassword.Password, out this.FTabNum, out this.FUserID, out this.FPermissionsLo);

            if (userOK == true)
            {
                //пользователь имеет возможность работы с данной системой - прячем this и открываем форму для создания связи между серийным номером ППЭ и серийным номером корпуса
                this.FTabNum = tb_User.Text;
                tb_User.Clear();
                pbPassword.Clear();

                this.Visibility = Visibility.Hidden;

                try
                {
                    this.Work();
                }
                finally
                {
                    //раз пользователь закрыл форму для просмотра связей между элементами ППЭ и корпусами - значит он хочет завершить свой сеанс работы с приложением, приложение оставляем работающим                                        
                    this.FTabNum = null;
                    this.Visibility = Visibility.Visible;
                    FocusManager.SetFocusedElement(this, tb_User);
                }
            }
            else
            {
                if ((userOK == null) && (this.LastInputElement != null))
                    FocusManager.SetFocusedElement(this, this.LastInputElement);
            }
        }

        private bool? IsUserOK(string name, string userPassword, out string tabNum, out long userID, out ulong permissionsLo)
        {
            //проверяем имеет ли пользователь регистрацию в системе DC
            long dcUserID = DbRoutines.CheckDCUserExist(name, userPassword);

            switch (dcUserID)
            {
                case -1:
                    //введённый пароль неверен, либо пользователя с именем userName не существует;
                    MessageBox.Show(string.Format(Properties.Resources.PasswordIsIncorrect, name), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    tabNum = null;
                    userID = -1;
                    permissionsLo = 0;

                    return null;

                case -2:
                    MessageBox.Show(Properties.Resources.PasswordIncorrect, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    tabNum = null;
                    userID = -1;
                    permissionsLo = 0;

                    return null;

                default:
                    if (dcUserID > 0)
                    {
                        //если больше нуля - пользователь userName является пользователем DC. проверяем является ли пользователь DC пользователем данного приложения
                        switch (DbRoutines.UserPermissions(dcUserID, out permissionsLo))
                        {
                            case false:
                                //пользователь userID не является пользователем приложения
                                MessageBox.Show(string.Format(Properties.Resources.UserIisNotAnApplicationUser, name, Application.ResourceAssembly.GetName().Name), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                                tabNum = null;
                                userID = -1;
                                permissionsLo = 0;

                                return false;

                            default:
                                tabNum = name;
                                userID = dcUserID;

                                return true;
                        }
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.PasswordIncorrect, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                        tabNum = null;
                        userID = -1;
                        permissionsLo = 0;

                        return null;
                    }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    this.btAuthenticate.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
            }
        }

        private void tb_User_LostFocus(object sender, RoutedEventArgs e)
        {
            this.LastInputElement = sender as IInputElement;
        }

        private void pbPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            this.LastInputElement = sender as IInputElement;
        }

    }
}
