using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using SCME.Types;
using SCME.Types.Profiles;
using SCME.UI.CustomControl;
using SCME.UI.IO;
using SCME.UI.PagesTech;
using SCME.UI.PagesUser;
using SCME.UI.Properties;

namespace SCME.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private const string PROFILE_ENDPOINT_NAME = "SCME.ProfileService";

        private readonly object m_InitialClampLabelContent;
        private readonly SolidColorBrush m_XRed, m_XOrange;
        private bool m_IsSafetyBreakIconVisible;
        private bool m_IsKeyboardShown;
        private Visibility m_GoTechButtonVisibility, m_AccountButtonVisibility, m_TechPasswordVisibility;
        private Page m_PrevPage;
        private Brush m_NominalClampPathStroke;

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Localization);
            }
            catch (Exception ex)
            {
                var dw = new DialogWindow("Localization error", ex.Message);
                dw.ButtonConfig(DialogWindow.EbConfig.OK);
                dw.ShowDialog();
            }

            Cache.Keyboards =
                new KeyboardLayouts(Path.IsPathRooted(Settings.Default.KeyboardsPath)
                                        ? Settings.Default.KeyboardsPath
                                        : Path.Combine(
                                            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase),
                                            Settings.Default.KeyboardsPath));

            InitializeComponent();

            m_InitialClampLabelContent = clampLabel.Content;

            m_XRed = (SolidColorBrush)FindResource("xRed1");
            m_XOrange = (SolidColorBrush)FindResource("xOrange1");

            TechPasswordVisibility = Visibility.Collapsed;

            try
            {
                Cache.Main = this;

                if (Settings.Default.NormalWindow)
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    ResizeMode = ResizeMode.CanResize;
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    Top = SystemParameters.WorkArea.Top;
                    Left = SystemParameters.WorkArea.Left;
                    Width = SystemParameters.WorkArea.Width;
                    Height = SystemParameters.WorkArea.Height;
                }

                Cache.Storage = new LocalStorage(Settings.Default.StoragePath);
                Cache.Net = new ControlLogic();
                MmeCode = Settings.Default.MMECode;

                IsSafetyBreakIconVisible = false;

                RestartRoutine(null, null);
            }
            catch (Exception ex)
            {
                var dw = new DialogWindow(Properties.Resources.ApplicationStartError, ex.Message);
                dw.ButtonConfig(DialogWindow.EbConfig.OK);
                dw.ShowDialog();

                Application.Current.Shutdown(1);
            }
        }


        private static void CurrentDomainOnUnhandledException(object Sender, UnhandledExceptionEventArgs Args)
        {
            MessageBox.Show(Args.ExceptionObject.ToString(), "Unhandled exception");
        }

        static void DispatcherUnhandledException(object Sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs E)
        {
            MessageBox.Show(E.Exception.ToString(), "Unhandled exception");
        }

        public bool ConnectionLabelVisible
        {
            set { connectionLabel.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsSafetyBreakIconVisible
        {
            get { return m_IsSafetyBreakIconVisible; }
            set
            {
                m_IsSafetyBreakIconVisible = value;
                NotifyPropertyChanged("IsSafetyBreakIconVisible");
            }
        }

        public Visibility GoTechButtonVisibility
        {
            get { return m_GoTechButtonVisibility; }
            set
            {
                m_GoTechButtonVisibility = value;
                NotifyPropertyChanged("GoTechButtonVisibility");
            }
        }

        public Visibility AccountButtonVisibility
        {
            get { return m_AccountButtonVisibility; }
            set
            {
                m_AccountButtonVisibility = value;
                NotifyPropertyChanged("AccountButtonVisibility");
            }
        }

        public Visibility TechPasswordVisibility
        {
            get { return m_TechPasswordVisibility; }
            set
            {
                m_TechPasswordVisibility = value;
                NotifyPropertyChanged("TechPasswordVisibility");
            }
        }

        internal string AccountName
        {
            get { return accountLabel.Content.ToString(); }
            set { accountLabel.Content = value; }
        }

        internal string State
        {
            get { return stateLable.Content.ToString(); }
            set { stateLable.Content = value; }
        }

        internal string MmeCode { get; set; }

        internal bool IsNeedToRestart { get; private set; }

        internal bool IsProfilesParsed { get; set; }

        internal TypeCommon.InitParams Param { get; private set; }

        internal void RestartRoutine(object Sender, RoutedEventArgs E)
        {
            Param = new TypeCommon.InitParams
            {
                TimeoutService = Cache.Welcome.GetTimeout(ComplexParts.Service),
                IsInternalEnabled = Cache.Welcome.IsDeviceEnabled(ComplexParts.FTDI) && Settings.Default.FTDIIsInUse,
                TimeoutAdapter = Cache.Welcome.GetTimeout(ComplexParts.Adapter),
                IsGateEnabled = Cache.Welcome.IsDeviceEnabled(ComplexParts.Gate) && Settings.Default.GateIsVisible,
                TimeoutGate = Cache.Welcome.GetTimeout(ComplexParts.Gate),
                IsSLEnabled = false,
                TimeoutSL = Cache.Welcome.GetTimeout(ComplexParts.SL),
                IsBVTEnabled = false,
                TimeoutBVT = Cache.Welcome.GetTimeout(ComplexParts.BVT),
                IsClampEnabled = Cache.Welcome.IsDeviceEnabled(ComplexParts.Clamping) && Settings.Default.ClampIsVisible,
                TimeoutClamp = Cache.Welcome.GetTimeout(ComplexParts.Clamping),
                IsdVdtEnabled = false,
                IsSctuEnabled = true,
                TimeoutSctu = Cache.Welcome.GetTimeout(ComplexParts.Sctu)
            };

            if (!Equals(mainFrame.Content, Cache.Welcome))
                mainFrame.Navigate(Cache.Welcome);

            Cache.Welcome.IsRestartEnable = false;
            Cache.Welcome.IsBackEnable = false;
            Cache.Technician.AreButtonsEnabled(Param);
            Cache.Calibration.AreButtonsEnabled(Param);
            Cache.Console.AreButtonEnabled(Param);
            Cache.Selftest.AreButtonEnabled(Param);


            IsNeedToRestart = true;
            IsProfilesParsed = false;
            Cache.Net.Deinitialize();
        }

        internal void SetClampState(Types.Clamping.SqueezingState State)
        {
            switch (State)
            {
                case Types.Clamping.SqueezingState.Down:
                    clampPath.Stroke = Brushes.DarkSeaGreen;
                    break;
                case Types.Clamping.SqueezingState.Squeezing:
                    clampPath.Stroke = Brushes.Gold;
                    break;
                case Types.Clamping.SqueezingState.Up:
                    clampPath.Stroke = m_XOrange;
                    break;
                case Types.Clamping.SqueezingState.Unsqueezing:
                    clampPath.Stroke = Brushes.Gold;
                    break;
                case Types.Clamping.SqueezingState.Undeterminated:
                    clampPath.Stroke = m_XRed;
                    break;
            }

            m_NominalClampPathStroke = clampPath.Stroke;
        }

        internal void SetClampWarning(Types.Clamping.HWWarningReason Warning)
        {
            if (Warning == Types.Clamping.HWWarningReason.None)
            {
                clampLabel.Content = m_InitialClampLabelContent;
                clampPath.Stroke = m_NominalClampPathStroke;
            }
            else
            {
                clampLabel.Content = Warning.ToString();
                clampPath.Stroke = Brushes.Tomato;
            }
        }

        internal void SetClampFault(Types.Clamping.HWFaultReason Fault)
        {
            if (Fault == Types.Clamping.HWFaultReason.None)
            {
                clampLabel.Content = m_InitialClampLabelContent;
                clampPath.Stroke = m_NominalClampPathStroke;
            }
            else
            {
                clampLabel.Content = Fault.ToString();
                clampPath.Stroke = Brushes.Tomato;
            }
        }

        private void BtnExit_Click(object Sender, RoutedEventArgs E)
        {
            Application.Current.Shutdown();
        }

        private void MainWindow_Closing(object Sender, CancelEventArgs E)
        {
            IsNeedToRestart = false;

            if (Cache.Net != null)
                Cache.Net.Deinitialize();
        }

        private void MainWindow_OnClosed(object Sender, EventArgs E)
        {
            if (Settings.Default.RunExplorer)
                Process.Start("explorer.exe");
        }

        private void Frame_Navigating(object Sender, NavigatingCancelEventArgs E)
        {
            if (Settings.Default.IsAnimationEnabled)
            {
                var animation = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = new Duration(TimeSpan.FromMilliseconds(200))
                    };
                mainFrame.BeginAnimation(OpacityProperty, animation);
            }
        }

        private void Frame_Navigated(object Sender, NavigationEventArgs E)
        {
            titleLabel.Content = ((Page)mainFrame.Content).Title;
            GoTechButtonVisibility = Equals(mainFrame.Content, Cache.ProfileSelection) ||
                                     Equals(mainFrame.Content, Cache.UserTest) ||
                                     Equals(mainFrame.Content, Cache.Login)
                                         ? Visibility.Visible
                                         : Visibility.Collapsed;
            AccountButtonVisibility = Equals(mainFrame.Content, Cache.ProfileSelection) ||
                                      Equals(mainFrame.Content, Cache.UserTest)
                                          ? Visibility.Visible
                                          : Visibility.Collapsed;

            if (Equals(mainFrame.Content, Cache.Password) && Settings.Default.IsTechPasswordEnabled)
                Cache.Technician.PreviousPage = m_PrevPage;
            m_PrevPage = E.Content as Page;

            if (Settings.Default.IsAnimationEnabled)
            {
                var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = new Duration(TimeSpan.FromMilliseconds(200))
                    };
                mainFrame.BeginAnimation(OpacityProperty, animation);
            }
        }

        private void Tech_Click(object Sender, RoutedEventArgs E)
        {
            if (Settings.Default.IsTechPasswordEnabled)
                mainFrame.Navigate(Cache.Password);
            else
                mainFrame.Navigate(Cache.Technician);
        }

        private void Logout_Click(object Sender, RoutedEventArgs E)
        {
            mainFrame.Navigate(Cache.Login);
        }

        #region Keyboard

        private void MyWindow_PreviewGotKeyboardFocus(object Sender, KeyboardFocusChangedEventArgs E)
        {
            var control = E.NewFocus as ValidatingTextBox;
            ShowKeyboard(control != null && !Settings.Default.NormalWindow, control);
        }

        private void MyWindow_MouseDown(object Sender, MouseButtonEventArgs E)
        {
            ShowKeyboard(false, null);
        }

        public void ShowKeyboard(bool Show, ValidatingTextBox Control)
        {
            if (!m_IsKeyboardShown && Show)
            {
                m_IsKeyboardShown = true;

                var asPopup =
                    !(Control.TransformToAncestor(this).Transform(new Point(0, Control.ActualHeight)).Y >
                      Height - defaultKeyboard.Height);

                if (asPopup)
                {
                    defaultKeyboard.Show(Settings.Default.IsAnimationEnabled);
                    keyboardPopup.IsOpen = true;
                }
                else
                {
                    keyboardBorder.Height = defaultKeyboard.Height;
                    defaultKeyboard.Show(Settings.Default.IsAnimationEnabled);
                    keyboardPopup.IsOpen = true;
                    AnimateScrollViewer(defaultKeyboard.Height);
                }
            }

            if (m_IsKeyboardShown && !Show)
            {
                m_IsKeyboardShown = false;
                AnimateScrollViewer(0);
                defaultKeyboard.Hide(Settings.Default.IsAnimationEnabled);
            }
        }

        private void AnimateScrollViewer(double To)
        {
            keyboardBorder.Height = To;

            if (Settings.Default.IsAnimationEnabled)
            {
                var verticalAnimation = new DoubleAnimation
                    {
                        To = To,
                        DecelerationRatio = .2,
                        Duration = new Duration(TimeSpan.FromMilliseconds(300))
                    };

                var storyboard = new Storyboard();
                storyboard.Children.Add(verticalAnimation);
                Storyboard.SetTarget(verticalAnimation, scrollViewer);
                Storyboard.SetTargetProperty(verticalAnimation,
                                             new PropertyPath(ScrollViewerBehavior.VerticalOffsetProperty));
                storyboard.Begin();
            }
            else
                scrollViewer.ScrollToVerticalOffset(To);
        }

        private void Keyboard_EnterPressed(object Sender, RoutedEventArgs E)
        {
            ShowKeyboard(false, null);
        }

        #endregion

        #region IPropertyChanged implementation

        protected void NotifyPropertyChanged(String Info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Info));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
                                                new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static void SetVerticalOffset(FrameworkElement Target, double Value)
        {
            Target.SetValue(VerticalOffsetProperty, Value);
        }

        public static double GetVerticalOffset(FrameworkElement Target)
        {
            return (double)Target.GetValue(VerticalOffsetProperty);
        }

        private static void OnVerticalOffsetChanged(DependencyObject Target, DependencyPropertyChangedEventArgs E)
        {
            var scrollViewer = Target as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset((double)E.NewValue);
            }
        }
    }
}