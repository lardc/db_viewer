using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using SCME.ProfileBuilder.Annotations;
using SCME.ProfileBuilder.CustomControl;
using SCME.ProfileBuilder.Properties;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.Commutation;
using SCME.Types.DatabaseServer;
using SCME.Types.Profiles;
using GateTestParameters = SCME.Types.Gate.TestParameters;
using BvtTestParameters = SCME.Types.BVT.TestParameters;
using VtmTestParameters = SCME.Types.SL.TestParameters;
using DvDtParameters = SCME.Types.dVdt.TestParameters;
using AtuParameters = SCME.Types.ATU.TestParameters;
using QrrTqParameters = SCME.Types.QrrTq.TestParameters;
using RacParameters = SCME.Types.RAC.TestParameters;

namespace SCME.ProfileBuilder.PagesTech
{
    /// <summary>
    ///     Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class ProfilePage
    {
        private ProfileDictionary m_ProfileEngine;
        private readonly IProfilesService _profilesService;
        private string m_OldName = string.Empty;
        private Profile _selectedItem;

        public ProfilePage(IProfilesService profilesService)
        {            
            InitializeComponent();
            _profilesService = profilesService;

            var profiles = GetProfiles();

            m_ProfileEngine = new ProfileDictionary(profiles);

            treeViewProfiles.ItemsSource = ProfileItems;

            InitFilter();
        }

        private List<Profile> GetProfiles()
        {
            var profileItems = _profilesService.GetProfileItems();

            var profiles = new List<Profile>();
            foreach (var profileItem in profileItems)
            {
                var profile = new Profile(profileItem.ProfileName, profileItem.ProfileKey, profileItem.ProfileTS)
                {
                    IsHeightMeasureEnabled = profileItem.IsHeightMeasureEnabled,
                    ParametersClamp = profileItem.ParametersClamp,
                    Height = profileItem.Height,
                    Temperature = profileItem.Temperature,
                    ChilProfiles = new List<Profile>(profileItem.ChildProfileItems.Count),
                    IsParent = true
                };

                foreach (var g in profileItem.GateTestParameters) profile.TestParametersAndNormatives.Add(g);
                foreach (var b in profileItem.BVTTestParameters) profile.TestParametersAndNormatives.Add(b);
                foreach (var v in profileItem.VTMTestParameters) profile.TestParametersAndNormatives.Add(v);
                foreach (var d in profileItem.DvDTestParameterses) profile.TestParametersAndNormatives.Add(d);
                foreach (var a in profileItem.ATUTestParameters) profile.TestParametersAndNormatives.Add(a);
                foreach (var q in profileItem.QrrTqTestParameters) profile.TestParametersAndNormatives.Add(q);
                foreach (var r in profileItem.RACTestParameters) profile.TestParametersAndNormatives.Add(r);

                foreach (var childProfileItem in profileItem.ChildProfileItems)
                {
                    var childProfile = new Profile(childProfileItem.ProfileName, childProfileItem.ProfileKey,
                        childProfileItem.ProfileTS)
                    {
                        IsHeightMeasureEnabled = childProfileItem.IsHeightMeasureEnabled,
                        ParametersClamp = childProfileItem.ParametersClamp,
                        Height = childProfileItem.Height,
                        Temperature = childProfileItem.Temperature,
                        IsParent = false
                    };

                    foreach (var g in childProfileItem.GateTestParameters) childProfile.TestParametersAndNormatives.Add(g);
                    foreach (var b in childProfileItem.BVTTestParameters) childProfile.TestParametersAndNormatives.Add(b);
                    foreach (var v in childProfileItem.VTMTestParameters) childProfile.TestParametersAndNormatives.Add(v);
                    foreach (var d in childProfileItem.DvDTestParameterses) childProfile.TestParametersAndNormatives.Add(d);
                    foreach (var a in childProfileItem.ATUTestParameters) childProfile.TestParametersAndNormatives.Add(a);
                    foreach (var q in childProfileItem.QrrTqTestParameters) childProfile.TestParametersAndNormatives.Add(q);
                    foreach (var r in childProfileItem.RACTestParameters) childProfile.TestParametersAndNormatives.Add(r);

                    profile.ChilProfiles.Add(childProfile);
                }

                profiles.Add(profile);
            }
            return profiles;
        }

        public void InitSorting()
        {
            if (TestParameters.ItemsSource == null)
                return;
            var collectionView = CollectionViewSource.GetDefaultView(TestParameters.ItemsSource);

            if (collectionView != null)
                collectionView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
        }

        public void InitFilter()
        {
            var collectionView = CollectionViewSource.GetDefaultView(treeViewProfiles.ItemsSource);
            collectionView.Filter = UserFilter;
        }

        public void ClearFilter()
        {
            if (m_ProfileEngine != null && treeViewProfiles != null && treeViewProfiles.ItemsSource != null)
            {
                var view = CollectionViewSource.GetDefaultView(treeViewProfiles.ItemsSource);
                FilterTextBox.Text = "";
                view.Filter = null;
                view.Refresh();
            }
        }

        public void CleanFilter()
        {
            FilterTextBox.Text = "";
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

        private void ProfilesList_SelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            //if (profilesList.SelectedIndex < 0 && profilesList.Items.Count > 0)
            //    profilesList.SelectedIndex = 0;

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
            CleanFilter();
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
                ParametersComm = Settings.Default.SinglePositionModuleMode ? ModuleCommutationType.Direct : ModuleCommutationType.MT3,
                IsParent = true

            };

            var index =
                m_ProfileEngine.PlainCollection.TakeWhile(
                    Item => Comparer<string>.Default.Compare(Item.Name, newProfile.Name) < 0).Count();

            m_ProfileEngine.PlainCollection.Insert(index, newProfile);

            var item = m_ProfileEngine.PlainCollection[index];

            SelectItem(treeViewProfiles, new List<object>() { item });
        }

