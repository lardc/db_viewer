using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SCME.Types;

namespace SCME.UI.CustomControl
{
    /// <summary>
    /// Interaction logic for UserControlItem.xaml
    /// </summary>
    public partial class HardwareItem : INotifyPropertyChanged
    {
        private Storyboard m_Storyboard;
        private SolidColorBrush m_Color, m_PbColor;
        private Visibility m_DisableButtonVisibility;
        private StreamGeometry m_Logo;
        private string m_Title, m_Error, m_DisableButtonContent;
        private bool m_IsDisabled, m_IsTimeoutEnabled;

        public event PropertyChangedEventHandler PropertyChanged;
        public ComplexParts Device { get; set; }
        public int InitializeTimeout { get; set; }
        public int OperationTimeout { get; set; }
        public bool DisableAvailable { get; set; }
        public int ClampTimeout { get; set; }

        public HardwareItem()
        {
            Device = ComplexParts.None;
            InitializeTimeout = 10000;
            OperationTimeout = 10000;
            ClampTimeout = 120000;

            DisableAvailable = false;

            InitializeComponent();

            m_Storyboard = new Storyboard();
            Backcolor = new SolidColorBrush(Colors.DarkOrange);
            ProgressBarBackground = FindResource("xGreen2") as SolidColorBrush;
            Logo = FindResource("CircleStreamPath") as StreamGeometry;

            Title = "Title";
            IsDisabled = false;
            Error = string.Empty;
            DisableButtonVisibility = Visibility.Hidden;
        }

        public SolidColorBrush Backcolor
        {
            get { return m_Color; }
            set
            {
                m_Color = value;
                NotifyPropertyChanged("Backcolor");
            }
        }

        public StreamGeometry Logo
        {
            get { return m_Logo; }
            set
            {
                m_Logo = value;
                NotifyPropertyChanged("Logo");
            }
        }

        public SolidColorBrush ProgressBarBackground
        {
            get { return m_PbColor; }
            set
            {
                m_PbColor = value;
                NotifyPropertyChanged("ProgressBarBackground");
            }
        }

        public Visibility DisableButtonVisibility
        {
            get { return m_DisableButtonVisibility; }
            set
            {
                m_DisableButtonVisibility = value;
                NotifyPropertyChanged("DisableButtonVisibility");
            }
        }

        public string Title
        {
            get { return m_Title; }
            set
            {
                m_Title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public bool IsDisabled
        {
            get { return m_IsDisabled; }
            set
            {
                m_IsDisabled = value;
                NotifyPropertyChanged("IsDisabled");
                DisableButtonContent = m_IsDisabled ? Properties.Resources.Enable : Properties.Resources.Disable;
            }
        }

        public string Error
        {
            get { return m_Error; }
            set
            {
                m_Error = value;
                NotifyPropertyChanged("Error");
            }
        }

        public string DisableButtonContent
        {
            get { return m_DisableButtonContent; }
            set
            {
                m_DisableButtonContent = value;
                NotifyPropertyChanged("DisableButtonContent");
            }
        }

        public void SetConnectionStatus(DeviceConnectionState State, string Message)
        {
            m_IsTimeoutEnabled = State == DeviceConnectionState.ConnectionInProcess;

            switch (State)
            {
                case DeviceConnectionState.ConnectionSuccess:
                    ProgressGo(100, false);
                    break;
                case DeviceConnectionState.ConnectionTimeout:
                case DeviceConnectionState.ConnectionFailed:
                    ProgressGo(100, false);
                    SetError(Message);
                    break;
                case DeviceConnectionState.ConnectionInProcess:
                    ProgressGo(0, 100, true);
                    break;
                case DeviceConnectionState.DisconnectionInProcess:
                    ProgressGo(100, 0, true);
                    break;
                case DeviceConnectionState.DisconnectionSuccess:
                    ProgressGo(0, false);
                    break;
                case DeviceConnectionState.None:
                    ProgressGo(0, false);
                    break;
            }
        }

        protected void NotifyPropertyChanged(String Info)
        {
            if (PropertyChanged != null) 
                PropertyChanged(this, new PropertyChangedEventArgs(Info));
        }

        private void ProgressGo(int To, bool WithAnimation)
        {
            ProgressBarBackground = FindResource("xGreen2") as SolidColorBrush;
            DisableButtonVisibility = IsDisabled ? Visibility.Visible : Visibility.Hidden;
            Error = string.Empty;

            if (WithAnimation)
            {
                AnimateProgressBar(To);
            }
            else if (m_Storyboard != null)
            {
                m_Storyboard.Stop();
                progressBar.Value = To;
            }
        }

        private void ProgressGo(int ArgFrom, int ArgTo, bool WithAnimation)
        {
            ProgressBarBackground = FindResource("xGreen2") as SolidColorBrush;
            DisableButtonVisibility = IsDisabled ? Visibility.Visible : Visibility.Hidden;
            Error = string.Empty;

            if (!WithAnimation)
            {
                if (m_Storyboard != null)
                {
                    m_Storyboard.Stop();
                    progressBar.Value = ArgTo;
                }
            }
            else
                AnimateProgressBar(ArgFrom, ArgTo);
        }

        private void SetError(string Err)
        {
            if (DisableAvailable)
                DisableButtonVisibility = Visibility.Visible;

            Error = Err;
            ProgressBarBackground = FindResource("xRed1") as SolidColorBrush;
        }

        private void ButtonDisable_Click(object Sender, RoutedEventArgs E)
        {
            IsDisabled = !IsDisabled;
        }

        private void AnimateProgressBar(int ArgTo)
        {
            var animation = new DoubleAnimation
                {
                    To = ArgTo,
                    Duration = new Duration(TimeSpan.FromMilliseconds(InitializeTimeout))
                };

            Storyboard.SetTarget(animation, progressBar);
            Storyboard.SetTargetProperty(animation, new PropertyPath(RangeBase.ValueProperty));

            m_Storyboard = new Storyboard();
            m_Storyboard.Completed += Sb_Completed;
            m_Storyboard.Children.Add(animation);
            m_Storyboard.Begin();
        }

        private void AnimateProgressBar(int ArgFrom, int ArgTo)
        {
            var animation = new DoubleAnimation
                {
                    From = ArgFrom,
                    To = ArgTo,
                    Duration = new Duration(TimeSpan.FromMilliseconds(InitializeTimeout))
                };

            Storyboard.SetTarget(animation, progressBar);
            Storyboard.SetTargetProperty(animation, new PropertyPath(RangeBase.ValueProperty));

            m_Storyboard = new Storyboard();
            m_Storyboard.Completed += Sb_Completed;
            m_Storyboard.Children.Add(animation);
            m_Storyboard.Begin();
        }

        private void Sb_Completed(object Sender, EventArgs E)
        {
            if (!m_IsTimeoutEnabled)
                return;

            Cache.Net.CallbackManager.DeviceConnectionHandler(Device, DeviceConnectionState.ConnectionTimeout,
                                                              Properties.Resources.DisplayTimeout);
        }
    }
}