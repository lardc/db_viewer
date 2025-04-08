using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using SCME.NetworkPrinting.Properties;
using SCME.Types.Utils;

namespace SCME.NetworkPrinting
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
                ms_ServiceHost = new ServiceHost(typeof(PrintingServer));
                ms_ServiceHost.Open();
            }
            catch (Exception ex)
            {
                Journal.AppendLog("SystemHost", LogJournalMessageType.Error,
                    String.Format("Error starting network printing service: {0}", ex.Message));
                return;
            }

            Journal.AppendLog("SystemHost", LogJournalMessageType.Info,
                String.Format("SCME Network printing service started on {0}",
                    ms_ServiceHost.BaseAddresses.FirstOrDefault()));
        }

        internal static void StopService()
        {
            try
            {
                if (ms_ServiceHost != null)
                {
                    if(ms_ServiceHost.State != CommunicationState.Opened)
                        ms_ServiceHost.Abort();
                    else
                        ms_ServiceHost.Close();

                    ms_ServiceHost = null;
                }
            }
            catch (Exception ex)
            {
                Journal.AppendLog("SystemHost", LogJournalMessageType.Warning,
                    String.Format("SCME Network printing service can not be stopped properly: {0}", ex.Message));
            }

            if (Journal != null)
            {
                Journal.AppendLog("SystemHost", LogJournalMessageType.Info, "SCME Network printing service stopped");
                Journal.Close();
                Journal = null;
            }
        }

        internal static void LogCriticalErrorMessage(Exception Ex)
        {
            try
            {
                File.AppendAllText(@"SCME.NetworkPrinting error.txt",
                    String.Format("\n\n{0}\nEXCEPTION: {1}\nINNER EXCEPTION: {2}\n", DateTime.Now, Ex,
                        Ex.InnerException ??
                        new Exception("No additional information - InnerException is null")));
            }
            catch
            {
            }
        }
    }
}
