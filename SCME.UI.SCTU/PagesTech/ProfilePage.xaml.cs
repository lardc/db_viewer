using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using SCME.Types;
using SCME.Types.BaseTestParams;
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

namespace SCME.UI.PagesTech
{
    /// <summary>
    ///     Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class ProfilePage
    {
        private readonly ProfileDictionary m_ProfileEngine;
        private string m_OldName = string.Empty;
        //private bool Renamed = false;

        public ProfilePage(ProfileDictionary Engine)
        {
            InitializeComponent();

            m_ProfileEngine = Engine;

            profilesList.SetBinding(ItemsControl.ItemsSourceProperty,
                new Binding { ElementName = "profilePage", Path = new PropertyPath("ProfileItems") });

            if (profilesList.Items.Count > 0)
                profilesList.SelectedIndex = 0;

            InitFilter();
        }

        public void InitSorting()
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
                        ? Types.Commutation.ModuleCommutationType.Direct
                        : Types.Commutation.ModuleCommutationType.MT3
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
                return;
            }

            profilesList.IsCloseVisible = Visibility.Collapsed;
            profilesList.Items.Refresh();
            tbProfileName.HideTip();



            if (NavigationService != null)
            {
                Cache.ProfileSelection = new ProfileSelectionPage(m_ProfileEngine);
                ClearFilter();
                NavigationService.Navigate(Cache.Technician);
            }

        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
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

            //if (NavigationService != null)
            //{
            //    ClearFilter();
            //    NavigationService.Navigate(Cache.Technician);
            //}

        }

        private void TbProfileName_GotKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E)
        {
            m_OldName = tbProfileName.Text;
        }

        private void TbProfileName_LostKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E)
        {
            var tb = Sender as ValidatingTextBox;
            if (tb == null || !tb.IsFocused)
                return;

            var newName = tb.Text.Trim();

            if (m_OldName != newName && CheckForExistingName(m_ProfileEngine.PlainCollection, newName))
            {
                tb.ShowTip("Profile with the same name is already exist");
                tb.Focus();
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = ButtonOk.IsEnabled = false;
            }
            else if (string.IsNullOrWhiteSpace(newName))
            {
                tb.ShowTip("Input profile name");
                tb.Focus();
            }
            else
            {
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = ButtonOk.IsEnabled = true;
                tb.HideTip();
                m_ProfileEngine.PlainCollection[profilesList.SelectedIndex].Name = newName;

                var item = m_ProfileEngine.PlainCollection[profilesList.SelectedIndex];
                m_ProfileEngine.PlainCollection.RemoveAt(profilesList.SelectedIndex);

                var index =
                    m_ProfileEngine.PlainCollection.TakeWhile(
                        Item => Comparer<string>.Default.Compare(Item.Name, newName) < 0).Count();

                m_ProfileEngine.PlainCollection.Insert(index, item);
                profilesList.SelectedIndex = index;
                profilesList.ScrollIntoView(profilesList.SelectedItem);
            }
        }

        private void TbProfileName_OnKeyUp(object sender, KeyEventArgs e)
        {
            var textBox = sender as ValidatingTextBox;
            if (textBox == null || m_ProfileEngine == null)
                return;
            var name = textBox.Text.Trim();
            if (CheckForExistingName(m_ProfileEngine.PlainCollection, name) && name != m_ProfileEngine.PlainCollection[profilesList.SelectedIndex].Name)
            {
                textBox.ShowTip("Profile with the same name is already exist");
                textBox.Focus();
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = ButtonOk.IsEnabled = false;
            }
            else
            {
                btnAdd.IsEnabled = tabControl.IsEnabled = FilterTextBox.IsEnabled = profilesList.IsEnabled = ButtonOk.IsEnabled = true;
                textBox.HideTip();
            }

        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ButtonAddTestParameters_OnClick(object sender, RoutedEventArgs e)
        {
            if (ComboBoxParametersType.SelectedItem == null)
                return;
            ClearFilter();
            var type = ComboBoxParametersType.SelectedItem.ToString();
            var item = m_ProfileEngine.PlainCollection[profilesList.SelectedIndex];
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

                item.TestParametersAndNormatives.Add(new GateTestParameters { Order = order + 1, IsEnabled = true});
            }
            else if (type.Contains("BVT"))
            {
                item.TestParametersAndNormatives.Add(new BvtTestParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("VTM"))
            {
                item.TestParametersAndNormatives.Add(new VtmTestParameters { Order = order + 1, IsEnabled = true });
            }
            else if (type.Contains("DvDt"))
            {
                item.TestParametersAndNormatives.Add(new DvDtParameters { Order = order + 1, IsEnabled = true });
            }
            else
            {
                item.IsHeightMeasureEnabled = true;
            }
            InitFilter();
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

            }
            item = selectedItem.SelectedItem as DvDtParameters;
            if (item != null)
                tabControl.SelectedIndex = 4;
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_ProfileEngine != null && profilesList != null && profilesList.ItemsSource != null)
                CollectionViewSource.GetDefaultView(profilesList.ItemsSource).Refresh();
        }


    }
}