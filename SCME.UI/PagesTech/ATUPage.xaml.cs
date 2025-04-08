using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.ATU;
using SCME.UI.Properties;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for ATUPage.xaml
    /// </summary>
    public partial class ATUPage : Page
    {
        private bool m_IsRunning;

        public TestParameters Parameters { get; set; }
        public Types.Clamping.TestParameters ClampParameters { get; set; }
        public Types.Commutation.ModuleCommutationType CommType { get; set; }
        public Types.Commutation.ModulePosition ModPosition { get; set; }

        private const int RoomTemp = 25;
        public int Temperature { get; set; }


        internal void SetResultAll(DeviceState State)
        {
            if (State == DeviceState.InProcess)
                ClearStatus();
            else IsRunning = false;
        }

        internal ATUPage()
        {
            Parameters = new Types.ATU.TestParameters { IsEnabled = true };
            ClampParameters = new Types.Clamping.TestParameters { StandardForce = Types.Clamping.ClampingForceInternal.Custom, CustomForce = 5 };
            CommType = Settings.Default.SinglePositionModuleMode ? Types.Commutation.ModuleCommutationType.Direct : Types.Commutation.ModuleCommutationType.MT3;
            Temperature = RoomTemp;

            InitializeComponent();

            ClearStatus();
        }

        private void ResetLabel(ContentControl Target)
        {
            Target.Content = "";
            Target.Background = Brushes.Transparent;
        }

        internal void ClearMeasureResult()
        //очистка label для вывода измеренных результатов
        {
            ResetLabel(lbAtuUBR);
            ResetLabel(lbAtuUPRSM);
            ResetLabel(lbAtuIPRSM);
            ResetLabel(lbAtuPRSM);
        }

        private void ClearStatus()
        {
            lbAtuWarning.Visibility = Visibility.Collapsed;
            lbAtuFaultReason.Visibility = Visibility.Collapsed;

            ClearMeasureResult();
        }

        internal bool IsRunning
        {
            get { return m_IsRunning; }

            set
            {
                m_IsRunning = value;
                btnStart.IsEnabled = !m_IsRunning;
                btnBack.IsEnabled = !m_IsRunning;
            }
        }

        internal void SetResult(DeviceState State, Types.ATU.TestResults Result)
        {
            if (State == DeviceState.InProcess)
            {
                ClearStatus();
            }
            else
            {
                IsRunning = false;
                SetLabel(lbAtuUBR, State, true, Result.UBR.ToString());
                SetLabel(lbAtuUPRSM, State, true, Result.UPRSM.ToString());
                SetLabel(lbAtuIPRSM, State, true, String.Format("{0:0.00}", Result.IPRSM)); //формат вывода - 2 знака после запятой
                SetLabel(lbAtuPRSM, State, true, String.Format("{0:0.00}", Result.PRSM));   //формат вывода - 2 знака после запятой
            }
        }

        internal void SetColorByWarning(ushort Warning)
        {
            //установка цвета lbAtuWarning в зависимости от принятого кода Warning
            switch (Warning)
            {
                //будем привлекать внимание оператора с помощью выделения сообщения цветом
                case (ushort)Types.ATU.HWWarningReason.Idle:
                case (ushort)Types.ATU.HWWarningReason.FacetBreak:
                    lbAtuWarning.Background = Brushes.Orange;
                    break;

                case (ushort)Types.ATU.HWWarningReason.BreakDUT:
                case (ushort)Types.ATU.HWWarningReason.Short:               
                    lbAtuWarning.Background = (SolidColorBrush)FindResource("xRed1");
                    break;

                default:
                    lbAtuWarning.Background = Brushes.Gold;
                    break;
            }
        }

        internal void SetWarning(ushort Warning)
        {
            //закрашиваем цветом поле вывода Warning, чтобы обратить на него внимание оператора
            SetColorByWarning(Warning);

            Types.ATU.HWWarningReason WarningReason = (Types.ATU.HWWarningReason)Warning;
            lbAtuWarning.Content = WarningReason.ToString();
            lbAtuWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(ushort Fault)
        {
            Types.ATU.HWFaultReason FaultReason = (Types.ATU.HWFaultReason)Fault;
            lbAtuFaultReason.Content = FaultReason.ToString();
            lbAtuFaultReason.Visibility = Visibility.Visible;
            IsRunning = false;
        }

        private static void SetLabel(ContentControl Target, DeviceState State, bool IsFitWithNormatives, string Value)
        {
            switch (State)
            {
                case DeviceState.None:
                    Target.Content = "";
                    Target.Background = Brushes.Transparent;
                    break;

                case DeviceState.InProcess:
                    Target.Background = Brushes.Gold;
                    break;

                case DeviceState.Success:
                    Target.Content = Value;
                    Target.Background = IsFitWithNormatives ? Brushes.LightGreen : Brushes.LightPink;
                    break;

                case DeviceState.Stopped:
                    Target.Content = Properties.Resources.Stopped;
                    Target.Background = Brushes.LightGreen;
                    break;

                case DeviceState.Problem:
                    Target.Content = Value;
                    Target.Background = Brushes.Gold;
                    break;

                case DeviceState.Fault:
                    Target.Content = Properties.Resources.Fault;
                    Target.Background = Brushes.Tomato;
                    break;
            }
        }

        internal void Start()
        {
            if (IsRunning) return;

            var paramGate = new Types.Gate.TestParameters { IsEnabled = false };
            var paramVtm = new Types.SL.TestParameters { IsEnabled = false };
            var paramBvt = new Types.BVT.TestParameters { IsEnabled = false };
            var paramQrrTq = new Types.QrrTq.TestParameters { IsEnabled = false };
            var paramRAC = new Types.RAC.TestParameters { IsEnabled = false };
            var paramIH = new Types.IH.TestParameters { IsEnabled = false };
            var paramRCC = new Types.RCC.TestParameters { IsEnabled = false };

            //если пресс был зажат вручную - не стоит пробовать зажимать его ещё раз
            ClampParameters.SkipClamping = Cache.Clamp.ManualClamping;

            if (!Cache.Net.Start(paramGate, paramVtm, paramBvt, Parameters, paramQrrTq, paramRAC, paramIH, paramRCC,
                                 new Types.Commutation.TestParameters
                                 {
                                     BlockIndex = (!Cache.Clamp.clampPage.UseTmax) ? Types.Commutation.HWBlockIndex.Block1 : Types.Commutation.HWBlockIndex.Block2,
                                     CommutationType = ConverterUtil.MapCommutationType(CommType),
                                     Position = ConverterUtil.MapModulePosition(ModPosition)
                                 }, ClampParameters))
                return;

            ClearStatus();
            IsRunning = true;
        }

        private void btnBack_OnClick(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null) NavigationService.GoBack();
        }

        private void btnTemperature_OnClick(object sender, RoutedEventArgs e)
        {
            Cache.Net.StartHeating(Temperature);
        }

        private void btnStart_OnClick(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void btnStop_OnClick(object Sender, RoutedEventArgs E)
        {
            Cache.Net.StopByButtonStop();
        }

        public void SetTopTemp(int temperature)
        {
            TopTempLabel.Content = temperature;

            var bottomTemp = Temperature - 2;
            var topTemp = Temperature + 2;

            if (temperature < bottomTemp || temperature > topTemp)
            {
                TopTempLabel.Background = Brushes.Tomato;
            }
            else
            {
                TopTempLabel.Background = Brushes.LightGreen;
            }
        }

        public void SetBottomTemp(int temperature)
        {
            BotTempLabel.Content = temperature;

            var bottomTemp = Temperature - 2;
            var topTemp = Temperature + 2;

            if (temperature < bottomTemp || temperature > topTemp)
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
