using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace SCME.Types.Utils
{
    public enum LogJournalMessageType
    {
        Note,
        Warning,
        Problem,
        Error,
        Info 
    };

    internal class LogItem
    {
        public string Source { get; set; }
        public LogJournalMessageType MessageType { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LogJournal
    {
        private readonly ConcurrentQueue<LogItem> m_MessageList;
        private readonly AutoResetEvent m_ListEvent;
        private Thread m_LogWriteThread;
        private bool m_Detailed;

        private volatile bool m_IsThreadClosed, m_ShutdownRequest;

        /// <summary>
        ///     Event journal class
        /// </summary>
        public LogJournal()
        {
            m_MessageList = new ConcurrentQueue<LogItem>();
            m_ListEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Open journal to write logs
        /// </summary>
        /// <param name="LogTracePath">Path to text log file</param>
        /// <param name="ForceLogTraceFlush">Force flushing to disk after each write</param>
        /// <param name="Detailed">Include info records</param>
        public void Open(string LogTracePath, bool ForceLogTraceFlush = true, bool Detailed = false)
        {
            m_Detailed = Detailed;

            if (m_LogWriteThread != null)
                m_LogWriteThread.Abort();
            m_LogWriteThread = new Thread(ThreadPoolCallback) { IsBackground = true, Priority = ThreadPriority.Lowest };

            Trace.Listeners.Clear();
            var listener = new TextWriterTraceListener(LogTracePath, @"FileLog") { IndentSize = 8 };
            Trace.Listeners.Add(listener);
            Trace.AutoFlush = ForceLogTraceFlush;

            m_IsThreadClosed = false;
            m_LogWriteThread.Start();
        }

        /// <summary>
        /// Close journal to write logs
        /// </summary>
        public void Close()
        {
            m_ShutdownRequest = true;
            m_ListEvent.Set();

            while (!m_IsThreadClosed)
                Thread.Sleep(10);

            Trace.Flush();
        }

        /// <summary>
        ///     Append log file
        /// </summary>
        /// <param name="Device">Device that triggered log append</param>
        /// <param name="Type">Type of log message</param>
        /// <param name="Message">Message of log item</param>
        public void AppendLog(string Device, LogJournalMessageType Type, string Message)
        {
            Message = Message.Replace("'", string.Empty);

            m_MessageList.Enqueue(new LogItem
                {
                    Timestamp = DateTime.Now,
                    Source = Device,
                    MessageType = Type,
                    Message = Message
                });

            m_ListEvent.Set();
        }

        #region Private methods

        private void ThreadPoolCallback(object Parameter)
        {
            try
            {
                while (!m_ShutdownRequest || (m_MessageList.Count > 0))
                {
                    LogItem item;

                    while (m_MessageList.TryDequeue(out item))
                    {
                        if (!m_Detailed && item.MessageType == LogJournalMessageType.Note)
                            continue;

                        switch (item.MessageType)
                        {
                            case LogJournalMessageType.Note:
                            case LogJournalMessageType.Info:
                                Trace.WriteLine(String.Format("{0} INFORMATION - {1}: {2}", item.Timestamp,
                                                              item.Source, item.Message));
                                break;
                            case LogJournalMessageType.Warning:
                            case LogJournalMessageType.Problem:
                                Trace.WriteLine(String.Format("{0} WARNING - {1}: {2}", item.Timestamp,
                                                              item.Source, item.Message));
                                break;
                            case LogJournalMessageType.Error:
                                Trace.WriteLine(String.Format("{0} ERROR - {1}: {2}", item.Timestamp,
                                                              item.Source, item.Message));
                                break;
                        }
                    }
                    
                    if(!m_ShutdownRequest)
                        m_ListEvent.WaitOne();
                }
            }
            finally
            {
                m_IsThreadClosed = true;
            }
        }

        #endregion
    }
}