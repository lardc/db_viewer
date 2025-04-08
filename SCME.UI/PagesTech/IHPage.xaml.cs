using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SCME.UI.Properties;
using SCME.Types;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for IHPage.xaml
    /// </summary>
    public partial class IHPage : Page
    {
        private bool m_IsRunning;

        public Types.IH.TestParameters Parameters { get; set; }
        public Types.Clamping.TestParameters ClampParameters { get; set; }
        public Types.Commutation.ModuleCommutationType CommType { get; set; }
        public Types.Commutation.ModulePosition ModPosition { get; set; }

        private const int RoomTemp = 25;
        private const int TIME_STEP = 5;

        public int Temperature { get; set; }

        public IHPage()
        {
            Parameters = new Types.IH.TestParameters { IsEnabled = true };
            ClampParameters = new Types.Clamping.TestParameters { StandardForce = Types.Clamping.ClampingForceInternal.Custom, CustomForce = 5 };
            CommType = Settings.Default.SinglePositionModuleMode ? Types.Commutation.ModuleCommutationType.Direct : Types.Commutation.ModuleCommutationType.MT3;
            Temperature = RoomTemp;

            InitializeComponent();

            ClearStatus();
        }

        private void ClearStatus()
        {
            lbWarning.Visibility = Visibility.Collapsed;
            lbFaultReason.Visibility = Visibility.Collapsed;
            lbProblem.Visibility = Visibility.Collapsed;

            ClearMeasureResult();
        }

        private void ResetLabel(ContentControl Target)
        {
            Target.Content = "";
            Target.Background = Brushes.Transparent;
        }

        internal void ClearMeasureResult()
        //очистка label для вывода измеренных результатов
        {
            ResetLabel(lbIh);
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

        internal void SetResultAll(DeviceState State)
        {
            if (State == DeviceState.InProcess)
                ClearStatus();
            else IsRunning = false;
        }

        internal void SetResult(DeviceState State, Types.IH.TestResults Result)
        {
            if (State == DeviceState.InProcess)
            {
                ClearStatus();
            }
            else
            {
                IsRunning = false;
                SetLabel(lbIh, State, true, Result.Ih.ToString());
            }
        }

        internal void SetColorByProblem(ushort Problem)
        {
            //установка цвета lbProblem в зависимости от принятого кода Problem
            switch (Problem)
            {
                //будем привлекать внимание оператора с помощью выделения сообщения цветом
                case (ushort)Types.IH.HWProblemReason.None:
                    lbProblem.Background = Brushes.Transparent;
                    break;

                default:
                    lbProblem.Background = Brushes.LightPink;
                    break;
            }
        }

        internal void SetColorByWarning(ushort Warning)
        {
            //установка цвета lbWarning в зависимости от принятого кода Warning
            switch (Warning)
            {
                //будем привлекать внимание оператора с помощью выделения сообщения цветом
                case (ushort)Types.IH.HWWarningReason.None:
                    lbWarning.Background = Brushes.Transparent;
                    break;

                default:
                    lbWarning.Background = (SolidColorBrush)FindResource("xRed1");
                    break;
            }
        }

        internal void SetProblem(ushort Problem)
        {
            SetColorByProblem(Problem);

            Types.IH.HWProblemReason ProblemReason = (Types.IH.HWProblemReason)Problem;
            lbProblem.Content = "Problem " + ProblemReason.ToString();

            lbProblem.Visibility = Visibility.Visible;
        }

        internal void SetWarning(ushort Warning)
        {
            //закрашиваем цветом поле вывода Warning, чтобы обратить на него внимание оператора
            SetColorByWarning(Warning);

            Types.IH.HWWarningReason WarningReason = (Types.IH.HWWarningReason)Warning;
            lbWarning.Content = "Warning " + WarningReason.ToString();

            lbWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(ushort Fault)
        {
            Types.IH.HWFaultReason FaultReason = (Types.IH.HWFaultReason)Fault;

            lbFaultReason.Content = "Fault " + FaultReason.ToString();
            lbFaultReason.Visibility = Visibility.Visible;
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
            if (IsRunning)
                return;

            var paramGate = new Types.Gate.TestParameters { IsEnabled = false };
            var paramVtm = new Types.SL.TestParameters { IsEnabled = false };
            var paramBvt = new Types.BVT.TestParameters { IsEnabled = false };
            var paramATU = new Types.ATU.TestParameters { IsEnabled = false };
            var paramQrrTq = new Types.QrrTq.TestParameters { IsEnabled = false };
            var paramRAC = new Types.RAC.TestParameters { IsEnabled = false };
            var paramRCC = new Types.RCC.TestParameters { IsEnabled = false };

            //если пресс был зажат вручную - не стоит пробовать зажимать его ещё раз
            ClampParameters.SkipClamping = Cache.Clamp.ManualClamping;

            if (!Cache.Net.Start(paramGate, paramVtm, paramBvt, paramATU, paramQrrTq, paramRAC, Parameters, paramRCC,
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

        private void btnBack_OnClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null) NavigationService.GoBack();
        }

        private void btnTemperature_OnClick(object sender, RoutedEventArgs e)
        {
            Cache.Net.StartHeating(Temperature);
        }

        private void btnStop_OnClick(object sender, RoutedEventArgs e)
        {
            Cache.Net.StopByButtonStop();
        }

        private void btnStart_OnClick(object sender, RoutedEventArgs e)
        {
            Start();
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
