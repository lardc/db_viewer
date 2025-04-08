using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using SCME.Types;
using SCME.UI.Properties;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace SCME.UI.PagesTech
{
    /// <summary>
    ///     Interaction logic for GraphicPage.xaml
    /// </summary>
    public partial class GatePage
    {
        private readonly SolidColorBrush m_XRed, m_XGreen, m_XOrange;
        private bool m_IsRunning;
        private const int RoomTemp = 25;
        public int Temperature { get; set; }

        public Types.Gate.TestParameters Parameters { get; set; }
        public Types.Clamping.TestParameters ClampParameters { get; set; }
        public Types.Commutation.ModuleCommutationType CommType { get; set; }
        public Types.Commutation.ModulePosition ModPosition { get; set; }

        internal GatePage()
        {
            Parameters = new Types.Gate.TestParameters { IsEnabled = true };
            ClampParameters = new Types.Clamping.TestParameters
            {
                StandardForce = Types.Clamping.ClampingForceInternal.Custom,
                CustomForce = 5.0f,
                IsHeightMeasureEnabled = false
            };
            CommType = Settings.Default.SinglePositionModuleMode ? Types.Commutation.ModuleCommutationType.Direct : Types.Commutation.ModuleCommutationType.MT3;
            Temperature = RoomTemp;
            InitializeComponent();

            m_XRed = (SolidColorBrush)FindResource("xRed1");
            m_XGreen = (SolidColorBrush)FindResource("xGreen1");
            m_XOrange = (SolidColorBrush)FindResource("xOrange1");

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
                btnStart.IsEnabled = !m_IsRunning;
                btnBack.IsEnabled = !m_IsRunning;
            }
        }

        internal void SetResultAll(DeviceState State)
        {
            if (State == DeviceState.InProcess)
                ClearStatus();
            else
                IsRunning = false;
        }

        internal void SetResultKelvin(DeviceState State, bool IsKelvinOk)
        {
            SetLabel(lblKelvin, State, IsKelvinOk ? Properties.Resources.Ok : Properties.Resources.Fault);
        }

        internal void SetResultResistance(DeviceState State, float Resistance)
        {
            SetLabel(lblResistance, State, string.Format("{0}", Resistance));
        }

        internal void SetResultGT(DeviceState State, float IGT, float VGT, IList<short> ArrayI,
                                        IList<short> ArrayV)
        {
            SetLabel(lblIGT, State, string.Format("{0}", IGT));
            SetLabel(lblVGT, State, string.Format("{0}", VGT));

            if (State == DeviceState.Success)
            {
                Plot(@"Igt", m_XRed.Color, ArrayI);
                Plot(@"Vgt", m_XOrange.Color, ArrayV);
            }
        }

        internal void SetResultIh(DeviceState State, float IH, IList<short> Array)
        {
            SetLabel(lblIH, State, string.Format("{0}", IH));

            if (State == DeviceState.Success)
                Plot(@"Ih", m_XGreen.Color, Array);
        }

        internal void SetResultIl(DeviceState State, float IL)
        {
            SetLabel(lblIL, State, string.Format("{0}", IL));
        }

        internal void SetWarning(Types.Gate.HWWarningReason Warning)
        {
            if (lblWarning.Visibility != Visibility.Visible)
            {

                lblWarning.Content = Warning.ToString();
                lblWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetProblem(Types.Gate.HWProblemReason Problem)
        {
            lblWarning.Content = Problem.ToString();
            lblWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(Types.Gate.HWFaultReason Fault)
        {
            lblFault.Content = Fault.ToString();
            lblFault.Visibility = Visibility.Visible;
            IsRunning = false;
        }

        private void ClearStatus()
        {
            lblWarning.Visibility = Visibility.Collapsed;
            lblFault.Visibility = Visibility.Collapsed;
            ResetLabel(lblKelvin);
            ResetLabel(lblResistance);
            ResetLabel(lblIGT);
            ResetLabel(lblVGT);
            ResetLabel(lblIH);
            ResetLabel(lblIL);

            chartPlotter.Children.RemoveAll(typeof(LineGraph));
        }

        private static void SetLabel(ContentControl Target, DeviceState State, string Message)
        {
            Target.Content = string.Empty;

            switch (State)
            {
                case DeviceState.Success:
                    Target.Background = Brushes.LightGreen;
                    Target.Content = Message;
                    break;
                case DeviceState.Problem:
                    Target.Background = Brushes.Gold;
                    Target.Content = Message;
                    break;
                case DeviceState.InProcess:
                    Target.Background = Brushes.Gold;
                    break;
                case DeviceState.Stopped:
                case DeviceState.Fault:
                    Target.Background = Brushes.Tomato;
                    break;
                default:
                    Target.Background = Brushes.Transparent;
                    break;
            }
        }

        private void ResetLabel(ContentControl Target)
        {
            Target.Content = "";
            Target.Background = Brushes.Transparent;
        }

        private void Plot(string LineName, Color LineColor, IEnumerable<short> UPoints)
        {
            var points = UPoints.Select((T, I) => new PointF(I, T)).ToList();
            var dataSource = new EnumerableDataSource<PointF>(points);

            dataSource.SetXMapping(P => P.X);
            dataSource.SetYMapping(P => P.Y);

            chartPlotter.AddLineGraph(dataSource, LineColor, 3, LineName);
            chartPlotter.FitToView();
        }

        internal void Start()
        {
            if (IsRunning)
                return;

            var paramVtm = new Types.SL.TestParameters { IsEnabled = false };
            var paramBvt = new Types.BVT.TestParameters { IsEnabled = false };
            var paramATU = new Types.ATU.TestParameters { IsEnabled = false };
            var paramQrrTq = new Types.QrrTq.TestParameters { IsEnabled = false };
            var paramRAC = new Types.RAC.TestParameters { IsEnabled = false };
            var paramIH = new Types.IH.TestParameters { IsEnabled = false };
            var paramRCC = new Types.RCC.TestParameters { IsEnabled = false };

            ClampParameters.SkipClamping = Cache.Clamp.ManualClamping;

            if (!Cache.Net.Start(Parameters, paramVtm, paramBvt, paramATU, paramQrrTq, paramRAC, paramIH, paramRCC,
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

        private void Start_Click(object Sender, RoutedEventArgs E)
        {
            ScrollViewer.ScrollToBottom();
            Start();
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