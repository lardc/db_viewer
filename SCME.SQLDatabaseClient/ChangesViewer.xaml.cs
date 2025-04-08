using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SCME.SQLDatabaseClient.Annotations;

namespace SCME.SQLDatabaseClient
{
    /// <summary>
    /// Interaction logic for ChangesViewer.xaml
    /// </summary>
    public partial class ChangesViewer : Window, INotifyPropertyChanged
    {
        private string _displayText;

        public string DisplayText
        {
            get { return _displayText; }
            set
            {
                _displayText = value; 
                OnPropertyChanged();
            }
        }

        public ChangesViewer(string data)
        {
            InitializeComponent();

            DisplayText = data;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
