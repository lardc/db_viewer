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
    /// Interaction logic for GraphicPage.xaml
    /// </summary>
    public partial class SLPage
    {
        private const int TIME_STEP = 50;

        private readonly SolidColorBrush m_XGreen, m_XOrange;
        private bool m_IsRunning;

        public Types.SL.TestParameters Parameters { get; set; }
        public Types.Clamping.TestParameters ClampParameters { get; set; }
        public Types.Commutation.ModuleCommutationType CommType { get; set; }
        public Types.Commutation.ModulePosition ModPosition { get; set; }

        private const int RoomTemp = 25;
        public int Temperature { get; set; }

        internal SLPage()
        {
            Parameters = new Types.SL.TestParameters { IsEnabled = true, UseLsqMethod = Settings.Default.UseVTMPostProcessing };
            ClampParameters = new Types.Clamping.TestParameters
            {
                StandardForce = Types.Clamping.ClampingForceInternal.Custom,
                CustomForce = 5,
                IsHeightMeasureEnabled = false
            };
            CommType = Settings.Default.SinglePositionModuleMode ? Types.Commutation.ModuleCommutationType.Direct : Types.Commutation.ModuleCommutationType.MT3;
            Temperature = RoomTemp;
            InitializeComponent();

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

        internal void SetResultVtm(DeviceState State, Types.SL.TestResults Result)
        {
            if (State != DeviceState.InProcess)
            {
                IsRunning = false;

                if (State == DeviceState.Success)
                {
                    Plot(@"Itm", m_XGreen.Color, Result.ITMArray);
                    Plot(@"Vtm", m_XOrange.Color, Result.VTMArray);
                }
            }
            else
                ClearStatus();

            SetLabel(lblVtm, State, string.Format("{0}", Result.Voltage));

            //сознательно не используем SetLabel т.к. нам не нужна установка Background
            lblItm.Content = string.Format("{0}", Result.Current);
        }

        internal void SetWarning(Types.SL.HWWarningReason Warning)
        {
            if (labelWarning.Visibility != Visibility.Visible)
            {

                labelWarning.Content = Warning.ToString();
                labelWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetProblem(Types.SL.HWProblemReason Problem)
        {
            labelWarning.Content = Problem.ToString();
            labelWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(Types.SL.HWFaultReason Fault)
        {
            labelFault.Content = Fault.ToString();
            labelFault.Visibility = Visibility.Visible;
            IsRunning = false;
        }

        private void ClearStatus()
        {
            labelWarning.Visibility = Visibility.Collapsed;
            labelFault.Visibility = Visibility.Collapsed;

            ResetLabel(lblVtm);
            ResetLabel(lblItm);

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

        private static void ResetLabel(ContentControl Target)
        {
            Target.Content = "";
            Target.Background = Brushes.Transparent;
        }

        private void Plot(string LineName, Color LineColor, IEnumerable<short> UPoints)
        {
            var points = UPoints.Select((T, I) => new PointF(I, T)).ToList();
            var dataSource = new EnumerableDataSource<PointF>(points);
            dataSource.SetXMapping(P => P.X * TIME_STEP);
            dataSource.SetYMapping(P => P.Y);

            chartPlotter.AddLineGraph(dataSource, LineColor, 3, LineName);
            chartPlotter.FitToView();
        }

        internal void Start()
        {
            if (IsRunning)
                return;

            var paramGate = new Types.Gate.TestParameters { IsEnabled = false };
            var paramBvt = new Types.BVT.TestParameters { IsEnabled = false };
            var paramATU = new Types.ATU.TestParameters { IsEnabled = false };
            var paramQrrTq = new Types.QrrTq.TestParameters { IsEnabled = false };
            var paramRAC = new Types.RAC.TestParameters { IsEnabled = false };
            var paramIH = new Types.IH.TestParameters { IsEnabled = false };
            var paramRCC = new Types.RCC.TestParameters { IsEnabled = false };

            ClampParameters.SkipClamping = Cache.Clamp.ManualClamping;
            ClearStatus();

            if (!Cache.Net.Start(paramGate, Parameters, paramBvt, paramATU, paramQrrTq, paramRAC, paramIH, paramRCC,
                                 new Types.Commutation.TestParameters
                                 {
                                     BlockIndex = (!Cache.Clamp.UseTmax) ? Types.Commutation.HWBlockIndex.Block1 : Types.Commutation.HWBlockIndex.Block2,
                                     CommutationType = ConverterUtil.MapCommutationType(CommType),
                                     Position = ConverterUtil.MapModulePosition(ModPosition)
                                 }, ClampParameters))
                return;

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