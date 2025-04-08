using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using SCME.Types;
using SCME.Types.QrrTq;
using SCME.UI.Properties;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Drawing;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for QrrTqPage.xaml
    /// </summary>
    public partial class QrrTqPage : Page
    {
        private bool m_IsRunning;

        public Types.QrrTq.TestParameters Parameters { get; set; }
        public Types.Clamping.TestParameters ClampParameters { get; set; }
        public Types.Commutation.ModuleCommutationType CommType { get; set; }
        public Types.Commutation.ModulePosition ModPosition { get; set; }
        private readonly SolidColorBrush m_XGreen, m_XOrange;

        private const int RoomTemp = 25;
        private const int TIME_STEP = 5;

        public int Temperature { get; set; }

        public QrrTqPage()
        {
            Parameters = new Types.QrrTq.TestParameters { IsEnabled = true };
            ClampParameters = new Types.Clamping.TestParameters { StandardForce = Types.Clamping.ClampingForceInternal.Custom, CustomForce = 5 };
            CommType = Settings.Default.SinglePositionModuleMode ? Types.Commutation.ModuleCommutationType.Direct : Types.Commutation.ModuleCommutationType.MT3;
            Temperature = RoomTemp;

            InitializeComponent();

            m_XGreen = (SolidColorBrush)FindResource("xGreen1");
            m_XOrange = (SolidColorBrush)FindResource("xOrange1");

            ClearStatus();
        }

        public class DisplayRange
        {
            public double Start { get; set; }
            public double End { get; set; }

            public DisplayRange(double start, double end)
            {
                Start = start;
                End = end;
            }
        }

        private void Plot(string LineName, Color LineColor, IEnumerable<short> UPoints)
        {
            var points = UPoints.Select((Time, Value) => new PointF(Value, Time)).ToList();
            var dataSource = new EnumerableDataSource<PointF>(points);
            dataSource.SetXMapping(P => P.X * TIME_STEP);
            dataSource.SetYMapping(P => P.Y);

            chartPlotter.AddLineGraph(dataSource, LineColor, 3, LineName);
            chartPlotter.FitToView();
        }

        private void ClearStatus()
        {
            lbWarning.Visibility = Visibility.Collapsed;
            lbFaultReason.Visibility = Visibility.Collapsed;
            lbProblem.Visibility = Visibility.Collapsed;

            ClearMeasureResult();
            chartPlotter.Children.RemoveAll(typeof(LineGraph));
        }

        private void ResetLabel(ContentControl Target)
        {
            Target.Content = "";
            Target.Background = Brushes.Transparent;
        }

        internal void ClearMeasureResult()
        //очистка label для вывода измеренных результатов
        {
            ResetLabel(lbIdc);
            ResetLabel(lbQrr);
            ResetLabel(lbIrr);
            ResetLabel(lbTrr);
            ResetLabel(lbTq);
            ResetLabel(lbDCFactFallRate);
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

        internal void SetResult(DeviceState State, Types.QrrTq.TestResults Result)
        {
            if (State == DeviceState.InProcess)
            {
                ClearStatus();
            }
            else
            {
                IsRunning = false;
                SetLabel(lbIdc, State, true, Result.Idc.ToString());
                SetLabel(lbQrr, State, true, Result.Qrr.ToString());
                SetLabel(lbIrr, State, true, Result.Irr.ToString());
                SetLabel(lbTrr, State, true, Result.Trr.ToString());
                SetLabel(lbTq, State, true, Result.Tq.ToString());
                SetLabel(lbDCFactFallRate, State, true, Result.DCFactFallRate.ToString());

                //выводим графики тока и напряжения
                Plot(@"I", m_XGreen.Color, Result.CurrentData);
                Plot(@"U", m_XOrange.Color, Result.VoltageData);
            }
        }

        internal void SetColorByProblem(ushort Problem)
        {
            //установка цвета lbProblem в зависимости от принятого кода Problem
            switch (Problem)
            {
                //будем привлекать внимание оператора с помощью выделения сообщения цветом
                case (ushort)Types.QrrTq.HWProblemReason.None:
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
                case (ushort)Types.QrrTq.HWWarningReason.None:
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

            Types.QrrTq.HWProblemReason ProblemReason = (Types.QrrTq.HWProblemReason)Problem;
            lbProblem.Content = "Problem " + ProblemReason.ToString();

            lbProblem.Visibility = Visibility.Visible;
        }

        internal void SetWarning(ushort Warning)
        {
            //закрашиваем цветом поле вывода Warning, чтобы обратить на него внимание оператора
            SetColorByWarning(Warning);

            Types.QrrTq.HWWarningReason WarningReason = (Types.QrrTq.HWWarningReason)Warning;
            lbWarning.Content = "Warning " + WarningReason.ToString();

            lbWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(ushort Fault)
        {
            Types.QrrTq.HWFaultReason FaultReason = (Types.QrrTq.HWFaultReason)Fault;

            lbFaultReason.Content = "Fault " + FaultReason.ToString();
            lbFaultReason.Visibility = Visibility.Visible;
            IsRunning = false;
        }

        internal void RefreshKindOfFreezing(ushort KindOfFreezing)
        {
            btnStart.Content = string.Format(Properties.Resources.Start + " ({0})", KindOfFreezing);
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
            var paramATU = new Types.ATU.TestParameters { IsEnabled = false };
            var paramRAC = new Types.RAC.TestParameters { IsEnabled = false };
            var paramIH = new Types.IH.TestParameters { IsEnabled = false };
            var paramRCC = new Types.RCC.TestParameters { IsEnabled = false };

            //если пресс был зажат вручную - не стоит пробовать зажимать его ещё раз
            ClampParameters.SkipClamping = Cache.Clamp.ManualClamping;

            if (!Cache.Net.Start(paramGate, paramVtm, paramBvt, paramATU, Parameters, paramRAC, paramIH, paramRCC,
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
