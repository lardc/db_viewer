using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SCME.Types;
using SCME.Types.Profiles;

namespace SCME.UI.PagesUser
{
    /// <summary>
    /// Interaction logic for UserPage.xaml
    /// </summary>
    public partial class ProfileSelectionPage
    {
        public ProfileSelectionPage(ProfileDictionary ProEngine)
        {
            InitializeComponent();

            profilesList.Items.Clear();

            profilesList.ItemsSource = ProEngine.PlainCollection;
            
            profilesList.Items.Refresh();

            if (profilesList.Items.Count > 0)
                profilesList.SelectedIndex = 0;
           
        }

        public void InitSorting()
        {
            var collectionView = CollectionViewSource.GetDefaultView(TestParameters.ItemsSource);
            if(collectionView != null)
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
            btnGoNext.Visibility = Param.IsGateEnabled || Param.IsSLEnabled || Param.IsBVTEnabled
                                       ? Visibility.Visible
                                       : Visibility.Hidden;
        }

        private void Next_Click(object sender, RoutedEventArgs E)
        {
            var profile = profilesList.SelectedItem as Profile;
            if (profile == null) return;

            Cache.UserTest.Profile = profile;
            if (NavigationService == null) return;
            Cache.UserTest.InitSorting();
            Cache.UserTest.InitTemp();
            NavigationService.Navigate(Cache.UserTest);
        }

        private void Results_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
                NavigationService.Navigate(Cache.Results);
        }


        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (profilesList != null && profilesList.ItemsSource != null)
                CollectionViewSource.GetDefaultView(profilesList.ItemsSource).Refresh();
        }

        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(FilterTextBox.Text)||FilterTextBox.Text == "Поиск")
                return true;
            else
                return ((item as Profile).Name.IndexOf(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }


        private void ProfilesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InitSorting();
        }
    }
}