        private static void SelectItem(ItemsControl parentContainer, List<object> path)
        {
            var head = path.First();
            var tail = path.GetRange(1, path.Count - 1);
            var itemContainer = parentContainer.ItemContainerGenerator.ContainerFromItem(head) as TreeViewItem;

            if (itemContainer != null && itemContainer.Items.Count == 0)
            {
                itemContainer.IsSelected = true;

                var selectMethod = typeof(TreeViewItem).GetMethod("Select", BindingFlags.NonPublic | BindingFlags.Instance);
                selectMethod.Invoke(itemContainer, new object[] { true });
                itemContainer.BringIntoView();
            }
            else if (itemContainer != null)
            {
                itemContainer.IsExpanded = true;

                if (itemContainer.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    itemContainer.ItemContainerGenerator.StatusChanged += delegate
                    {
                        SelectItem(itemContainer, tail);
                    };
                }
                else
                {
                    SelectItem(itemContainer, tail);
                }
            }
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
                return;
            }

            //profilesList.IsCloseVisible = Visibility.Collapsed;
            treeViewProfiles.Items.Refresh();
            tbProfileName.HideTip();

            if (NavigationService != null)
            {
                ClearFilter();
                _profilesService.Dispose();
                NavigationService.Navigate(Cache.ConnectPage);
            }
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (tabControl != null && tabControl.SelectedIndex != 0)
            {
                tabControl.SelectedIndex = 0;
                return;
            }
            //profilesList.IsCloseVisible = Visibility.Collapsed;
            treeViewProfiles.Items.Refresh();
            tbProfileName.HideTip();

            mainGrid.IsEnabled = false;
            var profiles = m_ProfileEngine.PlainCollection;

