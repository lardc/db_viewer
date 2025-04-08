using System;
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
//
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
//

namespace SCME.UI.PagesTech
{
    /// <summary>
    ///     Interaction logic for BvtPage.xaml
    /// </summary>
    public partial class BvtPage
    {
        private const int DATA_LENGTH = 600;

        private readonly SolidColorBrush m_XRed, m_XOrange;
        private bool m_IsRunning;

        public Types.BVT.TestParameters Parameters { get; set; }
        public Types.Clamping.TestParameters ClampParameters { get; set; }
        public Types.Commutation.ModuleCommutationType CommType { get; set; }
        public Types.Commutation.ModulePosition ModPosition { get; set; }

        private const int RoomTemp = 25;
        public int Temperature { get; set; }
        private bool wasCurrentMore;

        internal BvtPage()
        {
            Parameters = new Types.BVT.TestParameters
            {
                IsEnabled = true,

                //форма не даёт возможность управлять значением параметра MeasurementMode - нет необходимости в этом, данный параметр никак не влияет на режимы измерений, он нужен только для контроля полученных результатов измерений, но если его значение не равно ModeI, то включить тест Udsm/Ursm будет нельзя. CheckBox, который включает режим Udsm/Ursm никак не изменяет значение параметра MeasurementMode поэтому будем устанавливать MeasurementMode=ModeI по умолчанию
                MeasurementMode = Types.BVT.BVTMeasurementMode.ModeI
            };

            ClampParameters = new Types.Clamping.TestParameters
            {
                StandardForce = Types.Clamping.ClampingForceInternal.Custom,
                CustomForce = 5,
                IsHeightMeasureEnabled = false
            };
            CommType = Settings.Default.SinglePositionModuleMode ? Types.Commutation.ModuleCommutationType.Direct : Types.Commutation.ModuleCommutationType.MT3;
            Temperature = RoomTemp;
            InitializeComponent();

            m_XRed = (SolidColorBrush)FindResource("xRed1");
            m_XOrange = (SolidColorBrush)FindResource("xOrange1");

            ClearStatus();

            ViewportAxesRangeRestriction restr = new ViewportAxesRangeRestriction { YRange = new DisplayRange(-7, 7) };
            chartPlotter.Viewport.Restrictions.Add(restr);
        }

        //добавил
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

        public class ViewportAxesRangeRestriction : IViewportRestriction
        {
            public DisplayRange XRange = null;
            public DisplayRange YRange = null;

            public Rect Apply(Rect oldVisible, Rect newVisible, Viewport2D viewport)
            {
                if (XRange != null)
                {
                    newVisible.X = XRange.Start;
                    newVisible.Width = XRange.End - XRange.Start;
                }

                if (YRange != null)
                {
                    newVisible.Y = YRange.Start;
                    newVisible.Height = YRange.End - YRange.Start;
                }

                return newVisible;
            }

            public event EventHandler Changed;
        }
        //окончание добавления



        private bool IsRunning
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
            if (State == DeviceState.InProcess) ClearStatus();
            else IsRunning = false;
        }

        internal void SetResultBvtDirect(DeviceState State, Types.BVT.TestResults Result)
        {
            labelWarning.Visibility = Visibility.Collapsed;
            labelFault.Visibility = Visibility.Collapsed;

            SetLabel(labelDirect, State,
                $"{Result.VDRM}{Properties.Resources.V} : {Result.IDRM}{Properties.Resources.mA}");

            if (State == DeviceState.Success)
                PlotYX("Direct", m_XRed.Color, Result.VoltageData, Result.CurrentData);
        }

        internal void SetResultReverseBvt(DeviceState State, Types.BVT.TestResults Result)
        {
            labelWarning.Visibility = Visibility.Collapsed;
            labelFault.Visibility = Visibility.Collapsed;

            SetLabel(labelReverse, State,
                $"{Result.VRRM}{Properties.Resources.V} : {Result.IRRM}{Properties.Resources.mA}");
            if (State == DeviceState.Success)
                PlotYX("Reverse", m_XOrange.Color, Result.VoltageData, Result.CurrentData);
        }

