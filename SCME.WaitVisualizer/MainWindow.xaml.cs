using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace WaitVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private bool FVisualizationSortingFiltering = false;

        private DispatcherTimer FRefresherTimer = new DispatcherTimer();
        private Stopwatch FStopWatch = new Stopwatch();
        private int FRefresherTimerTicsCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            //таймер this.FRefresherTimer должен работать всё время пока работает данный процесс
            this.FRefresherTimer.Tick += this.DispatcherTimerTick;
            this.FRefresherTimer.Interval = new TimeSpan(0, 0, 0, 0, 333);
            this.FRefresherTimer.Start();
        }

        private string FTimeElapsed;
        public string TimeElapsed
        {
            get { return this.FTimeElapsed; }

            set
            {
                if (value != this.FTimeElapsed)
                {
                    this.FTimeElapsed = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DispatcherTimerTick(object sender, EventArgs e)
        {
            //подсчитываем каждый тик таймера чтобы уменьшить нагрузку от данного приложения
            //проверяем существование запустившего нас процесса каждый раз когда значение счётчика тиков таймера равно 8
            TimeSpan ts = TimeSpan.FromMilliseconds(this.FStopWatch.Elapsed.TotalMilliseconds);
            this.TimeElapsed = ts.ToString(@"mm\:ss");

            //если счётчик прошедших тиков таймера равен 8 и запустивший нас процесс не существует - значит и данный процесс уже не нужен
            if (this.FRefresherTimerTicsCount == 8)
            {
                this.FRefresherTimerTicsCount = 0;

                if (!ExistOwnerProcess())
                    this.Close();
            }

            this.FRefresherTimerTicsCount++;
        }

        /*
        private void ME_WaitVisualizer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (sender is MediaElement me)
            {
                me.Position = new TimeSpan(0, 0, 1);
                me.Play();
            }
        }
        */     
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private void HideWindow()
        {
            //сразу при описании окна нельзя установить ShowInTaskbar = false, т.к. на стороне запускающего приложения из-за этого будет нельзя получить Handle главного окна создаваемого процесса
            if (this.ShowInTaskbar)
                this.ShowInTaskbar = false;

            this.FVisualizationSortingFiltering = false;
            this.FStopWatch.Stop();
            this.FStopWatch.Reset();
            //this.FRefresherTimer.Stop();

            this.Visibility = Visibility.Hidden;
        }

        private void ShowWindow(IntPtr wParam, IntPtr lParam)
        {
            //используем данные о положении родительского окна
            //в wParam передаётся координата X середины родительского окна
            //в lParam передаётся координата Y середины родительского окна
            this.Left = wParam.ToInt32() - Convert.ToInt32(Math.Ceiling(this.ActualWidth / 2));
            this.Top = lParam.ToInt32() - Convert.ToInt32(Math.Ceiling(this.ActualHeight / 2));
            this.Visibility = Visibility.Visible;

            this.FStopWatch.Start();
            //this.FRefresherTimer.Start();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case SCME.Common.Constants.WM_HIDE:
                    if (!this.FVisualizationSortingFiltering)
                        this.HideWindow();
                    break;

                case SCME.Common.Constants.WM_HIDESortingFiltering:
                    if (this.FVisualizationSortingFiltering)
                        this.HideWindow();
                    break;

                case SCME.Common.Constants.WM_SHOW:
                    this.FVisualizationSortingFiltering = false;
                    this.ShowWindow(wParam, lParam);
                    break;

                case SCME.Common.Constants.WM_SHOWSortingFiltering:
                    this.FVisualizationSortingFiltering = true;
                    this.ShowWindow(wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }

        private void WaitVisualizerForm_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private bool ExistOwnerProcess()
        {
            //проверяет наличие процесса запустившего данный процесс
            //возвращает:
            // true - запустивший нас процесс работает (в наличии);
            // false - запустивший нас процесс не существует

            //данный процесс при своём запуске в StartInfo.Arguments получил идентификатор запустившего его процесса
            if (int.TryParse((Application.Current as App).Arguments, out int ownerProcessID))
            {
                //получаем список всех процессов на данной машине
                Process[] localProcess = Process.GetProcesses();

                //проверяем существование запусившего нас процесса
                Process ownerProcess = localProcess.Where(p => p.Id == ownerProcessID).FirstOrDefault();

                return ownerProcess != null;
            }

            return false;
        }

        private void BtClose_Click(object sender, RoutedEventArgs e)
        {
            //если процесс запустивший данный процесс визуализации ожидания работает - скрываем данное окно
            //если процесса, который запустил данный процесс визуализации ожидания не существует - закрываем данное окно, это равнозначно уничтожению процесса визуализации ожидания
            switch (this.ExistOwnerProcess())
            {
                case true:
                    this.HideWindow();
                    break;

                default:
                    this.Close();
                    break;
            }
        }
    }
}