            var profileItems = new List<ProfileItem>(profiles.Count);
            foreach (var profile in profiles)
            {
                var profileItem = new ProfileItem
                {
                    ProfileName = profile.Name,
                    ProfileKey = profile.Key,
                    ProfileTS = profile.Timestamp,
                    GateTestParameters = new List<GateTestParameters>(),
                    VTMTestParameters = new List<Types.SL.TestParameters>(),
                    BVTTestParameters = new List<Types.BVT.TestParameters>(),
                    DvDTestParameterses = new List<Types.dVdt.TestParameters>(),
                    ATUTestParameters = new List<Types.ATU.TestParameters>(),
                    QrrTqTestParameters = new List<Types.QrrTq.TestParameters>(),
                    RACTestParameters = new List<Types.RAC.TestParameters>(),
                    CommTestParameters = profile.ParametersComm,
                    IsHeightMeasureEnabled = profile.IsHeightMeasureEnabled,
                    ParametersClamp = profile.ParametersClamp,
                    Height = profile.Height,
                    Temperature = profile.Temperature
                };

                foreach (var baseTestParametersAndNormativese in profile.TestParametersAndNormatives)
                {
                    var gate = baseTestParametersAndNormativese as GateTestParameters;
                    if (gate != null)
                    {
                        profileItem.GateTestParameters.Add(gate);
                        continue;
                    }

                    var sl = baseTestParametersAndNormativese as Types.SL.TestParameters;
                    if (sl != null)
                    {
                        profileItem.VTMTestParameters.Add(sl);
                        continue;
                    }

                    var bvt = baseTestParametersAndNormativese as Types.BVT.TestParameters;
                    if (bvt != null)
                        profileItem.BVTTestParameters.Add(bvt);

                    var dvdt = baseTestParametersAndNormativese as Types.dVdt.TestParameters;
                    if (dvdt != null)
                        profileItem.DvDTestParameterses.Add(dvdt);

                    var atu = baseTestParametersAndNormativese as Types.ATU.TestParameters;
                    if (atu != null)
                        profileItem.ATUTestParameters.Add(atu);

                    var qrrtq = baseTestParametersAndNormativese as Types.QrrTq.TestParameters;
                    if (qrrtq != null)
                        profileItem.QrrTqTestParameters.Add(qrrtq);

                    var rac = baseTestParametersAndNormativese as Types.RAC.TestParameters;
                    if (rac != null)
                        profileItem.RACTestParameters.Add(rac);
                }

                profileItems.Add(profileItem);
            }

            _profilesService.SaveProfiles(profileItems);
            m_ProfileEngine = new ProfileDictionary(GetProfiles());
            treeViewProfiles.ItemsSource = ProfileItems;
            InitFilter();
            
