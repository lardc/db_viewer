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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using SCME.UI.Annotations;
using SCME.UI.CustomControl;
using System.Collections.Concurrent;
using System.Windows.Threading;
using System.Threading;
using SCME.UI.Properties;
using SCME.Types;

namespace SCME.UI.PagesUser
{
    /// <summary>
    /// Interaction logic for ActivationWorkPlacePage.xaml
    /// </summary>
    public partial class ActivationWorkPlacePage : Page
    {
        private readonly ConcurrentQueue<Action> m_ActionQueue;
        private Thread m_ActivationWorkPlaceThread;
        private readonly DispatcherTimer m_Timer;
        private volatile bool m_Stop;
        private ushort REG_WORKPLACE_ACTIVATION_STATUS;
        private const int REQUEST_DELAY_MS = 500;

        private bool BrushScrollViewerChanged = false;
        private Brush PreviousBrushScrollViewer = null;
        private readonly ChannelByClumpType ChByClampType;

        public ActivationWorkPlacePage()
        {
            InitializeComponent();
            m_ActionQueue = new ConcurrentQueue<Action>();
            m_Timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 50) };
            m_Timer.Tick += TimerTick;

            //считываем из конфигурационного файла и запоминаем значение используемого канала измерения
            ChByClampType = (ChannelByClumpType)Settings.Default.ChannelByClampType;

