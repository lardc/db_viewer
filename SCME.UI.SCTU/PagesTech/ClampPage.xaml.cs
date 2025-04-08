using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using SCME.Types;
using SCME.UI.Annotations;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for ClampPage.xaml
    /// </summary>
    public partial class ClampPage : Page, INotifyPropertyChanged
    {
        private readonly SolidColorBrush m_XRed, m_XGreen, m_XOrange;
        private bool m_IsRunning, m_Clamped;

        public bool UseTmax { get; set; }
        public Types.Clamping.TestParameters ClampParameters { get; set; }

        public ushort Temperature { get; set; }

        public bool ManualClamping { get; private set; }

        public bool IsClamped
        {
            get
            {
                return m_Clamped;
            }
            set
            {
                m_Clamped = value;
                OnPropertyChanged("IsClamped");
            }
        }


        public ClampPage()
        {
            ClampParameters = new Types.Clamping.TestParameters { StandardForce = Types.Clamping.ClampingForceInternal.Custom, CustomForce = 5.0f };

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
                btnBack.IsEnabled = !m_IsRunning;
                btnClamp.IsEnabled = !m_IsRunning;
                btnUnclamp.IsEnabled = !m_IsRunning;
            }
        }

        private void ClearStatus()
        {
            lblWarning.Visibility = Visibility.Collapsed;
            lblFault.Visibility = Visibility.Collapsed;

            chartPlotter.Children.RemoveAll(typeof(LineGraph));
        }

        internal void Clamp()
        {
            if (IsRunning)
                return;

            ClearStatus();
            IsRunning = true;

            Cache.Net.Squeeze(ClampParameters);
            ManualClamping = true;
            IsClamped = true;
        }

        internal void Unclamp()
        {
            if (IsRunning)
                return;

            ClearStatus();
            IsRunning = true;

            Cache.Net.Unsqueeze(ClampParameters);
            ManualClamping = false;
            IsClamped = false;
        }

        internal void SetWarning(Types.Clamping.HWWarningReason Warning)
        {
            lblWarning.Content = Warning.ToString();
            lblWarning.Visibility = Visibility.Visible;
        }

        internal void SetProblem(Types.Clamping.HWProblemReason Problem)
        {
            lblWarning.Content = Problem.ToString();
            lblWarning.Visibility = Visibility.Visible;
        }

        internal void SetFault(Types.Clamping.HWFaultReason Fault)
        {
            lblFault.Content = Fault.ToString();
            lblFault.Visibility = Visibility.Visible;
            IsRunning = false;

            ManualClamping = false;
            IsClamped = false;
        }

        private void Plot(string LineName, Color LineColor, IEnumerable<float> UPoints)
        {
            var points = UPoints.Select((T, I) => new PointF(I, T)).ToList();
            var dataSource = new EnumerableDataSource<PointF>(points);

            dataSource.SetXMapping(P => P.X);
            dataSource.SetYMapping(P => P.Y);

            chartPlotter.AddLineGraph(dataSource, LineColor, 3, LineName);
            chartPlotter.FitToView();
        }

        internal void SetResult(Types.Clamping.SqueezingState State, IList<float> ArrayF,
                                        IList<float> ArrayFd)
        {
            IsRunning = false;

            if (State == Types.Clamping.SqueezingState.Up)
            {
                if(ArrayF != null)
                    Plot(@"F", m_XGreen.Color, ArrayF);

                if (ArrayFd != null)
                    Plot(@"Fd", m_XOrange.Color, ArrayFd);
            }
        }

        private void Clamp_Click(object Sender, RoutedEventArgs E)
        {
            Clamp();
        }

        private void Unclamp_Click(object Sender, RoutedEventArgs E)
        {
            Unclamp();
        }

        private void Stop_Click(object Sender, RoutedEventArgs E)
        {
            Cache.Net.StopByButtonStop();
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            NavigationService?.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string PropertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        private void BtnSetTemp_OnClick(object sender, RoutedEventArgs e)
        {
            Cache.Net.StartHeating(Temperature);
        }

        private void BtnCool_OnClick(object sender, RoutedEventArgs e)
        {
            Cache.Net.StopHeating();
        }

        public void SetTopTemp(int temeprature)
        {
            lblTop.Content = temeprature;
            var bottomTemp = Temperature - 2;
            var topTemp = Temperature + 2;
            if (temeprature < bottomTemp || temeprature > topTemp)
            {
                lblTop.Background = Brushes.Tomato;
            }
            else
            {
                lblTop.Background = Brushes.LightGreen;
            }
        }

        public void SetBottomTemp(int temeprature)
        {
            lblBot.Content = temeprature;
            var bottomTemp = Temperature - 2;
            var topTemp = Temperature + 2;
            if (temeprature < bottomTemp || temeprature > topTemp)
            {
                lblBot.Background = Brushes.Tomato;
            }
            else
            {
                lblBot.Background = Brushes.LightGreen;
            }
        }
    }
}
