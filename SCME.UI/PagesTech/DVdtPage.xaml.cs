using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.dVdt;
using SCME.UI.Properties;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for ClampPage.xaml
    /// </summary>
    public partial class DVdtPage : Page
    {
        private bool m_IsRunning;

        public TestParameters Parameters { get; set; }

        public Types.Clamping.TestParameters ClampParameters { get; set; }
        private const int RoomTemp = 25;
        public int Temperature { get; set; }

        public Types.Commutation.ModuleCommutationType CommType { get; set; }
        public Types.Commutation.ModulePosition ModPosition { get; set; }

        public DVdtPage()
        {
            ClampParameters = new Types.Clamping.TestParameters
            {
                StandardForce = Types.Clamping.ClampingForceInternal.Custom,
                CustomForce = 5.0f,
                IsHeightMeasureEnabled = false
            };
            Temperature = RoomTemp;
            Parameters = new TestParameters() { IsEnabled = true, Mode = DvdtMode.Detection };
            CommType = Settings.Default.SinglePositionModuleMode ? Types.Commutation.ModuleCommutationType.Direct : Types.Commutation.ModuleCommutationType.MT3;
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
            labelResult.Content = "";
            labelVoltageRate.Content = "";
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

        private static void SetLabel(ContentControl Target, DeviceState State, bool IsFitWithNormatives, string Value)
        {
            switch (State)
            {
                case DeviceState.InProcess:
                    Target.Background = Brushes.Gold;
                    break;
                case DeviceState.Stopped:
                    Target.Content = Properties.Resources.Stopped;
                    Target.Background = Brushes.LightGreen;
                    break;
                case DeviceState.Success:
                    Target.Content = Value;
                    Target.Background = IsFitWithNormatives ? Brushes.LightGreen : Brushes.LightPink;
                    break;
                case DeviceState.Problem:
                    Target.Content = Value;
                    Target.Background = Brushes.Gold;
                    break;
                case DeviceState.Fault:
                    Target.Content = Properties.Resources.Fault;
                    Target.Background = Brushes.Tomato;
                    break;
                case DeviceState.None:
                    Target.Content = "";
                    Target.Background = Brushes.Transparent;
                    break;
            }
        }

        internal void SetResult(DeviceState State, Types.dVdt.TestResults Result)
        {
            if (State != DeviceState.InProcess)
            {
                IsRunning = false;
                SetLabel(labelResult, State, Result.Passed, Result.Passed ? "OK" : "NotOk");
                SetLabel(labelVoltageRate, State, true, Result.VoltageRate.ToString());
            }
        }

        private void Stop_Click(object Sender, RoutedEventArgs E)
        {
            Cache.Net.StopByButtonStop();
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
                NavigationService.GoBack();
        }

        private void BtnStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (IsRunning)
                return;

            //если пресс был зажат вручную - не стоит пробовать зажимать его ещё раз
            ClampParameters.SkipClamping = Cache.Clamp.ManualClamping;

            var commPar = new Types.Commutation.TestParameters()
            {
                BlockIndex = (!Cache.Clamp.UseTmax) ? Types.Commutation.HWBlockIndex.Block1 : Types.Commutation.HWBlockIndex.Block2,
                                         CommutationType = ConverterUtil.MapCommutationType(CommType),
                                         Position = ConverterUtil.MapModulePosition(ModPosition)
            };
           
            var parameters = new List<BaseTestParametersAndNormatives>(1);
            parameters.Add(Parameters);

            if (!Cache.Net.Start(commPar, ClampParameters, parameters))
                return;
            IsRunning = true;
        }

        private void BtnTemp_OnClick(object sender, RoutedEventArgs e)
        {
            Cache.Net.StartHeating(Temperature);
        }

        public void SetTopTemp(int temeprature)
        {
            TopTempLabel.Content = temeprature;
            var bottomTemp = Temperature - 2;
            var topTemp = Temperature + 2;
            if (temeprature < bottomTemp || temeprature > topTemp)
            {
                TopTempLabel.Background = Brushes.Tomato;
            }
            else
            {
                TopTempLabel.Background = Brushes.LightGreen;
            }
        }

        public void SetBottomTemp(int temeprature)
        {
            BotTempLabel.Content = temeprature;
            var bottomTemp = Temperature - 2;
            var topTemp = Temperature + 2;
            if (temeprature < bottomTemp || temeprature > topTemp)
            {
                BotTempLabel.Background = Brushes.Tomato;
            }
            else
            {
                BotTempLabel.Background = Brushes.LightGreen;
            }
        }
    }
}