            //узнаём номер регистра активации рабочего места, чтобы поток мог с ним работать
            REG_WORKPLACE_ACTIVATION_STATUS = Cache.Net.ActivationWorkPlace(ComplexParts.Sctu, ChannelByClumpType.NullValue, Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE);
        }

        public void Free()
        {
            //завершаем работу потока
            if (m_ActivationWorkPlaceThread != null)
            {
                m_ActivationWorkPlaceThread.Abort();
                m_ActivationWorkPlaceThread = null;
            }

            //останавливаем таймер, который анализирует и исполняет очередь действий
            m_Stop = true;
        }

        private void TimerTick(object Sender, EventArgs E)
        {
            Action act;

            if (m_Stop)
            {
                m_Timer.Stop();
                return;
            }

            while (m_ActionQueue.TryDequeue(out act))
                act.Invoke();
        }

        private void StartQueueAnalizer()
        {
            //запуск анализатора очереди действий
            m_Stop = false;
            m_Timer.Start();
        }

        private void ButtonActivationWorkPlace_OnClick(object sender, RoutedEventArgs e)
        {
            //активируем рабочее место
            Cache.Net.ActivationWorkPlace(Types.ComplexParts.Sctu, ChByClampType, Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE);
        }

        private void AddWorkPlaceIsFreeEvent()
        {
            //демонстрация только кнопки активации
            m_ActionQueue.Enqueue(delegate
            {
                btnActivation.Visibility = Visibility.Visible;
                lbWorkPlaceIsBlocked.Visibility = Visibility.Collapsed;
                this.Background = null;

                if (BrushScrollViewerChanged)
                    Cache.Main.scrollViewer.Background = PreviousBrushScrollViewer;

                //запрещаем данному рабочему месту занимать шину CAN
                Cache.Net.SetPermissionToUseCanDataBus(false);
            });
        }

        private void AddWorkPlaceIsBlockedEvent()
        {
            //демонстрация надписи о заблокированном рабочем месте на чёрном фоне
            m_ActionQueue.Enqueue(delegate
            {
                btnActivation.Visibility = Visibility.Collapsed;
                lbWorkPlaceIsBlocked.Visibility = Visibility.Visible;
                this.Background = Brushes.Black;
                Cache.Main.scrollViewer.Background = Brushes.Black;

                //запрещаем данному рабочему месту занимать шину CAN
                Cache.Net.SetPermissionToUseCanDataBus(false);

                //запоминаем, что мы изменили значение Cache.Main.scrollViewer.Background
                BrushScrollViewerChanged = true;
            });
        }

        private void AddNavigateSctuPageEvent()
        {
            //открываем SctuPage
            m_ActionQueue.Enqueue(delegate
            {
                Cache.Main.mainFrame.Navigate(Cache.SctuPage);

                //разрешаем данному рабочему месту использование шины CAN
                Cache.Net.SetPermissionToUseCanDataBus(true);
            });
        }

        private void AddShowMessageEvent(string Message)
        {
            m_ActionQueue.Enqueue(delegate
            {
                var dw = new DialogWindow("Ошибка", Message);
                dw.ButtonConfig(DialogWindow.EbConfig.OK);
                dw.ShowDialog();
            });
        }

        private Types.SCTU.SctuWorkPlaceActivationStatuses ActivationWorkPlaceThreadWorker()
        {
            //считываем значение статуса активации
            ushort ActivationStatus;
            string Error = "";

            try
            {
                ActivationStatus = Cache.Net.ReadRegister(Types.ComplexParts.Sctu, REG_WORKPLACE_ACTIVATION_STATUS);
            }
            catch (Exception ex)
            {
                Error = string.Format("Error while reading register number {0}.", REG_WORKPLACE_ACTIVATION_STATUS.ToString());
                throw new Exception(Error + "\n" +
                                    ex.Message);
            }

            switch (ActivationStatus)
            {
                case ((ushort)Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE):
                    //рабочее место свободно, его можно активировать
                    this.AddWorkPlaceIsFreeEvent();
                    return Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE;

                case ((ushort)Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE):
                    this.AddNavigateSctuPageEvent();
                    return Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE;

                case ((ushort)Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED):
                    //рабочее место заблокировано
                    this.AddWorkPlaceIsBlockedEvent();
                    return Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED;

                default:
                    //случай, когда мы добавили новое значение во множество значений Types.SCTU.SctuWorkPlaceActivationStatuses, а в данной реализации не предусмотрели для него обработку
                    Error = string.Format("In this implementation, there is no provision for processing the entire set of all values 'Types.SCTU.SctuWorkPlaceActivationStatuses'. Value={0}.", ActivationStatus.ToString());
                    AddShowMessageEvent(Error);
                    return Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE;
            }
        }

        private void StartThread()
        {
            //создаём поток, который будет опрашивать состояние регистра статуса активации рабочего места и управлять видом ActivationWorkPlacePage
            m_ActivationWorkPlaceThread = new Thread(delegate ()
            {
                try
                {
                    //поток работает до момента активации рабочего места, т.е. как только рабочее место активировано - он завершает свою работу
                    while (ActivationWorkPlaceThreadWorker() != Types.SCTU.SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE)
                    {
                        Thread.Sleep(REQUEST_DELAY_MS);
                    }
                }
                catch (Exception ex)
                {
                    AddShowMessageEvent(ex.Message);
                }
            });

            //запускаем выполнение потока
            m_ActivationWorkPlaceThread.Start();
        }

        private void ActivationPage_Loaded(object sender, RoutedEventArgs e)
        {
            //запоминаем цвет Cache.Main.scrollViewer чтобы в реализации ActivationPage_Unloaded вернуть его назад 
            PreviousBrushScrollViewer = Cache.Main.scrollViewer.Background;

            //запускаем таймер, который будет просматривать очередь поставленных на исполнение действий и исполнять их по мере поступления в главном потоке приложения т.к. исполнять их в фоновом потоке m_ActivationWorkPlaceThread нельзя
            StartQueueAnalizer();

            //создаём и запускаем поток, который будет опрашивать значение регистра активации и в зависимости от его значения будет изменять внешний вид этой формы
            StartThread();
        }

        private void ActivationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            //мы могли изменить цвет Cache.Main.scrollViewer, поэтому возвращаем то значение, которое было на момент выполнения ActivationPage_Loaded  
            if (BrushScrollViewerChanged)
                Cache.Main.scrollViewer.Background = PreviousBrushScrollViewer;

            //останавливаем анализатор очереди действий и поток 
            this.Free();
        }

    }
}
