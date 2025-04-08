using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SCME.Types;
using SCME.Types.Profiles;
using SCME.Types.DatabaseServer;
using SCME.UI.Properties;
using System.Collections.Concurrent;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media;
using SCME.UI.IO;
using SCME.Types.BaseTestParams;

namespace SCME.UI.PagesUser
{
    /// <summary>
    /// Interaction logic for UserPage.xaml
    /// </summary>   

    public partial class ProfileSelectionPage
    {
        private readonly ConcurrentQueue<Action> m_ActionQueue;
        private readonly DispatcherTimer TimerForFound;
        private readonly DispatcherTimer TimerSyncSelectedProfile;
        private volatile bool m_StopAnaliseQueue = false;
        private Guid? FLastSelectedProfileKey;

        public ProfileSelectionPage(ProfileDictionary ProEngine)
        {
            InitializeComponent();

            profilesList.Items.Clear();

            profilesList.ItemsSource = ProEngine.PlainCollection;

            profilesList.Items.Refresh();

            if (profilesList.Items.Count > 0)
                profilesList.SelectedIndex = 0;

            //для возможности развязать по времени поиск профилей по части его имени от визуализации результата этого поиска
            m_ActionQueue = new ConcurrentQueue<Action>();
            TimerForFound = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 2000) };
            TimerForFound.Tick += OnTimerForFoundTick;
            TimerForFound.IsEnabled = false;

