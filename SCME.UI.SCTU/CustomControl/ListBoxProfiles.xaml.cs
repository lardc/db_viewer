using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SCME.Types.Profiles;

namespace SCME.UI.CustomControl
{
    /// <summary>
    /// Interaction logic for CListBox.xaml
    /// </summary>
    public partial class ListBoxProfiles : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String Info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Info));
        }

        public ListBoxProfiles()
        {
            IsEditable = false;
            InitializeComponent();

            IsCloseVisible = Visibility.Collapsed;
        }

        public bool IsEditable { get; set; }

        private Visibility m_IsCloseVisible = Visibility.Collapsed;

        public Visibility IsCloseVisible
        {
            get { return m_IsCloseVisible; }
            set { m_IsCloseVisible = value; }
        }

        private void MouseRightButtonDownHandler(object Sender, MouseEventArgs E)
        {
            if (!IsEditable)
                return;

            switch (IsCloseVisible)
            {
                case Visibility.Collapsed:
                    IsCloseVisible = Visibility.Visible;
                    break;
                case Visibility.Visible:
                    IsCloseVisible = Visibility.Collapsed;
                    break;
            }

            NotifyPropertyChanged("IsCloseVisible");
        }

        private void Btn_Click(object Sender, RoutedEventArgs E)
        {
            var button = Sender as Button;
            if (button == null)
                return;

            var lbi = button.CommandParameter as ListBoxItem;
            if (lbi == null)
                return;

            var profile = lbi.Content as Profile;
            if (profile == null)
                return;

            var re = new RemoveItemEventArgs {Profile = profile};
            var handler = RemoveItem;
            if (handler != null)
                handler(this, re);

            if (!re.Cancel)
            {
                var collection = ItemsSource as ObservableCollection<Profile>;

                if (collection != null)
                    collection.Remove(profile);
            }
        }

        public class RemoveItemEventArgs : EventArgs
        {
            public Profile Profile { get; set; }
            public bool Cancel { get; set; }
        }

        public event EventHandler<RemoveItemEventArgs> RemoveItem;
    }
}