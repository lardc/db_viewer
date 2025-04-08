using System.Windows;
using SCME.Types;
using SCME.UI.IO;

namespace SCME.UI.PagesCommon
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class HardwareStatusPage
    {
        private bool m_IsBackEnable;
        private bool m_IsRestartEnable;

        public HardwareStatusPage()
        {
            InitializeComponent();

            IsBackEnable = false;
            IsRestartEnable = false;

            btnRestart.Click += Cache.Main.RestartRoutine;
        }

        internal bool IsBackEnable
        {
            get { return m_IsBackEnable; }
            set
            {
                m_IsBackEnable = value;
                btnBack.Visibility = m_IsBackEnable ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        internal bool IsRestartEnable
        {
            get { return m_IsRestartEnable; }
            set
            {
                m_IsRestartEnable = value;
                btnRestart.Visibility = m_IsRestartEnable ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        internal void State(ComplexParts Device, DeviceConnectionState ConnectionState, string Message)
        {
            switch (Device)
            {
                case ComplexParts.FTDI:
                    internalControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.Service:
                    serviceControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.Adapter:
                    adapterControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.Gateway:
                    gatewayControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.Commutation:
                    commutationControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.Gate:
                    gateControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.SL:
                    vtmControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.BVT:
                    bvtControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.Clamping:
                    clampControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.CommutationEx:
                    commutationControlEx.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.DvDt:
                    dVdtControl.SetConnectionStatus(ConnectionState, Message);
                    break;
                case ComplexParts.Sctu:
                    sctuControl.SetConnectionStatus(ConnectionState,Message);
                    break;
            }
        }

        internal bool IsDeviceEnabled(ComplexParts Device)
        {
            switch (Device)
            {
                case ComplexParts.FTDI:
                    return !internalControl.IsDisabled;
                case ComplexParts.Service:
                    return !serviceControl.IsDisabled;
                case ComplexParts.Adapter:
                    return !adapterControl.IsDisabled;
                case ComplexParts.Gateway:
                    return !gatewayControl.IsDisabled;
                case ComplexParts.Commutation:
                    return !commutationControl.IsDisabled;
                case ComplexParts.Gate:
                    return !gateControl.IsDisabled;
                case ComplexParts.SL:
                    return !vtmControl.IsDisabled;
                case ComplexParts.BVT:
                    return !bvtControl.IsDisabled;
                case ComplexParts.Clamping:
                    return !clampControl.IsDisabled;
                case ComplexParts.CommutationEx:
                    return !commutationControlEx.IsDisabled;
                case ComplexParts.DvDt:
                    return !dVdtControl.IsDisabled;
                case ComplexParts.Sctu:
                    return !sctuControl.IsDisabled;
                default:
                    return false;
            }
        }

        internal int GetTimeout(ComplexParts Device)
        {
            switch (Device)
            {
                case ComplexParts.FTDI:
                    return internalControl.OperationTimeout;
                case ComplexParts.Service:
                    return serviceControl.OperationTimeout;
                case ComplexParts.Commutation:
                    return commutationControl.OperationTimeout;
                case ComplexParts.CommutationEx:
                    return commutationControlEx.OperationTimeout;
                case ComplexParts.Adapter:
                    return adapterControl.OperationTimeout;
                case ComplexParts.Gateway:
                    return gatewayControl.OperationTimeout;
                case ComplexParts.Gate:
                    return gateControl.OperationTimeout;
                case ComplexParts.SL:
                    return vtmControl.OperationTimeout;
                case ComplexParts.BVT:
                    return bvtControl.OperationTimeout;
                case ComplexParts.Clamping:
                    return clampControl.OperationTimeout;
                case ComplexParts.DvDt:
                    return dVdtControl.OperationTimeout;
                case ComplexParts.Sctu:
                    return sctuControl.OperationTimeout;
                default:
                    return 10000;
            }
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
                NavigationService.GoBack();
        }
    }
}