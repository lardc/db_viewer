using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.Commutation;
using SCME.Types.BVT;
using SCME.Types.Profiles;
using SCME.UI.Annotations;
using SCME.UI.CustomControl;
using SCME.UI.IO;
using SCME.UI.PagesUser;
using SCME.UI.Properties;
using GateTestParameters = SCME.Types.Gate.TestParameters;
using BvtTestParameters = SCME.Types.BVT.TestParameters;
using VtmTestParameters = SCME.Types.SL.TestParameters;
using DvDtParameters = SCME.Types.dVdt.TestParameters;
using AtuParameters = SCME.Types.ATU.TestParameters;
using QrrTqParameters = SCME.Types.QrrTq.TestParameters;
using RACParameters = SCME.Types.RAC.TestParameters;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace SCME.UI.PagesTech
{
    /// <summary>
    ///     Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class ProfilePage
    {
        private readonly ProfileDictionary m_ProfileEngine;
        private string m_OldName = string.Empty;
        private object FLastSelectedItem;
        private readonly ConcurrentQueue<Action> m_ActionQueue;
        private readonly DispatcherTimer TimerForFound;
        private volatile bool m_StopAnaliseQueue = false;

        public ProfilePage(ProfileDictionary Engine)
        {
            InitializeComponent();

            m_ProfileEngine = Engine;

            profilesList.SetBinding(ItemsControl.ItemsSourceProperty,
                new Binding { ElementName = "profilePage", Path = new PropertyPath("ProfileItems") });

            if (profilesList.Items.Count > 0)
                profilesList.SelectedIndex = 0;

            InitFilter();

            //для возможности развязать по времени поиск профилей по части его имени от визуализации результата этого поиска
            m_ActionQueue = new ConcurrentQueue<Action>();
            TimerForFound = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 2000) };
            TimerForFound.Tick += OnTimerForFoundTick;
            TimerForFound.IsEnabled = false;
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

        private void InitSorting()
        {
            if (TestParameters.ItemsSource == null)
                return;

            var collectionView = CollectionViewSource.GetDefaultView(TestParameters.ItemsSource);
            collectionView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
        }

        public void InitFilter()
        {
            var collectionView = CollectionViewSource.GetDefaultView(profilesList.ItemsSource);
            collectionView.Filter = UserFilter;
        }

        public void ClearFilter()
        {
            if (m_ProfileEngine != null && profilesList != null && profilesList.ItemsSource != null)
            {
                var view = CollectionViewSource.GetDefaultView(profilesList.ItemsSource);
                FilterTextBox.Text = "";
                view.Filter = null;
                view.Refresh();
            }
        }

        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(FilterTextBox.Text) || FilterTextBox.Text == "Поиск")
                return true;

            return ((item as Profile).Name.IndexOf(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [UsedImplicitly]
        public ObservableCollection<Profile> ProfileItems
        {
            get { return m_ProfileEngine.PlainCollection; }
        }

        private void ProfilesList_RemoveItem(object Sender, ListBoxProfiles.RemoveItemEventArgs E)
        {
            E.Cancel = E.Profile.Name == @"_Default";
        }

        private void ProfilesList_SelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            if (profilesList.SelectedIndex < 0 && profilesList.Items.Count > 0)
                profilesList.SelectedIndex = 0;

            if (tabControl != null && tabControl.SelectedIndex != 0)
                tabControl.SelectedIndex = 0;

            InitSorting();
        }

        private void Button_Click(object Sender, RoutedEventArgs E)
        {
            var btn = Sender as Button;

            if (btn != null && btn.IsEnabled)
                tabControl.SelectedIndex = Convert.ToInt16(btn.CommandParameter);
        }

        private void Add_Click(object Sender, RoutedEventArgs E)
        {
            ClearFilter();
            const string name = @"New profile ";

            bool exists;
            var i = 0;

            do
            {
                i++;
                exists = CheckForExistingName(m_ProfileEngine.PlainCollection, name + i);
            } while (exists);

            var newProfile = new Profile
            {
                Name = name + i,
                Key = Guid.NewGuid(),
                ParametersComm =
                    Settings.Default.SinglePositionModuleMode
                        ? ModuleCommutationType.Direct
                        : ModuleCommutationType.MT3
            };

            var index =
                m_ProfileEngine.PlainCollection.TakeWhile(
                    Item => Comparer<string>.Default.Compare(Item.Name, newProfile.Name) < 0).Count();

            m_ProfileEngine.PlainCollection.Insert(index, newProfile);

            profilesList.SelectedIndex = index;
            var collectionView = CollectionViewSource.GetDefaultView(profilesList.ItemsSource);
            collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            profilesList.ScrollIntoView(profilesList.SelectedItem);
        }

        private static bool CheckForExistingName(IEnumerable<Profile> ProfileCollection, string ProfileName)
        {
            return ProfileCollection.Any(T => T.Name == ProfileName);
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (tabControl != null && tabControl.SelectedIndex != 0)
            {
                tabControl.SelectedIndex = 0;

                //забываем выбранный в TestParameters редактируемый в данный момент тип теста, чтобы событие TestParameters.PreviewMouseLeftButtonDown (TestParameters_PreviewMouseLeftButtonDown) работало корректно. если этого не сделать - в данном событии всегда будет обрабатываться первоначально выбранный тест без возможности сделать другой выбор
                TestParameters.SelectedItem = null;

                return;
            }

            profilesList.IsCloseVisible = Visibility.Collapsed;
            profilesList.Items.Refresh();
            tbProfileName.HideTip();

            if (NavigationService != null)
            {
                Cache.ProfileSelection = new ProfileSelectionPage(m_ProfileEngine);
                ClearFilter();

                switch (Cache.WorkMode)
                {
                    case (UserWorkMode.SpecialMeasure):
                        NavigationService.Navigate(Cache.UserWorkMode);
                        break;

                    default:
                        NavigationService.Navigate(Cache.Technician);
                        break;
                }
            }
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            //данный обработчик нажатия кнопки не может быть использован в режиме специальных измерений, проверяем режим работы
            if (Cache.WorkMode != UserWorkMode.SpecialMeasure)
            {
                if (tabControl != null && tabControl.SelectedIndex != 0)
                {
                    tabControl.SelectedIndex = 0;
                    return;
                }

                profilesList.IsCloseVisible = Visibility.Collapsed;
                profilesList.Items.Refresh();
                tbProfileName.HideTip();

                mainGrid.IsEnabled = false;
                ProfilesDbLogic.SaveProfilesToDb(m_ProfileEngine.PlainCollection);

                var dialog = new DialogWindow("Сообщение", "Профили в базе обновлены");
                dialog.ButtonConfig(DialogWindow.EbConfig.OK);

                var result = dialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    mainGrid.IsEnabled = true;
                }
            }
        }

        void ButtonNext_OnClick(object sender, RoutedEventArgs e)
        {
            //данный обработчик можно использовать только в режиме специальных измерений, проверяем режим работы
            if (Cache.WorkMode == UserWorkMode.SpecialMeasure)
            {
                //смысл этого режима - возможность изменения профиля без сохранения этих изменений в базу данных. параметры профиля при этом хранятся в оперативной памяти
                if (tabControl != null && tabControl.SelectedIndex != 0)
                {
                    tabControl.SelectedIndex = 0;

                    return;
                }

                profilesList.IsCloseVisible = Visibility.Collapsed;
                profilesList.Items.Refresh();
                tbProfileName.HideTip();

                //отредактированный профиль сразу выбираем для проведения измерений в page UserTest
                var profile = profilesList.SelectedItem as Profile;

                if (profile == null)
                    return;

                //запоминаем в UserTest тот профиль, который был отредактирован
                Cache.UserTest.Profile = profile;

                //запоминаем в UserTest флаг 'Режим специальных измерений' для возможности корректной работы её MultiIdentificationFieldsToVisibilityConverter 
                Cache.UserTest.SpecialMeasureMode = true;

                //открываем page UserTest, при этом данная page должна проверить установленный режим работы и спрятать от пользователя поля ввода идентификационной информации, т.к. в режиме специальных измерений идентификационная информация вводится не должна и не должна сохранятся в базу данных (ни в локальную, ни в центральную)  
                if (NavigationService != null)
                    NavigationService.Navigate(Cache.UserTest);
            }
        }

        private void TbProfileName_GotKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E)
        {
            m_OldName = tbProfileName.Text;
        }

        private void TbProfileName_LostKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E)
        {
            var tb = Sender as ValidatingTextBox;
            if (tb == null || !tb.IsFocused || profilesList.SelectedIndex == -1)
                return;

            var newName = tb.Text.Trim();

            if (m_OldName != newName && CheckForExistingName(m_ProfileEngine.PlainCollection, newName))
            {
                tb.ShowTip("Profile with the same name is already exist");
                tb.Focus();
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = btUniversal.IsEnabled = false;
            }
            else if (string.IsNullOrWhiteSpace(newName))
            {
                tb.ShowTip("Input profile name");
                tb.Focus();
            }
            else
            {
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = btUniversal.IsEnabled = true;
                tb.HideTip();
                var item = profilesList.SelectedItem as Profile;
                if (ReferenceEquals(item, null))
                    return;
                item.Name = newName;
                InitSorting();
                ClearFilter();
                InitFilter();
                profilesList.ScrollIntoView(profilesList.SelectedItem);
            }
        }

        private void TbProfileName_OnKeyUp(object sender, KeyEventArgs e)
        {
            var textBox = sender as ValidatingTextBox;
            if (textBox == null || m_ProfileEngine == null || profilesList.SelectedIndex == -1)
                return;
            var name = textBox.Text.Trim();
            if (CheckForExistingName(m_ProfileEngine.PlainCollection, name) && name != m_ProfileEngine.PlainCollection[profilesList.SelectedIndex].Name)
            {
                textBox.ShowTip("Profile with the same name is already exist");
                textBox.Focus();
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = btUniversal.IsEnabled = false;
            }
            else
            {
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = btUniversal.IsEnabled = true;
                textBox.HideTip();
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ButtonAddTestParameters_OnClick(object sender, RoutedEventArgs e)
        {
            if (ComboBoxParametersType.SelectedItem == null || profilesList.SelectedIndex == -1)
                return;
            ClearFilter();
            var type = ComboBoxParametersType.SelectedItem.ToString();
            var item = profilesList.SelectedItem as Profile;
            if (ReferenceEquals(item, null))
                return;
            var order = item.TestParametersAndNormatives.Count > 0
                ? item.TestParametersAndNormatives.Max(t => t.Order)
                : 0;
            if (order == 10)
            {
                var dialog = new DialogWindow("Ошибка", "Превышен лимит измерений!");
                dialog.ButtonConfig(DialogWindow.EbConfig.OK);
                dialog.ShowDialog();
                return;
            }

            if (type.Contains("Gate"))
            {
                item.TestParametersAndNormatives.Add(new GateTestParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("BVT"))
            {
                item.TestParametersAndNormatives.Add(new BvtTestParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("VTM"))
            {
                item.TestParametersAndNormatives.Add(new VtmTestParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("dVdt"))
            {
                item.TestParametersAndNormatives.Add(new DvDtParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("ATU"))
            {
                item.TestParametersAndNormatives.Add(new AtuParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("QrrTq"))
            {
                item.TestParametersAndNormatives.Add(new QrrTqParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("RAC"))
            {
                item.TestParametersAndNormatives.Add(new RACParameters { Order = order + 1, IsEnabled = true });
            }
            else
            {
                item.IsHeightMeasureEnabled = true;
            }
            InitFilter();
        }

        private void TestParameters_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //отказался от использования события MouseDoubleClick, т.к. сенсор установленный на оборудовании для которого разработано это ПО не понимает двойной тап, т.е. мышь работает отлично, а сенсор полностью игнорирует двойной тап и без мыши зайти в редактирование параметров профиля не получается
            //работает это так:
            //1-ый тап по экрану - пользователь жмёт пальцем на нужный тест выбранного профиля, при этом this.FLastSelectedItem=null. по завершению исполнения кода данного события система запоминает выбранный тип теста - устанавливается значение ListBox.SelectedItemthis.FLastSelectedItem;
            //2-ой тап по экрану - this.FLastSelectedItem уже установлен и данная реализация лишь устанавливает соответствующий выбранному типу теста tabControl.SelectedIndex.
            //по нажатию кнопки Back см. обработчик Back_Click система забывает выбранный в пределах профиля тест, получается как бы двойной клик для выбора редактора параметров в выбранном типе теста, хотя время между первым тапом и вторым тапом не контролируется

            ListBoxTestParameters ListBox = sender as ListBoxTestParameters;

            if (ListBox == null)
                return;

            //если значение ListBox.SelectedItem не определено - производится выбор нужного теста в данном профиле
            if (ListBox.SelectedItem == null)
            {
                return;
            }
            else
            {
                if (this.FLastSelectedItem == null)
                {
                    //первый тап по экрану
                    this.FLastSelectedItem = ListBox.SelectedItem;
                    return;
                }
                else
                {
                    //второй тап по экрану
                    if (this.FLastSelectedItem.GetType() == ListBox.SelectedItem.GetType())
                    {
                        this.FLastSelectedItem = ListBox.SelectedItem;
                    }
                    else
                    {
                        //выбран другой тип теста
                        this.FLastSelectedItem = null;
                        return;
                    }
                }
            }

            //раз мы здесь - это случай второго тап по экрану и выбран в точности тот же тип теста, что и при первом тапе по экрану
            BaseTestParametersAndNormatives item = ListBox.SelectedItem as GateTestParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 1;
                this.FLastSelectedItem = null;
                return;
            }

            item = ListBox.SelectedItem as VtmTestParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 2;
                this.FLastSelectedItem = null;
                return;
            }

            item = ListBox.SelectedItem as BvtTestParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 3;
                this.FLastSelectedItem = null;
                return;
            }

            item = ListBox.SelectedItem as DvDtParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 4;
                this.FLastSelectedItem = null;
                return;
            }

            item = ListBox.SelectedItem as AtuParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 5;
                this.FLastSelectedItem = null;
                return;
            }

            item = ListBox.SelectedItem as QrrTqParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 6;
                this.FLastSelectedItem = null;
                return;
            }

            item = ListBox.SelectedItem as RACParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 7;
                this.FLastSelectedItem = null;
                return;
            }
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            RestartTimerForFound();

            m_ActionQueue?.Enqueue(delegate
            {
                if (m_ProfileEngine != null && profilesList != null && profilesList.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(profilesList.ItemsSource).Refresh();
                    BuildTittle();
                }
            });

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

        private void ProfilePage_Loaded(object Sender, RoutedEventArgs E)
        {
            //в зависимости от режима работы будем использовать разные обработчики нажатия кнопки btUniversal, также будем менять Content этой кнопки
            if (Cache.WorkMode == UserWorkMode.SpecialMeasure)
            {
                //форма открыта в режиме специальных измерений - меняем Content кнопки на "Next"
                btUniversal.Content = Properties.Resources.Next;

                //уставливаем обработчик нажатия для режима специальных измерений
                btUniversal.Click += ButtonNext_OnClick;
            }
            else
            {
                //в любом режиме работы кроме режима специальных измерений кнопка имеет Content="OK"
                btUniversal.Content = Properties.Resources.Ok;

                //уставливаем обработчик нажатия для кнопки Ok
                btUniversal.Click += ButtonOk_OnClick;
            }

            //событие TextChanged любого TextBox наступает автоматически при инициализации всего Page. чтобы оно не наступало автоматически при инициализации Page будем вешать его обработчик здесь
            FilterTextBox.TextChanged += FilterTextBox_OnTextChanged;

            BuildTittle();
        }

        private void BuildTittle()
        {
            Title = Properties.Resources.Profiles;

            if (profilesList != null)
                Title = Title + "\n" + string.Format("всего {0} шт.", profilesList.Items.Count);

            Cache.Main.TopTitle = Title;
        }

        private void profilePage_Unloaded(object sender, RoutedEventArgs e)
        {
            TimerForFound.IsEnabled = false;
        }
    }
}