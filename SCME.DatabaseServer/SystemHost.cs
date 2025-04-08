using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using SCME.DatabaseServer.Properties;
using SCME.InterfaceImplementations;
using SCME.Types.Utils;

namespace SCME.DatabaseServer
{
    internal static class SystemHost
    {
        private static ServiceHost ms_ServiceHost; 

        internal static LogJournal Journal { get; private set; }

        internal static void StartService()
        {
            Journal = new LogJournal();

            try
            {
                var path = String.Format(Settings.Default.LogPathTemplate,
                    DateTime.Now.ToString(CultureInfo.CurrentCulture).Replace('/', '_').Replace(':', '_'));

                if (!Path.IsPathRooted(path))
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

                Journal.Open(path, true, true);
            }
            catch (Exception ex)
            {
                LogCriticalErrorMessage(ex);
                return;
            }

            try
            {
                var service = new SQLCentralDatabaseService(Settings.Default);
                ms_ServiceHost = new ServiceHost(service);
                ms_ServiceHost.Open();
            }
            catch (Exception ex)
            {
                Journal.AppendLog("SystemHost", LogJournalMessageType.Error,
                    $"Error starting database service: {ex.Message}");
                return;
            }

            Journal.AppendLog("SystemHost", LogJournalMessageType.Info,
                $"SCME database service started on {ms_ServiceHost.BaseAddresses.FirstOrDefault()}");
        }

        internal static void StopService()
        {
            try
            {
                if (ms_ServiceHost != null)
                {
                    if (ms_ServiceHost.State != CommunicationState.Opened)
                        ms_ServiceHost.Abort();
                    else
                        ms_ServiceHost.Close();

                    ms_ServiceHost = null;
                }
            }
            catch (Exception ex)
            {
                Journal.AppendLog("SystemHost", LogJournalMessageType.Warning,
                    $"SCME database service can not be stopped properly: {ex.Message}");
            }

            if (Journal != null)
            {
                Journal.AppendLog("SystemHost", LogJournalMessageType.Info, "SCME database service stopped");
                Journal.Close();
                Journal = null;
            }
        }

        internal static void LogCriticalErrorMessage(Exception Ex)
        {
            try
            {
                File.AppendAllText(@"SCME.DatabseServer error.txt",
                    $"\n\n{DateTime.Now}\nEXCEPTION: {Ex}\nINNER EXCEPTION: {Ex.InnerException ?? new Exception("No additional information - InnerException is null")}\n");
            }
            catch
            {
            }
        }

        public static int GetPort()
        {
            //возвращает номер слушающего порта сервера:
            //  успешный результат возврата всегда больше нуля;
            //  возвращает -1 при не успешном чтении номера слушающего порта сервера
            int result = -1;

            if (ms_ServiceHost != null)
            {
                try
                {
                    switch (ms_ServiceHost.ChannelDispatchers.Count)
                    {
                        case 1:
                            result = ms_ServiceHost.ChannelDispatchers.FirstOrDefault().Listener.Uri.Port;
                            break;

                        default:
                            result = -1;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Journal.AppendLog("SystemHost", LogJournalMessageType.Error, $"Error getting listener port from database service: {ex.Message}");
                    result = -1;
                }
            }

            return result;
        }
    }
}
