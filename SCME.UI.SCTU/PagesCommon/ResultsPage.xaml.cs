using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SCME.Types;
using SCME.UI.Annotations;

namespace SCME.UI.PagesCommon
{
    /// <summary>
    /// Interaction logic for ResultsPage.xaml
    /// </summary>
    public partial class ResultsPage : INotifyPropertyChanged
    {
        private ObservableCollection<string> m_GroupCollection;
        private ReportSelectionPredicate m_ReportSelection;
        private string m_FieldCustomer;
        private string m_FieldDeviceType;
        private bool m_Active;

        public ResultsPage()
        {
            InitializeComponent();
        }
        
        public string FieldCustomer
        {
            get { return m_FieldCustomer; }
            set
            {
                m_FieldCustomer = value;
                OnPropertyChanged("FieldCustomer");
            }
        }

        public string FieldDeviceType
        {
            get { return m_FieldDeviceType; }
            set { m_FieldDeviceType = value;
                OnPropertyChanged("FieldDeviceType");
            }
        }
       
        public ObservableCollection<string> GroupCollection
        {
            get { return m_GroupCollection; }
            private set
            {
                m_GroupCollection = value;
                OnPropertyChanged("GroupCollection");
            }
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
                NavigationService.GoBack();
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string PropertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion

        private void Print_Click(object Sender, RoutedEventArgs E)
        {
            if(lblGroup.Content == null)
                return;

            var group = lblGroup.Content.ToString();

            if (String.IsNullOrWhiteSpace(group))
                return;

            lblStatus.Content = Properties.Resources.Pending;
            lblStatus.Content = Cache.Net.RequestRemotePrinting(group,
                String.IsNullOrEmpty(FieldCustomer) ? "Proton" : FieldCustomer, FieldDeviceType,
                m_ReportSelection)
                ? Properties.Resources.Successful
                : Properties.Resources.Failed;
        }

        private void TgbAll_Cheched(object Sender, RoutedEventArgs E)
        {
            if (tgbPassed != null)
                tgbPassed.IsChecked = false;
            if (tgbRejected != null)
                tgbRejected.IsChecked = false;

            m_ReportSelection = ReportSelectionPredicate.All;
        }

        private void TgbPassed_Checked(object Sender, RoutedEventArgs E)
        {
            if (tgbAll != null)
                tgbAll.IsChecked = false;
            if (tgbRejected != null)
                tgbRejected.IsChecked = false;

            m_ReportSelection = ReportSelectionPredicate.QC;
        }

        private void TgbRejected_Checked(object Sender, RoutedEventArgs E)
        {
            if (tgbAll != null)
                tgbAll.IsChecked = false;
            if (tgbPassed != null)
                tgbPassed.IsChecked = false;

            m_ReportSelection = ReportSelectionPredicate.Rejected;
        }

        private void ResultsPage_OnLoaded(object Sender, RoutedEventArgs E)
        {
            m_Active = true;
            lblStatus.Content = Properties.Resources.Waiting;
            tgbAll.IsChecked = true;
            FieldCustomer = "";
            FieldDeviceType = "";

            cbPeriod.SelectedIndex = 0;
            CbPeriod_OnSelectionChanged(Sender, null);
        }

        private void CbPeriod_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            if (m_Active)
            {
                var from = DateTime.Today;

                switch (cbPeriod.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        from = from.AddDays(-1);
                        break;
                    case 2:
                        from = from.AddDays(-7);
                        break;
                    case 3:
                        from = from.AddMonths(-1);
                        break;
                }

                try
                {
                     GroupCollection = new ObservableCollection<string>(Cache.Net.ReadGroupsFromServer(from, DateTime.Now));
                }
                catch (Exception)
                {
                    GroupCollection = new ObservableCollection<string>(Cache.Net.ReadGroupsFromLocal(from, DateTime.Now));
                }
               
            }
        }

        private void LbGroupList_SelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            lblStatus.Content = Properties.Resources.Waiting;

            if (lbGroupList.SelectedItem != null)
            {
                
                DeviceItem firstDevice;

                try
                {
                    firstDevice = Cache.Net.ReadDevicesFromServer(lbGroupList.SelectedItem.ToString()).FirstOrDefault();
                }
                catch (Exception)
                {
                    firstDevice = Cache.Net.ReadDevicesFromLocal(lbGroupList.SelectedItem.ToString()).FirstOrDefault();
                }
                

                if (firstDevice != null)
                    FieldDeviceType = firstDevice.ProfileName;
            }
        }
    }
}