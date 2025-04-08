using System;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Windows.Forms;
using SCME.Logger;
using SCME.Service.Properties;
using SCME.Types;

namespace SCME.Service
{
    internal static class SystemHost
    {
        private static ExternalControlServer ms_ControlService;
        private static ServiceHost ms_ControlServiceHost;
        private static ServiceHost ms_DatabaseServiceHost;
        private static ServiceHost ms_MaintenanceServiceHost;
        private static BroadcastCommunication m_Communication;

        internal static bool? IsSyncedWithServer { get; private set; }

        private static void AfterSyncWithServerRoutineHandler(string notSyncedReason)
        {
            //данная реализация будет вызвана после того, как будет пройдена стадия синхронизации данных SQLLite базы данных с данными центральной базы
            switch (notSyncedReason == String.Empty)
            {
                case true:
                    //если принятый notSyncedReason пуст - синхронизация успешно выполнена
                    IsSyncedWithServer = true;
                    Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, "Local database was successfully synced with a central database");
                    break;

                default:
                    //синхронизация не выполнена. описание причины содержится в notSyncedReason
                    IsSyncedWithServer = false;

                    LogMessageType logMessageType;
                    switch (Settings.Default.DisableResultDB)
                    {
                        case true:
                            //синхронизация не выполнена потому, что отключена параметром в конфигурационном файле
                            logMessageType = LogMessageType.Info;
                            break;

                        default:
                            //синхронизация не выполнена потому, что в процессе синхронизации произошла ошибка
                            logMessageType = LogMessageType.Error;
                            break;
                    }

                    Journal.AppendLog(ComplexParts.Service, logMessageType, string.Format("Local database not synced with a central database. Reason: {0}", notSyncedReason));
                    break;
            }

            //процесс синхронизации завершён, сообщаем об этом UI
            FireSyncDBAreProcessedEvent();
        }

        internal static void SetCommunication(BroadcastCommunication Communication)
        {
            m_Communication = Communication;
        }

        internal static bool Initialize()
        {
            try
            {
                Journal = new EventJournal();
                Journal.Open(Settings.Default.DisableLogDB ? String.Empty : Settings.Default.LogsDatabasePath, Settings.Default.DBOptionsLogs,
                             String.Format(Settings.Default.LogsTracePathTemplate, DateTime.Now.ToString(CultureInfo.CurrentCulture).Replace('/', '_').Replace(':', '_')),
                             Settings.Default.ForceLogFlush,
                             Settings.Default.IncludeDetailsInLog);
                Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, Resources.Log_SystemHost_Application_started);
            }
            catch (Exception ex)
            {
                File.AppendAllText(@"SCME.Service error.txt",
                    $"\n\n{DateTime.Now}\nEXCEPTION: {ex}\nINNER EXCEPTION: {ex.InnerException ?? new Exception("No additional information - InnerException is null")}\n");

                return false;
            }

            try
            {
                Results = new ResultsJournal();
                Results.Open(Settings.Default.DisableResultDB ? String.Empty : Settings.Default.ResultsDatabasePath, Settings.Default.DBOptionsResults, Settings.Default.MMECode);

                if (!Settings.Default.DisableResultDB)
                    Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, Resources.Log_SystemHost_Result_journal_opened);

                //нам ещё не известно как завершится процесс синхронизации данных, поэтому
                IsSyncedWithServer = null;

                switch (Settings.Default.DisableResultDB)
                {
                    case true:
                        //синхронизация отключена, уведомляем UI, что стадия синхронизации баз данных пройдена
                        AfterSyncWithServerRoutineHandler("Synchronization of the local database with a central database is prohibited by parameter DisableResultDB");
                        break;

                    default:
                        //запускаем в потоке синхронизацию результатов измерений и профилей 
                        Results.SyncWithServer(AfterSyncWithServerRoutineHandler);
                        break;
                }