            var dialog = new DialogWindow("Сообщение", "Профили в базе обновлены");
            dialog.ButtonConfig(DialogWindow.EbConfig.OK);
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                mainGrid.IsEnabled = true;
            }

            //if (NavigationService != null)
            //{
            //    ClearFilter();
            //    _profilesService.Dispose();
            //    NavigationService.Navigate(Cache.ConnectPage);
            //}
        }

        private void TbProfileName_GotKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E)
        {
            m_OldName = tbProfileName.Text;
        }

        public Profile SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; }
        }

        private void TbProfileName_LostKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E)
        {
            var tb = Sender as ValidatingTextBox;
            var tvItem = treeViewProfiles.SelectedItem as Profile;
            if (tb == null || !tb.IsFocused || tvItem != null)//|| profilesList.SelectedIndex == -1
                return;

            var newName = tb.Text.Trim();

            if (m_OldName != newName && m_ProfileEngine != null && CheckForExistingName(m_ProfileEngine.PlainCollection, newName))
            {
                tb.ShowTip("Profile with the same name is already exist");
                tb.Focus();
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = treeViewProfiles.IsEnabled = ButtonOk.IsEnabled = false;
            }
            else if (String.IsNullOrWhiteSpace(newName))
            {
                tb.ShowTip("Input profile name");
                tb.Focus();
            }
            else
            {
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = treeViewProfiles.IsEnabled = ButtonOk.IsEnabled = true;
                tb.HideTip();
                var item = m_ProfileEngine.PlainCollection.FirstOrDefault(t => t.Name.Equals(tvItem.Name));
                var indexOf = m_ProfileEngine.PlainCollection.IndexOf(item);
                m_ProfileEngine.PlainCollection[indexOf].Name = newName;

                m_ProfileEngine.PlainCollection.RemoveAt(indexOf);

                var index =
                    m_ProfileEngine.PlainCollection.TakeWhile(
                        Item => Comparer<string>.Default.Compare(Item.Name, newName) < 0).Count();

                m_ProfileEngine.PlainCollection.Insert(index, item);
            }
        }

        private void TbProfileName_OnKeyUp(object sender, KeyEventArgs e)
        {
            var textBox = sender as ValidatingTextBox;
            if (textBox == null || m_ProfileEngine == null || treeViewProfiles.SelectedItem == null)
                return;
            var name = textBox.Text.Trim();
            if (CheckForExistingName(m_ProfileEngine.PlainCollection, name))//&& name != m_ProfileEngine.PlainCollection[profilesList.SelectedIndex].Name
            {
                textBox.ShowTip("Profile with the same name is already exist");
                textBox.Focus();
                btnAdd.IsEnabled =
                    tabControl.IsEnabled = FilterTextBox.IsEnabled = treeViewProfiles.IsEnabled = ButtonOk.IsEnabled = false;
            }
            else
            {
                btnAdd.IsEnabled =
                    tabControl.IsEnabled = FilterTextBox.IsEnabled = treeViewProfiles.IsEnabled = ButtonOk.IsEnabled = true;
                textBox.HideTip();
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ButtonAddTestParameters_OnClick(object sender, RoutedEventArgs e)
        {
            if (ComboBoxParametersType.SelectedItem == null || treeViewProfiles.SelectedItem == null)
                return;

            var type = ComboBoxParametersType.SelectedItem.ToString();

            var selectedItem = treeViewProfiles.SelectedItem as Profile;
            var index = m_ProfileEngine.PlainCollection.IndexOf(selectedItem);
            var item = m_ProfileEngine.PlainCollection[index];

            var order = item.TestParametersAndNormatives.Count > 0 ? item.TestParametersAndNormatives.Max(t => t.Order) : 0;
            if (order == 10)
            {
                var dialog = new DialogWindow("Ошибка", "Превышен лимит измерений!");
                dialog.ButtonConfig(DialogWindow.EbConfig.OK);
                dialog.ShowDialog();
                return;
            }
            if (type.Contains("Gate"))
            {
                item.TestParametersAndNormatives.Add(new GateTestParameters() { Order = order + 1 });
            }
            else if (type.Contains("BVT"))
            {
                item.TestParametersAndNormatives.Add(new BvtTestParameters() { Order = order + 1 });
            }
            else if (type.Contains("SL"))
            {
                item.TestParametersAndNormatives.Add(new VtmTestParameters() { Order = order + 1 });
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
                item.TestParametersAndNormatives.Add(new RacParameters { Order = order + 1, IsEnabled = true });
            }
            else
            {
                item.IsHeightMeasureEnabled = true;
            }
        }

        private void TestParameters_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = (sender as ListBoxTestParameters);
            if (selectedItem == null)
                return;

            BaseTestParametersAndNormatives item = selectedItem.SelectedItem as GateTestParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 1;
                return;
            }

            item = selectedItem.SelectedItem as VtmTestParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 2;
                return;
            }

            item = selectedItem.SelectedItem as BvtTestParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 3;
                return;
            }

            item = selectedItem.SelectedItem as DvDtParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 4;
                return;
            }

            item = selectedItem.SelectedItem as AtuParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 5;
                return;
            }

            item = selectedItem.SelectedItem as QrrTqParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 6;
                return;
            }

            item = selectedItem.SelectedItem as RacParameters;
            if (item != null)
            {
                tabControl.SelectedIndex = 7;
                return;
            }
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_ProfileEngine != null && treeViewProfiles != null && treeViewProfiles.ItemsSource != null)
                CollectionViewSource.GetDefaultView(treeViewProfiles.ItemsSource).Refresh();
        }

        private void ButtonProfles_OnClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
            {
                NavigationService.Navigate(Cache.ConnectionsPage);
            }
        }
        
        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var item = menuItem.DataContext as Profile;
                m_ProfileEngine.PlainCollection.Remove(item);
            }
        }

        private void TreeViewProfiles_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            tabControl.SelectedIndex = 0;
            InitSorting();
        }
    }
}