            //настраиваем таймер, который будет использоваться при синхронизации выбранного профиля
            TimerSyncSelectedProfile = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 2500) };
            TimerSyncSelectedProfile.Tick += OnTimerSyncSelectedProfileTick;
            TimerSyncSelectedProfile.IsEnabled = false;
        }

        private void OnTimerForFoundTick(object Sender, EventArgs E)
        {
            if (m_StopAnaliseQueue)
            {
                DispatcherTimer timer = Sender as DispatcherTimer;

                if (timer != null)
                    timer.Stop();

                return;
            }

            if (m_ActionQueue != null)
            {
                Action act;

                while (m_ActionQueue.TryDequeue(out act))
                    act.Invoke();
            }
        }

        private void RestartTimerForFound()
        {
            if (TimerForFound != null)
                TimerForFound.Stop();

            if (m_ActionQueue != null)
            {
                //пользователю важен конечный результат - т.е. отклик на последний введённый символ, поэтому выгребаем всю очередь без её обработки
                Action act;
                while (m_ActionQueue.TryDequeue(out act)) ;
            }

            m_StopAnaliseQueue = false;

            if (TimerForFound != null)
                TimerForFound.Start();
        }

        private ProfileItem GetProfileItemFromServerDb(object Sender, string ProfileName, string MMECode, ref bool? Found)
        {
            //UI имеет возможность работы с сервером CentralDatabaseService без использования Service.exe. если связь с сервером установлена и профиль успешно получен, то синхронизируем выбранный профиль
            ProfileItem ProfileItem = null;
            bool ProfileItemFound = false;

            try
            {
                ProfileItem = Cache.Net.GetProfileFromServerDb(ProfileName, MMECode, ref ProfileItemFound);
                Found = ProfileItemFound;
            }
            catch (Exception)
            {
                if (TimerSyncSelectedProfile != null)
                    TimerSyncSelectedProfile.Stop();

                Found = null;
                NeedSyncButton.Visibility = Visibility.Hidden;
            }

            return ProfileItem;
        }

        private void CheckSyncedSelectedProfile()
        {
            //проверяет синхронизирован ли выбранный профиль или нет
            Profile profile = profilesList.SelectedItem as Profile;

            if (profile != null)
            {
                //если после истечения времени отсчитанного таймером пользователь ещё не успел выбрать профиль, либо выбран всё тот же профиль - вывешиваем пользователю огромную прозрачную кнопку с сообщением о необходимости синхронизации выбранного профиля
                if ((FLastSelectedProfileKey == null) || (FLastSelectedProfileKey == profile.Key))
                {
                    bool? Found = null;

                    ProfileItem ActualProfileItem = GetProfileItemFromServerDb(null, profile.Name, Settings.Default.MMECode, ref Found);

                    switch (Found)
                    {
                        case null:
                            //связь с SCME.DatabaseServer не установлена, но работоспособность должна быть в любом случае
                            NeedSyncButton.Visibility = Visibility.Hidden;
                            break;

                        case true:
                            if (ActualProfileItem != null)
                            {
                                //профиль получен, смотрим надо ли выполнять синхронизацию
                                if (IsNeedSync(ActualProfileItem, profile))
                                    NeedSyncButton.Visibility = Visibility.Visible;
                                else NeedSyncButton.Visibility = Visibility.Hidden;
                            }

                            break;

                        case false:
                            //искомый профиль не найден, возможно он был удалён
                            NeedSyncButton.Visibility = Visibility.Visible;
                            break;
                    }
                }
            }
        }

        private void OnTimerSyncSelectedProfileTick(object Sender, EventArgs E)
        {
            //вызывается при истечении интервала времени, отсчитываемого таймером TimerSyncSelectedProfile
            CheckSyncedSelectedProfile();
        }

        public void InitSorting()
        {
            var collectionView = CollectionViewSource.GetDefaultView(TestParameters.ItemsSource);

            if (collectionView != null)
                collectionView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
        }

        public void InitFilter()
        {
            var collectionView = CollectionViewSource.GetDefaultView(profilesList.ItemsSource);
            collectionView.Filter = UserFilter;
        }

        public void ClearFilter()
        {
            if (profilesList != null && profilesList.ItemsSource != null)
            {
                var view = CollectionViewSource.GetDefaultView(profilesList.ItemsSource);
                FilterTextBox.Text = "";
                view.Filter = null;
                view.Refresh();
            }
        }

        internal void SetNextButtonVisibility(TypeCommon.InitParams Param)
        {
            btnGoNext.Visibility = Param.IsGateEnabled || Param.IsSLEnabled || Param.IsBVTEnabled || Param.IsdVdtEnabled || Param.IsATUEnabled || Param.IsIHEnabled || Param.IsRACEnabled || Param.IsQrrTqEnabled
                                       ? Visibility.Visible
                                       : Visibility.Hidden;
        }

        private void Next_Click(object sender, RoutedEventArgs E)
        {
            var profile = profilesList.SelectedItem as Profile;
            if (profile == null)
                return;

            Cache.UserTest.Profile = profile;
            
            //запоминаем в UserTest флаг 'Режим специальных измерений' для возможности корректной работы её MultiIdentificationFieldsToVisibilityConverter 
            Cache.UserTest.SpecialMeasureMode = (Cache.WorkMode == UserWorkMode.SpecialMeasure);

            if (NavigationService != null)
            {
                Cache.UserTest.InitSorting();
                Cache.UserTest.InitTemp();
                NavigationService.Navigate(Cache.UserTest);
            }
        }

        private void Results_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
                NavigationService.Navigate(Cache.Results);
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            RestartTimerForFound();

            m_ActionQueue?.Enqueue(delegate
            {
                if (profilesList != null && profilesList.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(profilesList.ItemsSource).Refresh();
                    m_StopAnaliseQueue = true;
                    BuildTittle();
                }
            });
        }

        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(FilterTextBox.Text) || FilterTextBox.Text == "Поиск")
                return true;
            else
                return ((item as Profile).Name.IndexOf(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase) != -1);
        }

        private Guid? SelectedProfileKey()
        {
            if (profilesList == null)
                return null;
            else
            {
                Profile profile = profilesList.SelectedItem as Profile;

                if (profile == null)
                    return null;
                else return profile.Key;
            }
        }

        private void ProfilesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InitSorting();

            //запоминаем какой профиль выбрал пользователь
            FLastSelectedProfileKey = SelectedProfileKey();
            TimerSyncSelectedProfile?.Start();
        }

        private void BuildTittle()
        {
            Title = Properties.Resources.UserPage_Title;

            if (profilesList != null)
                Title = Title + "\n" + Properties.Resources.Total.ToLower() + string.Format(" {0} шт.", profilesList.Items.Count);

            Cache.Main.TopTitle = Title;
        }

        private BaseTestParametersAndNormatives TestByTypeAndOrder(ProfileItem profileItem, TestParametersType type, int order)
        {
            //извлекает из принятого profileItem первый найденный тест с принятым type и порядком исполнения order
            BaseTestParametersAndNormatives Test = null;

            if (profileItem != null)
            {
                switch (type)
                {
                    case TestParametersType.Gate:
                        for (int i = 0; i < profileItem.GateTestParameters.Count; i++)
                        {
                            Test = profileItem.GateTestParameters[i];

                            if (Test.Order == order)
                                return Test;
                        }

                        break;

                    case TestParametersType.StaticLoses:
                        for (int i = 0; i < profileItem.VTMTestParameters.Count; i++)
                        {
                            Test = profileItem.VTMTestParameters[i];

                            if (Test.Order == order)
                                return Test;
                        }

                        break;

                    case TestParametersType.Bvt:
                        for (int i = 0; i < profileItem.BVTTestParameters.Count; i++)
                        {
                            Test = profileItem.BVTTestParameters[i];

                            if (Test.Order == order)
                                return Test;
                        }

                        break;

                    case TestParametersType.Dvdt:
                        for (int i = 0; i < profileItem.DvDTestParameterses.Count; i++)
                        {
                            Test = profileItem.DvDTestParameterses[i];

                            if (Test.Order == order)
                                return Test;
                        }

                        break;

                    case TestParametersType.ATU:
                        for (int i = 0; i < profileItem.ATUTestParameters.Count; i++)
                        {
                            Test = profileItem.ATUTestParameters[i];

                            if (Test.Order == order)
                                return Test;
                        }

                        break;

                    case TestParametersType.QrrTq:
                        for (int i = 0; i < profileItem.QrrTqTestParameters.Count; i++)
                        {
                            Test = profileItem.QrrTqTestParameters[i];

                            if (Test.Order == order)
                                return Test;
                        }

                        break;

                    case TestParametersType.RAC:
                        for (int i = 0; i < profileItem.RACTestParameters.Count; i++)
                        {
                            Test = profileItem.RACTestParameters[i];

                            if (Test.Order == order)
                                return Test;
                        }

                        break;
                }
            }

            //раз мы здесь - значит ничего не нашли
            return null;
        }

        private bool IsNeedSync(ProfileItem actualProfileItem, Profile profile)
        {
            //отвечает на вопрос о различии в данных принятых actualProfileItem и profile, т.е. надо ли синхронизировать принятый profile данными actualProfileItem
            bool Result;

            if ((actualProfileItem != null) && (profile != null))
            {
                Result = !((profile.Name == actualProfileItem.ProfileName) &
                          //(profile.Key == actualProfileItem.ProfileKey) &
                          //(profile.Timestamp == actualProfileItem.ProfileTS) &
                          (profile.IsHeightMeasureEnabled == actualProfileItem.IsHeightMeasureEnabled) &
                          (profile.ParametersClamp == actualProfileItem.ParametersClamp) &
                          (profile.Height == actualProfileItem.Height) &
                          (profile.Temperature == actualProfileItem.Temperature) &
                          (profile.ParametersComm == actualProfileItem.CommTestParameters));

                //если уже найдены отличия - то искать их дальше нет смысла
                if (Result)
                    return Result;

                //отличий пока не выявлено - продолжаем их искать перебирая списки параметров
                int GateCount = 0;
                int BVTCount = 0;
                int SlCount = 0;
                int DvDTCount = 0;
                int ATUCount = 0;
                int QrrTqCount = 0;
                int RACCount = 0;

                foreach (var baseTestParametersAndNormativese in profile.TestParametersAndNormatives)
                {
                    var gate = baseTestParametersAndNormativese as Types.Gate.TestParameters;
                    if (gate != null)
                    {
                        var g = TestByTypeAndOrder(actualProfileItem, TestParametersType.Gate, gate.Order);

                        if (g == null)
                            return true;

                        if (g.IsHasChanges(gate))
                            return true;

                        GateCount++;
                    }

                    var bvt = baseTestParametersAndNormativese as Types.BVT.TestParameters;
                    if (bvt != null)
                    {
                        var b = TestByTypeAndOrder(actualProfileItem, TestParametersType.Bvt, bvt.Order);

                        if (b == null)
                            return true;

                        if (b.IsHasChanges(bvt))
                            return true;

                        BVTCount++;
                    }

                    var sl = baseTestParametersAndNormativese as Types.SL.TestParameters;
                    if (sl != null)
                    {
                        var s = TestByTypeAndOrder(actualProfileItem, TestParametersType.StaticLoses, sl.Order);

                        if (s == null)
                            return true;

                        if (s.IsHasChanges(sl))
                            return true;

                        SlCount++;
                    }

                    var dvdt = baseTestParametersAndNormativese as Types.dVdt.TestParameters;
                    if (dvdt != null)
                    {
                        var d = TestByTypeAndOrder(actualProfileItem, TestParametersType.Dvdt, dvdt.Order);

                        if (d == null)
                            return true;

                        if (d.IsHasChanges(dvdt))
                            return true;

                        DvDTCount++;
                    }

                    var atu = baseTestParametersAndNormativese as Types.ATU.TestParameters;
                    if (atu != null)
                    {
                        var a = TestByTypeAndOrder(actualProfileItem, TestParametersType.ATU, atu.Order);

                        if (a == null)
                            return true;

                        if (a.IsHasChanges(atu))
                            return true;

                        ATUCount++;
                    }

                    var qrrTq = baseTestParametersAndNormativese as Types.QrrTq.TestParameters;
                    if (qrrTq != null)
                    {
                        var q = TestByTypeAndOrder(actualProfileItem, TestParametersType.QrrTq, qrrTq.Order);

                        if (q == null)
                            return true;

                        if (q.IsHasChanges(qrrTq))
                            return true;

                        QrrTqCount++;
                    }

                    var rac = baseTestParametersAndNormativese as Types.RAC.TestParameters;
                    if (rac != null)
                    {
                        var r = TestByTypeAndOrder(actualProfileItem, TestParametersType.RAC, rac.Order);

                        if (r == null)
                            return true;

                        if (r.IsHasChanges(rac))
                            return true;

                        RACCount++;
                    }
                }

                //проверяем, что количество параметров в принятых actualProfileItem и profile одинаково
                Result = (GateCount != actualProfileItem.GateTestParameters.Count);

                if (Result)
                    return Result;

                Result = (BVTCount != actualProfileItem.BVTTestParameters.Count);

                if (Result)
                    return Result;

                Result = (SlCount != actualProfileItem.VTMTestParameters.Count);

                if (Result)
                    return Result;

                Result = (DvDTCount != actualProfileItem.DvDTestParameterses.Count);

                if (Result)
                    return Result;

                Result = (ATUCount != actualProfileItem.ATUTestParameters.Count);

                if (Result)
                    return Result;

                Result = (QrrTqCount != actualProfileItem.QrrTqTestParameters.Count);

                if (Result)
                    return Result;

                Result = (RACCount != actualProfileItem.RACTestParameters.Count);

                if (Result)
                    return Result;
            }

            //раз мы здесь - отличий не найдено
            return false;
        }

        private void CopyProfileItemToProfile(ProfileItem Source, Profile Dest)
        {
            //копирование данных Source в Dest
            if (Source != null)
            {
                if (Dest != null)
                {
                    Dest.Name = Source.ProfileName;
                    Dest.Key = Source.ProfileKey;
                    Dest.Timestamp = Source.ProfileTS;

                    Dest.IsHeightMeasureEnabled = Source.IsHeightMeasureEnabled;
                    Dest.ParametersClamp = Source.ParametersClamp;
                    Dest.Height = Source.Height;
                    Dest.Temperature = Source.Temperature;
                    Dest.ParametersComm = Source.CommTestParameters;

                    Dest.TestParametersAndNormatives.Clear();

                    foreach (var g in Source.GateTestParameters)
                        Dest.TestParametersAndNormatives.Add(g);

                    foreach (var b in Source.BVTTestParameters)
                        Dest.TestParametersAndNormatives.Add(b);

                    foreach (var v in Source.VTMTestParameters)
                        Dest.TestParametersAndNormatives.Add(v);

                    foreach (var d in Source.DvDTestParameterses)
                        Dest.TestParametersAndNormatives.Add(d);

                    foreach (var a in Source.ATUTestParameters)
                        Dest.TestParametersAndNormatives.Add(a);

                    foreach (var q in Source.QrrTqTestParameters)
                        Dest.TestParametersAndNormatives.Add(q);

                    foreach (var r in Source.RACTestParameters)
                        Dest.TestParametersAndNormatives.Add(r);
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        private void RefreshBindings()
        {
            //поиск компонентов Label, расположенных внутри borderCommutation и обновление отображаемой информации
            BindingExpression be;

            foreach (Label lb in FindVisualChildren<Label>(borderCommutation))
            {
                be = BindingOperations.GetBindingExpression(lb, Label.ContentProperty);

                if (be != null)
                    be.UpdateTarget();
            }

            //обновление данных, отображаемых TestParameters (ListViewTestParametersSelection)
            be = BindingOperations.GetBindingExpression(TestParameters, SCME.UI.CustomControl.ListViewTestParametersSelection.ItemsSourceProperty);

            if (be != null)
                be.UpdateTarget();

            //обновление данных, расположенных внутри borderHeight
            foreach (CheckBox cb in FindVisualChildren<CheckBox>(borderHeight))
            {
                be = BindingOperations.GetBindingExpression(cb, CheckBox.IsCheckedProperty);

                if (be != null)
                    be.UpdateTarget();
            }
        }

        private void NeedSyncButton_Click(object sender, RoutedEventArgs e)
        {
            //синхронизируем выделенный профиль
            Profile profile = profilesList.SelectedItem as Profile;

            if (profile != null)
            {
                bool? Found = false;

                //UI имеет возможность работы с сервером CentralDatabaseService без использования Service.exe. если связь с сервером установлена и профиль успешно получен, то синхронизируем выбранный профиль
                ProfileItem ActualProfileItem = GetProfileItemFromServerDb(sender, profile.Name, Settings.Default.MMECode, ref Found);

                switch (Found)
                {
                    case null:
                        //связь с SCME.DatabaseServer не установлена, но работоспособность должна быть в любом случае
                        break;

                    case true:
                        if (ActualProfileItem != null)
                        {
                            //профиль получен, смотрим надо ли выполнять синхронизацию
                            if (IsNeedSync(ActualProfileItem, profile))
                            {
                                //есть необходимость в синхронизации данных, заменяем данные профиля который отображается в списке теми данными, что содержатся в ActualProfileItem
                                CopyProfileItemToProfile(ActualProfileItem, profile);

                                //после выполнения копирования идентификатор профиля изменился, поэтому
                                FLastSelectedProfileKey = profile.Key;

                                //сохраняем синхронизированный profile в локальную базу данных
                                IList<Profile> ProfileEngine = (IList<Profile>)profilesList.ItemsSource;
                                ProfilesDbLogic.SaveProfilesToDb(ProfileEngine);

                                //обновляем данные на форме
                                RefreshBindings();
                            }
                        }

                        break;

                    case false:
                        {
                            //искомый профиль не найден, возможно он был удалён из центральной базы данных, значит надо удалить его и из локальноЙ базы данных
                            IList<Profile> ProfileEngine = (IList<Profile>)profilesList.ItemsSource;
                            ProfileEngine.Remove(profile);
                            ProfilesDbLogic.SaveProfilesToDb(ProfileEngine);
                            BuildTittle();

                            if (profilesList.Items.Count > 0)
                                profilesList.SelectedIndex = 0;
                        }

                        break;
                }

                //синхронизация выбранного профиля выполнена - прячем кнопку
                ((Button)sender).Visibility = Visibility.Hidden;
            }
        }

        private void ProfileSelectionPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            //чтобы пользователь не смог начать измерения с профилем, который ещё не синхронизирован
            CheckSyncedSelectedProfile();

            //TimerForFound.IsEnabled = true;
            TimerSyncSelectedProfile.IsEnabled = true;

            //событие TextChanged любого TextBox наступает автоматически при инициализации всего Page. чтобы оно не наступало автоматически при инициализации Page будем вешать его обработчик здесь
            FilterTextBox.TextChanged += FilterTextBox_OnTextChanged;

            BuildTittle();
        }

        private void ProfileSelectionPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            TimerForFound.IsEnabled = false;
            TimerSyncSelectedProfile.IsEnabled = false;
        }
    }
}