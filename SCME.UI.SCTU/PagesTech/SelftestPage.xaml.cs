using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using SCME.Types;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for SelftestPage.xaml
    /// </summary>
    public partial class SelftestPage
    {
        private const int MAX_PERSENTAGE_VALUE = 110;

        private Storyboard m_Storyboard;
        private int m_Index;
        private bool m_IsRunning;

        internal float PgBar0Value { get; set; }
        internal float PgBar1Value { get; set; }
        internal float PgBar2Value { get; set; }
        internal float PgBar3Value { get; set; }
        internal float PgBar4Value { get; set; }
        internal float PgBar5Value { get; set; }
        internal float PgBar6Value { get; set; }
        internal float PgBar7Value { get; set; }
        internal float PgBar8Value { get; set; }
        internal float PgBar9Value { get; set; }

        internal string Lbl0Value { get; set; }
        internal string Lbl1Value { get; set; }
        internal string Lbl2Value { get; set; }
        internal string Lbl3Value { get; set; }
        internal string Lbl4Value { get; set; }
        internal string Lbl5Value { get; set; }
        internal string Lbl6Value { get; set; }
        internal string Lbl7Value { get; set; }
        internal string Lbl8Value { get; set; }
        internal string Lbl9Value { get; set; }

        public SelftestPage()
        {
            InitializeComponent();
            btnTest.Visibility = Visibility.Collapsed;

            ClearStatus();
        }

        internal void AreButtonEnabled(TypeCommon.InitParams Param)
        {
            btnVtm.IsEnabled = Param.IsSLEnabled;
            btnBvt.IsEnabled = Param.IsBVTEnabled;
        }

        internal bool IsRunning
        {
            get { return m_IsRunning; }
            set
            {
                m_IsRunning = value;
                btnTest.IsEnabled = !m_IsRunning;
                btnBack.IsEnabled = !m_IsRunning;
            }
        }

        internal void SetResult(DeviceState State, Types.SL.TestResults Result)
        {
            if (State != DeviceState.InProcess)
            {
                IsRunning = false;

                if (State == DeviceState.Success)
                    Plot(Result.SelfTestArray, Result.CapacitorsArray);
            }
            else
                ClearStatus();
        }

        internal void SetWarning(Types.SL.HWWarningReason Warning)
        {
            labelWarning.Content = Warning.ToString();
            labelWarning.Visibility = Visibility.Visible;
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
            ProgressGo(pgBar0, 0, true);
            ProgressGo(pgBar1, 0, true);
            ProgressGo(pgBar2, 0, true);
            ProgressGo(pgBar3, 0, true);
            ProgressGo(pgBar4, 0, true);
            ProgressGo(pgBar5, 0, true);
            ProgressGo(pgBar6, 0, true);
            ProgressGo(pgBar7, 0, true);
            ProgressGo(pgBar8, 0, true);
            ProgressGo(pgBar9, 0, true);

            labelPercent0.Content = string.Empty;
            labelPercent1.Content = string.Empty;
            labelPercent2.Content = string.Empty;
            labelPercent3.Content = string.Empty;
            labelPercent4.Content = string.Empty;
            labelPercent5.Content = string.Empty;
            labelPercent6.Content = string.Empty;
            labelPercent7.Content = string.Empty;
            labelPercent8.Content = string.Empty;
            labelPercent9.Content = string.Empty;

            label0.Content = string.Format("{0} V", 0);
            label1.Content = string.Format("{0} V", 0);
            label2.Content = string.Format("{0} V", 0);
            label3.Content = string.Format("{0} V", 0);
            label4.Content = string.Format("{0} V", 0);
            label5.Content = string.Format("{0} V", 0);
            label6.Content = string.Format("{0} V", 0);
            label7.Content = string.Format("{0} V", 0);
            label8.Content = string.Format("{0} V", 0);
            label9.Content = string.Format("{0} V", 0);

            labelWarning.Visibility = Visibility.Collapsed;
            labelFault.Visibility = Visibility.Collapsed;
        }

        private void Plot(IEnumerable<short> VPoints, IList<float> CPoints)
        {
            var uPoints = VPoints.Select(T => (short)(T > MAX_PERSENTAGE_VALUE ? MAX_PERSENTAGE_VALUE : T)).ToList();

            ProgressGo(pgBar0, uPoints[0], true);
            ProgressGo(pgBar1, uPoints[1], true);
            ProgressGo(pgBar2, uPoints[2], true);
            ProgressGo(pgBar3, uPoints[3], true);
            ProgressGo(pgBar4, uPoints[4], true);
            ProgressGo(pgBar5, uPoints[5], true);
            ProgressGo(pgBar6, uPoints[6], true);
            ProgressGo(pgBar7, uPoints[7], true);
            ProgressGo(pgBar8, uPoints[8], true);
            ProgressGo(pgBar9, uPoints[9], true);

            labelPercent0.Content = string.Format("{0} %", uPoints[0]);
            labelPercent1.Content = string.Format("{0} %", uPoints[1]);
            labelPercent2.Content = string.Format("{0} %", uPoints[2]);
            labelPercent3.Content = string.Format("{0} %", uPoints[3]);
            labelPercent4.Content = string.Format("{0} %", uPoints[4]);
            labelPercent5.Content = string.Format("{0} %", uPoints[5]);
            labelPercent6.Content = string.Format("{0} %", uPoints[6]);
            labelPercent7.Content = string.Format("{0} %", uPoints[7]);
            labelPercent8.Content = string.Format("{0} %", uPoints[8]);
            labelPercent9.Content = string.Format("{0} %", uPoints[9]);

            label0.Content = string.Format("{0} V", CPoints[0]);
            label1.Content = string.Format("{0} V", CPoints[1]);
            label2.Content = string.Format("{0} V", CPoints[2]);
            label3.Content = string.Format("{0} V", CPoints[3]);
            label4.Content = string.Format("{0} V", CPoints[4]);
            label5.Content = string.Format("{0} V", CPoints[5]);
            label6.Content = string.Format("{0} V", CPoints[6]);
            label7.Content = string.Format("{0} V", CPoints[7]);
            label8.Content = string.Format("{0} V", CPoints[8]);
            label9.Content = string.Format("{0} V", CPoints[9]);
        }

        private void ProgressGo(RangeBase Bar, float To, bool WithAnimation)
        {
            if (!WithAnimation)
            {
                if (m_Storyboard != null)
                {
                    m_Storyboard.Stop();
                    Bar.Value = To;
                }
            }
            else
                AnimateProgressBar(Bar, To);
        }

        private void AnimateProgressBar(DependencyObject Bar, float To)
        {
            var animation = new DoubleAnimation
            {
                To = To,
                Duration = new Duration(TimeSpan.FromMilliseconds(100))
            };

            Storyboard.SetTarget(animation, Bar);
            Storyboard.SetTargetProperty(animation, new PropertyPath(RangeBase.ValueProperty));
            m_Storyboard = new Storyboard();
            m_Storyboard.Children.Add(animation);
            m_Storyboard.Begin();
        }

        private void Button_Click(object Sender, RoutedEventArgs E)
        {
            var btn = Sender as Button;
            if (btn == null)
                return;

            m_Index = Convert.ToInt16(btn.CommandParameter);
            btnTest.Visibility = (m_Index != 0) ? Visibility.Visible : Visibility.Collapsed;
            tabControl.SelectedIndex = m_Index;
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (tabControl.SelectedIndex != 0)
            {
                m_Index = 0;
                tabControl.SelectedIndex = m_Index;
                btnTest.Visibility = Visibility.Collapsed;
            }
            else if (NavigationService != null)
                NavigationService.GoBack();
        }

        private void Test_Click(object Sender, RoutedEventArgs E)
        {
            if (tabControl.SelectedIndex != 1)
                return;

            var paramGate = new Types.Gate.TestParameters { IsEnabled = false };
            var paramBvt = new Types.BVT.TestParameters { IsEnabled = false };
            var paramVtm = new Types.SL.TestParameters { IsEnabled = false };
            var paramAtu = new Types.ATU.TestParameters { IsEnabled = false };
            var paramQrrTq = new Types.QrrTq.TestParameters { IsEnabled = false };
            var paramRAC = new Types.RAC.TestParameters { IsEnabled = false };
            var paramIH = new Types.IH.TestParameters { IsEnabled = false };
            var paramRCC = new Types.RCC.TestParameters { IsEnabled = false };

            switch (tabControl.SelectedIndex)
            {
                case 1:
                    paramVtm.IsEnabled = true;
                    paramVtm.IsSelfTest = true;
                    break;
            }

            var started = Cache.Net.Start(paramGate, paramVtm, paramBvt, paramAtu, paramQrrTq, paramRAC, paramIH, paramRCC,
                                          new Types.Commutation.TestParameters
                                          {
                                              BlockIndex = Types.Commutation.HWBlockIndex.Block1,
                                              CommutationType = Types.Commutation.HWModuleCommutationType.Direct,
                                              Position = Types.Commutation.HWModulePosition.Position1
                                          }, new Types.Clamping.TestParameters { SkipClamping = true }, true);

            var stopped = Cache.Net.IsStopButtonPressed;
            if (stopped || !started)
                return;

            IsRunning = true;
        }
    }
}