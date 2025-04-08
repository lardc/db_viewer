using System.Windows;
using System.Windows.Controls;
using SCME.Types;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for ClampPage.xaml
    /// </summary>
    public partial class DVdtPage : Page
    {
        private bool m_IsRunning;

        public DVdtPage()
        {
            InitializeComponent();

            ClearStatus();
        }

        internal bool IsRunning
        {
            get
            {
                return m_IsRunning;
            }
            set
            {
                m_IsRunning = value;
                btnBack.IsEnabled = !m_IsRunning;
            }
        }

        private void ClearStatus()
        {
            lblWarning.Visibility = Visibility.Collapsed;
            lblFault.Visibility = Visibility.Collapsed;
        }

        internal void SetWarning(Types.dVdt.HWWarningReason Warning)
        {
            lblWarning.Content = Warning.ToString();
            lblWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(Types.dVdt.HWFaultReason Fault)
        {
            lblFault.Content = Fault.ToString();
            lblFault.Visibility = Visibility.Visible;
            IsRunning = false;
        }

        internal void SetResult(DeviceState State, Types.dVdt.TestResults Result)
        {
            IsRunning = false;
        }

        private void Stop_Click(object Sender, RoutedEventArgs E)
        {
            Cache.Net.Stop();
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
                NavigationService.GoBack();
        }
    }
}
