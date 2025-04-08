using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SCME.ProfileBuilder.CustomControl;
using SCME.Types.DatabaseServer;
using SCME.Types.DataContracts;

namespace SCME.ProfileBuilder.PagesTech
{
    /// <summary>
    /// Interaction logic for Connections.xaml
    /// </summary>
    public partial class Connections
    {
        private readonly IProfilesConnectionService _service;
        public readonly ObservableCollection<MmeCode> MmeCodes;
        
        public Connections(IProfilesConnectionService service)
        {
            InitializeComponent();
            _service = service;

            var mmeCodes = _service.GetMmeCodes();
            MmeCodes = new ObservableCollection<MmeCode>(mmeCodes);
            ListViewMmeCodes.ItemsSource = MmeCodes;

            if (MmeCodes.Count <= 0)
                return;

            ListViewMmeCodes.SelectedIndex = 0;
            InitFilter();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
            {
                NavigationService.Navigate(Cache.ProfileEdit);
            }
        }

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            Cache.Main.mainFrame.IsEnabled = false;
            _service.SaveConnections(MmeCodes.ToList());
            Cache.Main.mainFrame.IsEnabled = true;
        }

        private void ListViewMmeCodes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InitFilter();
        }

        private void InitFilter()
        {
            if (ListViewProfiles != null && ListViewProfiles.ItemsSource != null)
            {
                var collectionView = CollectionViewSource.GetDefaultView(ListViewProfiles.ItemsSource);
                collectionView.Filter = UserFilter;
            }
        }

        private bool UserFilter(object obj)
        {
            if (string.IsNullOrEmpty(FilterTextBox.Text) || FilterTextBox.Text == "Поиск")
                return true;

            var profileMme = obj as ProfileMme;
            return (profileMme != null) && (profileMme.Name.IndexOf(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ListViewProfiles?.ItemsSource != null)
                CollectionViewSource.GetDefaultView(ListViewProfiles.ItemsSource).Refresh();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            const string name = @"MME";

            bool exists;
            var i = 0;

            do
            {
                i++;
                exists = CheckForExistingName(MmeCodes, name + i.ToString("D3"));
            } while (exists);

            var dialog = new InsertNameDialog("Введите название КИП", name + i.ToString("D3"));
            var result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                var newMme = new MmeCode { Name = dialog.tbMessage.Text };

                var index =
                   MmeCodes.TakeWhile(
                       itm => Comparer<string>.Default.Compare(itm.Name, newMme.Name) < 0).Count();

                newMme.ProfileMmes = _service.GetMmeProfiles(0).ToList();
                MmeCodes.Insert(index, newMme);

                var item = MmeCodes[index];
                ListViewMmeCodes.SelectedIndex = index;
                ListViewMmeCodes.ScrollIntoView(item);
            }
        }

        public static bool CheckForExistingName(IEnumerable<MmeCode> mmeCodes, string name)
        {
            return mmeCodes.Any(T => T.Name == name);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null)
                return;

            var item = menuItem.DataContext as MmeCode;
            MmeCodes.Remove(item);
        }

        private void ButtonCheckAll_OnClick(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(ListViewProfiles.ItemsSource);
            view.Filter = UserFilter;

            foreach (ProfileMme o in view)
                o.IsSelected = true;
        }

        private void ButtonUnCheckAll_OnClick(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(ListViewProfiles.ItemsSource);
            view.Filter = UserFilter;

            foreach (ProfileMme o in view)
                o.IsSelected = false;
        }
    }
}
