using System;
using System.Globalization;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SCME.Types;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for ConsolePage.xaml
    /// </summary>
    public partial class ConsolePage
    {
        private readonly Brush m_TbGradientBrush;
        private int m_Index;

        public ConsolePage()
        {
            InitializeComponent();

            lblTitle.Content = "";
            lblError.Text = "";
            m_TbGradientBrush = tbCallAddress.BorderBrush;
        }

        internal void AreButtonEnabled(TypeCommon.InitParams Param)
        {
            btnGate.IsEnabled = Param.IsGateEnabled;
            btnVtm.IsEnabled = Param.IsSLEnabled;
            btnBvt.IsEnabled = Param.IsBVTEnabled;
            btnClamp.IsEnabled = Param.IsClampEnabled;
            btndVdt.IsEnabled = Param.IsdVdtEnabled;
            btnAtu.IsEnabled = Param.IsATUEnabled;
            btnQrrTq.IsEnabled = Param.IsQrrTqEnabled;
            btnRAC.IsEnabled = Param.IsRACEnabled;
        }

        private void TabControl_SelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            switch (m_Index)
            {
                case 1:
                    lblTitle.Content = Properties.Resources.Gateway;
                    break;
                case 2:
                    lblTitle.Content = Properties.Resources.Commutation;
                    break;
                case 3:
                    lblTitle.Content = Properties.Resources.Gate;
                    break;
                case 4:
                    lblTitle.Content = Properties.Resources.Vtm;
                    break;
                case 5:
                    lblTitle.Content = Properties.Resources.Bvt;
                    break;
                case 6:
                    lblTitle.Content = Properties.Resources.Commutation;
                    break;
                case 7:
                    lblTitle.Content = Properties.Resources.Clamp;
                    break;
                case 8:
                    lblTitle.Content = Properties.Resources.dVdt;
                    break;
                case 9:
                    lblTitle.Content = Properties.Resources.ATU;
                    break;
                case 10:
                    lblTitle.Content = Properties.Resources.QrrTq;
                    break;
                case 11:
                    lblTitle.Content = Properties.Resources.RAC;
                    break;
            }
        }

        private void Button_Click(object Sender, RoutedEventArgs E)
        {
            var btn = Sender as Button;
            if (btn == null)
                return;

            lblReadValue.Content = "";
            tbReadAddress.Text = "0";
            tbWriteAddress.Text = "0";
            tbWriteValue.Text = "0";
            tbCallAddress.Text = "0";

            m_Index = Convert.ToInt16(btn.CommandParameter);
            tabControl.SelectedIndex = m_Index == 0 ? 0 : 1;
        }

        private void ClearBorderBrush()
        {
            tbReadAddress.BorderBrush = m_TbGradientBrush;
            tbWriteAddress.BorderBrush = m_TbGradientBrush;
            tbWriteValue.BorderBrush = m_TbGradientBrush;
            tbCallAddress.BorderBrush = m_TbGradientBrush;
        }

        private static ComplexParts IndexToDevice(int Index)
        {
            var device = ComplexParts.None;
            switch (Index)
            {
                case 1:
                    device = ComplexParts.Gateway;
                    break;
                case 2:
                    device = ComplexParts.Commutation;
                    break;
                case 3:
                    device = ComplexParts.Gate;
                    break;
                case 4:
                    device = ComplexParts.SL;
                    break;
                case 5:
                    device = ComplexParts.BVT;
                    break;
                case 6:
                    device = ComplexParts.CommutationEx;
                    break;
                case 7:
                    device = ComplexParts.Clamping;
                    break;
                case 8:
                    device = ComplexParts.DvDt;
                    break;
                case 9:
                    device = ComplexParts.ATU;
                    break;
                case 10:
                    device = ComplexParts.QrrTq;
                    break;
                case 11:
                    device = ComplexParts.RAC;
                    break;
            }
            return device;
        }

        private void ShowError(string Error)
        {
            lblError.Text = Error;
        }

        private void Read_Click(object Sender, RoutedEventArgs E)
        {
            lblError.Text = "";
            var device = IndexToDevice(m_Index);
            ClearBorderBrush();
            ushort address;

            if (!ushort.TryParse(tbReadAddress.Text, out address))
            {
                tbReadAddress.BorderBrush = Brushes.Tomato;
                return;
            }

            try
            {
                var value = Cache.Net.ReadRegister(device, address);
                lblReadValue.Content = value.ToString(CultureInfo.InvariantCulture);
            }
            catch (FaultException<FaultData> ex)
            {
                ShowError(string.Format("{0}. {1} : {2}", ex.Detail.Device, ex.Message, ex.Detail.Message));
            }
        }

        private void Write_Click(object Sender, RoutedEventArgs E)
        {
            lblError.Text = "";
            var device = IndexToDevice(m_Index);
            ushort address, value;

            ClearBorderBrush();

            if (!ushort.TryParse(tbWriteAddress.Text, out address))
            {
                tbWriteAddress.BorderBrush = Brushes.Tomato;
                return;
            }

            if (!ushort.TryParse(tbWriteValue.Text, out value))
            {
                tbWriteValue.BorderBrush = Brushes.Tomato;
                return;
            }

            try
            {
                Cache.Net.WriteRegister(device, address, value);
            }
            catch (FaultException<FaultData> ex)
            {
                ShowError(string.Format("{0}. {1} : {2}", ex.Detail.Device, ex.Message, ex.Detail.Message));
            }
        }

        private void Call_Click(object Sender, RoutedEventArgs E)
        {
            lblError.Text = "";
            var device = IndexToDevice(m_Index);
            ushort address;

            ClearBorderBrush();

            if (!ushort.TryParse(tbCallAddress.Text, out address))
            {
                tbCallAddress.BorderBrush = Brushes.Tomato;
                return;
            }

            try
            {
                Cache.Net.CallAction(device, address);
            }
            catch (FaultException<FaultData> ex)
            {
                ShowError(string.Format("{0}. {1} : {2}", ex.Detail.Device, ex.Message, ex.Detail.Message));
            }
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (tabControl.SelectedIndex != 0)
            {
                m_Index = 0;
                tabControl.SelectedIndex = m_Index;
            }
            else if (NavigationService != null)
                NavigationService.GoBack();
        }
    }
}