        internal void SetWarning(Types.BVT.HWWarningReason Warning)
        {
            if (labelWarning.Visibility != Visibility.Visible)
            {
                if (labelWarning.Content.ToString() != string.Empty)
                    labelWarning.Content += " ";

                labelWarning.Content += Warning.ToString();
                labelWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetProblem(Types.BVT.HWProblemReason Problem)
        {
            if (labelWarning.Content.ToString() != string.Empty)
                labelWarning.Content += " ";

            labelWarning.Content += Problem.ToString();
            labelWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(Types.BVT.HWFaultReason Fault)
        {
            if (labelFault.Content.ToString() != string.Empty)
                labelFault.Content += " ";

            labelFault.Content += Fault.ToString();
            labelFault.Visibility = Visibility.Visible;
            IsRunning = false;
        }

        internal void SetResultBvtUdsmUrsmDirect(DeviceState State, Types.BVT.TestResults Result)
        {
            SetLabel(labelDirectSM, State,
                $"{Result.VDSM}{Properties.Resources.V} : {Result.IDSM}{Properties.Resources.mA}");
        }

        internal void SetResultBvtUdsmUrsmReverse(DeviceState State, Types.BVT.TestResults Result)
        {
            SetLabel(labelReverseSM, State,
                $"{Result.VRSM}{Properties.Resources.V} : {Result.IRSM}{Properties.Resources.mA}");
        }

        private void ClearStatus()
        {
            labelWarning.Visibility = Visibility.Collapsed;
            labelFault.Visibility = Visibility.Collapsed;

            ResetLabel(labelDirect);
            ResetLabel(labelReverse);

            chartPlotter.Children.RemoveAll(typeof(LineGraph));
            chartPlotter.Children.RemoveAll(typeof(MarkerPointsGraph));

            ResetLabel(labelDirectSM);
            ResetLabel(labelReverseSM);
        }

        private void PlotYX(string LineName, Color LineColor, ICollection<short> UxPoints, IEnumerable<short> UyPoints)
        {
            var crop = UxPoints.Count - DATA_LENGTH;

            var dataI = UxPoints.Skip(crop).Select(I => (Math.Abs(I) <= 2 ? (short)0 : I));
            var dataV = UyPoints.Skip(crop);

            if (dataI.Any() && (dataI.Min() < -7 * 10 || Math.Abs(dataI.Max()) > 7 * 10) || wasCurrentMore)
            {
                wasCurrentMore = true;
                chartPlotter.Viewport.Restrictions.Clear();
            }
            else
            {
                chartPlotter.Viewport.Restrictions.Clear();
                var restr = new ViewportAxesRangeRestriction { YRange = new DisplayRange(-7, 7) };
                chartPlotter.Viewport.Restrictions.Add(restr);
            }

            var points =
                dataI.Zip(dataV, (I, V) => new PointF(V, I / 10.0f))
                     .Select((P => (Math.Abs(P.X) < 200 ? new PointF(P.X, 0) : P)));

            var dataSource = new EnumerableDataSource<PointF>(points);
            dataSource.SetXMapping(P => P.X);
            dataSource.SetYMapping(P => P.Y);

            chartPlotter.AddLineGraph(dataSource, LineColor, 3, LineName);

            chartPlotter.FitToView();
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

        internal void Start()
        {
            if (IsRunning)
                return;

            var paramGate = new Types.Gate.TestParameters { IsEnabled = false };
            var paramVtm = new Types.SL.TestParameters { IsEnabled = false };
            var paramATU = new Types.ATU.TestParameters { IsEnabled = false };
            var paramQrrTq = new Types.QrrTq.TestParameters { IsEnabled = false };
            var paramRAC = new Types.RAC.TestParameters { IsEnabled = false };
            var paramIH = new Types.IH.TestParameters { IsEnabled = false };
            var paramRCC = new Types.RCC.TestParameters { IsEnabled = false };

            ClampParameters.SkipClamping = Cache.Clamp.ManualClamping;

            Parameters.VoltageFrequency = (ushort)Settings.Default.BVTVoltageFrequency;
            //Parameters.MeasurementMode = Types.BVT.BVTMeasurementMode.ModeV; //закоментировал после реализации UDSM/URSM

            if (!Cache.Net.Start(paramGate, paramVtm, Parameters, paramATU, paramQrrTq, paramRAC, paramIH, paramRCC,
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
            wasCurrentMore = false;

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