                ms_ControlService = new ExternalControlServer();
                ms_ControlServiceHost = new ServiceHost(ms_ControlService);
                ms_ControlServiceHost.Open();
                Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, String.Format(Resources.Log_SystemHost_Control_service_is_listening));

                ms_DatabaseServiceHost = new ServiceHost(typeof(DatabaseServer));

                try
                {
                    ms_DatabaseServiceHost.AddServiceEndpoint(typeof(IDatabaseCommunicationService), new NetTcpBinding("DefaultTcpBinding"), Settings.Default.DBServiceExternalEndpoint);
                }
                catch (Exception ex)
                {
                    Journal.AppendLog(ComplexParts.Service, LogMessageType.Warning, String.Format("Can't open external database service port: {0}", ex.Message));
                }

                ms_DatabaseServiceHost.Open();
                Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, String.Format(Resources.Log_SystemHost_Database_service_is_listening));

                ms_MaintenanceServiceHost = new ServiceHost(typeof(MaintenanceServer));

                try
                {
                    ms_MaintenanceServiceHost.AddServiceEndpoint(typeof(IDatabaseMaintenanceService), new NetTcpBinding("DefaultTcpBinding"), Settings.Default.MaintenanceServiceExternalEndpoint);
                }
                catch (Exception ex)
                {
                    Journal.AppendLog(ComplexParts.Service, LogMessageType.Warning, String.Format("Can't open external maintenance service port: {0}", ex.Message));
                }

                ms_MaintenanceServiceHost.Open();
                Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, String.Format(Resources.Log_SystemHost_Maintenance_service_is_listening));

                return true;
            }
            catch (Exception ex)
            {
                Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, ex.Message);
                Journal.Close();

                return false;
            }
        }

        internal static EventJournal Journal { get; private set; }
        internal static ResultsJournal Results { get; private set; }

        internal static void Close()
        {
            try
            {
                if (ms_ControlServiceHost != null)
                {
                    try
                    {
                        (ms_ControlService as IExternalControl).Deinitialize();
                    }
                    finally
                    {
                        if (ms_ControlServiceHost.State == CommunicationState.Faulted)
                            ms_ControlServiceHost.Abort();
                        else
                            ms_ControlServiceHost.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                if (Journal != null)
                    Journal.AppendLog(ComplexParts.Service, LogMessageType.Error,
                                        String.Format(Resources.Log_SystemHost_Error_while_closing, @"Control host", ex.Message));
            }

            try
            {
                if (ms_DatabaseServiceHost != null)
                    if (ms_DatabaseServiceHost.State == CommunicationState.Faulted)
                        ms_DatabaseServiceHost.Abort();
                    else
                        ms_DatabaseServiceHost.Close();
            }
            catch (Exception ex)
            {
                if (Journal != null)
                    Journal.AppendLog(ComplexParts.Service, LogMessageType.Error,
                                        String.Format(Resources.Log_SystemHost_Error_while_closing, @"DB host", ex.Message));
            }

            try
            {
                if (ms_MaintenanceServiceHost != null)
                    if (ms_MaintenanceServiceHost.State == CommunicationState.Faulted)
                        ms_MaintenanceServiceHost.Abort();
                    else
                        ms_MaintenanceServiceHost.Close();
            }
            catch (Exception ex)
            {
                if (Journal != null)
                    Journal.AppendLog(ComplexParts.Service, LogMessageType.Error,
                                        String.Format(Resources.Log_SystemHost_Error_while_closing, @"Maintenance host", ex.Message));
            }

            try
            {
                Results.Close();
            }
            catch (Exception ex)
            {
                if (Journal != null)
                    Journal.AppendLog(ComplexParts.Database, LogMessageType.Error,
                                        String.Format(Resources.Log_SystemHost_Error_while_closing, @"Result journal", ex.Message));
            }

            if (Journal != null)
            {
                Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, Resources.Log_SystemHost_Application_closed);
                Journal.Close();
            }
        }

        private static void FireSyncDBAreProcessedEvent()
        {
            string sCommunicationLive = String.Empty;

            switch (m_Communication == null)
            {
                case false:
                    m_Communication.PostSyncDBAreProcessedEvent();
                    break;

                default:
                    sCommunicationLive = "comunication object is not live (null value)";
                    break;
            }

            if (Journal != null)
            {
                string Mess = "Sync DataBases are processed";

                if (sCommunicationLive != String.Empty)
                    Mess = Mess + ", " + sCommunicationLive;

                Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, Mess);
            }
        }
    }
}