using System;
using System.ComponentModel;
using System.Threading;

namespace SCME.Service
{
    internal class ThreadAlreadyRunning : ApplicationException
    {
        internal ThreadAlreadyRunning() : base("Thread is already running") { }
    }

    internal class ThreadFinishedEventArgs : EventArgs
    {
        internal bool Stopped { get; set; }
        internal Exception Error { get; set; }
    }

    internal class ThreadService
    {
        private int m_Inverval;
        private volatile bool m_IsThreadClosed = true;
        private volatile bool m_IsRunning;
        private Thread m_CycleThread;

        internal event EventHandler<ThreadFinishedEventArgs> FinishedHandler;

        static ThreadService()
        {
        }

        internal bool IsRunning
        {
            get { return m_IsRunning; }
        }

        internal void StartSingle(Action<object> Method, object OptionalData = null)
        {
            if (m_IsRunning)
                throw new ThreadAlreadyRunning();

            ThreadPool.QueueUserWorkItem(State =>
                {
                    var handler = FinishedHandler;
                    Exception exception = null;

                    try
                    {
                        Method(State);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    if (handler != null)
                        handler(this, new ThreadFinishedEventArgs {Error = exception});
                }, OptionalData);
        }

        internal void StartCycle(Action Method, int Interval)
        {
            if (m_IsRunning)
                throw new ThreadAlreadyRunning();

            m_Inverval = Interval;

            m_CycleThread = new Thread(delegate()
                {
                    var handler = FinishedHandler;
                    Exception exception = null;
                    m_IsRunning = true;
                    m_IsThreadClosed = false;

                    try
                    {
                        while (m_IsRunning)
                        {
                            Method();

                            Thread.Sleep(m_Inverval);
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    finally
                    {
                        m_IsThreadClosed = true;
                    }

                    handler?.Invoke(this, new ThreadFinishedEventArgs {Stopped = !m_IsRunning, Error = exception});

                    m_IsRunning = false;
                });

            m_CycleThread.Start();
        }

        internal void StopCycle(bool IsApplicationDoEventsEnabled)
        {
            if (!m_IsRunning)
                return;

            m_IsRunning = false;

            var ts = Environment.TickCount + m_Inverval * 10;
            while (!m_IsThreadClosed && Environment.TickCount < ts) { }

            m_CycleThread.Abort();
        }
    }
}