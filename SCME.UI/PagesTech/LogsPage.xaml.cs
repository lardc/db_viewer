using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using SCME.Types;
using SCME.UI.Annotations;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for LogsPage.xaml
    /// </summary>
    public partial class LogsPage: INotifyPropertyChanged
    {
        private const int LOG_PORTION_SIZE = 50;

        private ObservableCollection<LogItem> m_Collection;
        private long m_LastLogID;
        private bool m_LoadActive;

        public LogsPage()
        {
            LogItems = new ObservableCollection<LogItem>();

            InitializeComponent();

            listbox.SetBinding(ItemsControl.ItemsSourceProperty,
                        new Binding { ElementName = "logsPage", Path = new PropertyPath("LogItems") });
        }

        public ObservableCollection<LogItem> LogItems
        {
            get { return m_Collection; }
            private set
            {
                m_Collection = value;
                OnPropertyChanged("LogItems");
            }
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
                NavigationService.GoBack();
        }

        private void LogsPage_OnLoaded(object Sender, RoutedEventArgs E)
        {
            m_LastLogID = Int64.MaxValue;

            m_LoadActive = true;
            GridListBox_OnEndOfData(Sender, E);

            if (m_Collection.Count > 0)
                listbox.ScrollIntoView(listbox.Items[0]);
        }

        private void LogsPage_OnUnloaded(object Sender, RoutedEventArgs E)
        {
            m_LoadActive = false;
            LogItems.Clear();
        }

        private void GridListBox_OnEndOfData(object Sender, RoutedEventArgs E)
        {
            LoadDataFragment();
        }

        private void LoadDataFragment()
        {
            if (m_LoadActive)
            {
                try
                {
                    var nextData = Cache.Net.ReadLogsFromLocal(m_LastLogID, LOG_PORTION_SIZE);

                    try
                    {
                        m_LastLogID = nextData.Select(D => D.ID).Min();
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    foreach (var logItem in nextData)
                        LogItems.Add(logItem);
                }
                catch (FaultException<FaultData>)
                {
                }
            }
            else
                Application.Current.Dispatcher.Invoke(
                    DispatcherPriority.Background,
                    new ThreadStart(delegate { }));
        }

        #region Interface INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string PropertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
                    handler(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion
    }
}