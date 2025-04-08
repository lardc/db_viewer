using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using SCME.Types;
using ThreadState = System.Threading.ThreadState;

namespace SCME.Logger
{
    public class EventJournal
    {
        private static readonly object ms_Locker = new object();

        private readonly ConcurrentQueue<LogItem> m_MessageList;
        private readonly AutoResetEvent m_ListEvent;
        private SQLiteConnection m_Connection;
        private SQLiteCommand m_InsertCommand;
        private Thread m_LogWriteThread;
        private bool m_Detailed, m_ThreadStarted;

        private volatile bool m_IsThreadClosed, m_ShutdownRequest;

        /// <summary>
        ///     Event journal class
        /// </summary>
        public EventJournal()
        {
            m_MessageList = new ConcurrentQueue<LogItem>();
            m_ListEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Open journal to write logs
        /// </summary>
        /// <param name="LogDatabasePath">Path to database source</param>
        /// <param name="LogDatabaseOptions">Additional connect options</param>
        /// <param name="LogTracePath">Path to text log file</param>
        /// <param name="ForceLogTraceFlush">Force flushing to disk after each write</param>
        /// <param name="Detailed">Include info records</param>
        public void Open(string LogDatabasePath, string LogDatabaseOptions, string LogTracePath, bool ForceLogTraceFlush = true, bool Detailed = false)
        {
            m_Detailed = Detailed;

            if (m_LogWriteThread != null)
                m_LogWriteThread.Abort();
            m_LogWriteThread = new Thread(ThreadPoolCallback) { IsBackground = true, Priority = ThreadPriority.Lowest };

            if (!String.IsNullOrWhiteSpace(LogDatabasePath))
            {
                m_Connection = new SQLiteConnection(String.Format("data source={0};{1}", LogDatabasePath, LogDatabaseOptions), false);
                m_Connection.Open();
                m_InsertCommand = m_Connection.CreateCommand();
            }

            Trace.Listeners.Clear();
            var listener = new TextWriterTraceListener(LogTracePath, @"FileLog") { IndentSize = 8 };
            Trace.Listeners.Add(listener);
            Trace.AutoFlush = ForceLogTraceFlush;

            m_IsThreadClosed = false;
            m_LogWriteThread.Start();
            m_ThreadStarted = true;
        }

        /// <summary>
        /// Close journal to write logs
        /// </summary>
        public void Close()
        {
            try
            {
                m_ShutdownRequest = true;
                m_ListEvent.Set();

                if (m_ThreadStarted)
                    while (!m_IsThreadClosed)
                        Thread.Sleep(10);

                Trace.Flush();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                    m_Connection.Close();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Append log file
        /// </summary>
        /// <param name="Device">Device that triggered log append</param>
        /// <param name="Type">Type of log message</param>
        /// <param name="Message">Message of log item</param>
        public void AppendLog(ComplexParts Device, LogMessageType Type, string Message)
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

        /// <summary>
        /// Read log sequence from the end to the beginning
        /// </summary>
        public List<LogItem> ReadFromEnd(long Tail, long Count)
        {
            lock (ms_Locker)
            {
                var logs = new List<LogItem>();

                if (m_Connection == null || m_Connection.State != ConnectionState.Open)
                    return logs;

                try
                {
                    var cmd = m_Connection.CreateCommand();
                    cmd.CommandText =
                        string.Format(
                            "SELECT * FROM MainTable WHERE MainTable.ID < {0} ORDER BY MainTable.ID DESC LIMIT {1}",
                            Tail, Count);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new LogItem
                                {
                                    ID = (Int64) reader[0],
                                    Timestamp = DateTime.Parse((string) reader[1], CultureInfo.InvariantCulture),
                                    Source = (ComplexParts)(byte) reader[2],
                                    MessageType = (LogMessageType)(byte)reader[3],
                                    Message = (string) reader[4]
                                });
                        }
                    }
                }
                catch (Exception ex)
                {
                    logs.Add(new LogItem
                        {
                            ID = 0,
                            Timestamp = DateTime.Now,
                            Source = 0,
                            MessageType = 0,
                            Message = ex.Message
                        });
                }

                return logs;
            }
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
                        if (!m_Detailed && item.MessageType == LogMessageType.Note)
                            continue;

                        InsertIntoDatabase(item.Timestamp, item.Source,
                                           item.MessageType, item.Message);

                        switch (item.MessageType)
                        {
                            case LogMessageType.Note:
                            case LogMessageType.Info:
                                Trace.WriteLine(String.Format("{0} INFORMATION - {1}: {2}", item.Timestamp,
                                                              item.Source, item.Message));
                                break;
                            case LogMessageType.Warning:
                            case LogMessageType.Problem:
                                Trace.WriteLine(String.Format("{0} WARNING - {1}: {2}", item.Timestamp,
                                                              item.Source, item.Message));
                                break;
                            case LogMessageType.Error:
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

        private void InsertIntoDatabase(DateTime Timestamp, ComplexParts Source, LogMessageType Type, string Message)
        {
            lock (ms_Locker)
            {
                if (m_Connection == null || m_Connection.State != ConnectionState.Open)
                    return;
                
                try
                {
                    m_InsertCommand.CommandText =
                        string.Format(
                            "INSERT INTO MainTable(ID, DateTimeStamp, SourceID, TypeID, Message) VALUES(NULL, '{0}', {1}, {2}, '{3}')",
                            Timestamp.ToString(@"yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), (byte)Source, (byte)Type, Message);

                    m_InsertCommand.ExecuteNonQuery();
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion
    }
}