using System;
using System.Collections.Generic;
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
using SCME.UI.IO;
using SCME.UI.Properties;
using System.Windows.Navigation;

namespace SCME.UI.PagesUser
{
    /// <summary>
    /// Interaction logic for UserWorkMode.xaml
    /// </summary>
    public partial class UserWorkModePage
    {
        public UserWorkModePage()
        {
            InitializeComponent();

            btn_SpecialMeasure.Visibility = Settings.Default.SpecialMeasureForUse ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetButtonsIsEnabled(bool IsEnabled)
        {
            btn_Operator.IsEnabled = IsEnabled;
            btn_ServiceMan.IsEnabled = IsEnabled;
            btn_SpecialMeasure.IsEnabled = IsEnabled;
            btn_Back.IsEnabled = IsEnabled;
        }

        private void Button_Click(object Sender, RoutedEventArgs E)
        {
            //чтобы пользователь не смог нажать повторно на кнопку, чей обработчик уже исполняется или на любую другую кнопку этой формы
            SetButtonsIsEnabled(false);

            try
            {
                var btn = (Button)Sender;
                switch (Convert.ToUInt16(btn.CommandParameter))
                {
                    case 1:
                        //если мы были в режиме специальных измерений - надо перечитать содержимое профилей из базы данных чтобы откатить все изменения, сделанные в профилях в режиме специальных имерений
                        if (Cache.WorkMode == UserWorkMode.SpecialMeasure)
                            ProfilesDbLogic.ImportProfilesFromDb();

                        //запоминаем выбранный пользователем режим работы
                        Cache.WorkMode = UserWorkMode.Operator;

                        NavigationService?.Navigate(Cache.Login);

                        break;

                    case 2:
                        //если мы были в режиме специальных измерений - надо перечитать содержимое профилей из базы данных чтобы откатить все изменения, сделанные в профилях в режиме специальных имерений
                        if (Cache.WorkMode == UserWorkMode.SpecialMeasure)
                            ProfilesDbLogic.ImportProfilesFromDb();

                        //запоминаем выбранный пользователем режим работы
                        Cache.WorkMode = UserWorkMode.ServiceMan;
                        Cache.Main.ServiceManLogin();

                        break;

                    case 3:
                        if (Settings.Default.IsTechPasswordEnabled)
                        {
                            //без ввода пароля наладчика не будем пускать в режим специальных измерений
                            Cache.Password.AfterOkRoutine += SpecialMeasureCallBack;
                            NavigationService?.Navigate(Cache.Password);
                        }
                        else SpecialMeasureCallBack(NavigationService);

                        break;
                }
            }
            finally
            {
                SetButtonsIsEnabled(true);
            }
        }

        private void SpecialMeasureCallBack(NavigationService ns)
        {
            //запоминаем выбранный пользователем режим работы
            Cache.WorkMode = UserWorkMode.SpecialMeasure;

            Cache.ProfileSelection.ClearFilter();
            Cache.ProfileEdit.InitFilter();

            ns?.Navigate(Cache.ProfileEdit);
        }

        private void btn_Back_OnClick(object Sender, RoutedEventArgs E)
        {
            NavigationService?.Navigate(Cache.Welcome);
        }
    }
}
