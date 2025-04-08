using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using SCME.ExcelPrinting;
using SCME.NetworkPrinting.Properties;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.Utils;

namespace SCME.NetworkPrinting
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
        Namespace = "http://proton-electrotex.com/SCME",
        ConcurrencyMode = ConcurrencyMode.Single)]
    public class PrintingServer : IPrintingService
    {
        private const string DATABASE_SERVER_ENDPOINT_NAME = "SCME.Service.DatabaseServer";

        private DatabaseCommunicationProxy m_DatabaseClient;

        bool IPrintingService.RequestRemotePrinting(string MMECode, string GroupName, string CustomerName, string DeviceType, ReportSelectionPredicate Predicate)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(Dummy => ImplementDeferredPrinting(MMECode, GroupName, CustomerName, DeviceType, Predicate));

                return true;
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(new FaultData
                {
                    Device = ComplexParts.None,
                    Message = ex.Message,
                    TimeStamp = DateTime.Now
                });
            }
        }

        private void ImplementDeferredPrinting(string MMECode, string GroupName, string CustomerName, string DeviceType,
            ReportSelectionPredicate Predicate)
        {
            SystemHost.Journal.AppendLog("PrintingServer", LogJournalMessageType.Note,
                String.Format(
                    "Printing request: ID - {0}, Group - {1}, Customer - {2}, Device - {3}, Predicate - {4}",
                    MMECode, GroupName, CustomerName, DeviceType, Predicate));

            var clientsListPath = Settings.Default.ClientsListPath;
            if (!Path.IsPathRooted(clientsListPath))
                clientsListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, clientsListPath);

            var clientsList = new ClientsEngine(clientsListPath);
            var record = clientsList.ClientRecords.FirstOrDefault(Arg => Arg.MMECode == MMECode);

            if (record == null)
            {
                SystemHost.Journal.AppendLog("PrintingServer", LogJournalMessageType.Error,
                    String.Format("Equipment ID {0} is not found in the configuration list", MMECode));

                return;
            }

            var deviceItemsWithParams = new List<DeviceItemWithParams>();
            var conditions = new List<ConditionItem>();
            var normatives = new List<ParameterNormativeItem>();

            try
            {
                using (var centralDbClient = new CentralDatabaseServiceClient())
                {
                    var devices = centralDbClient.GetDevices(GroupName);

                    deviceItemsWithParams.AddRange(from deviceItem in devices
                                                   let error = centralDbClient.ReadDeviceErrors(deviceItem.InternalID).FirstOrDefault()
                                                   let devParams = centralDbClient.ReadDeviceParameters(deviceItem.InternalID)
                                                   where
                                                       (error == 0) && (Predicate == ReportSelectionPredicate.QC) ||
                                                       (error != 0) && (Predicate == ReportSelectionPredicate.Rejected) ||
                                                       (Predicate == ReportSelectionPredicate.All)
                                                   select new DeviceItemWithParams
                                                   {
                                                       GeneralInfo = deviceItem,
                                                       Parameters = devParams,
                                                       DefectCode = error
                                                   });

                    if (deviceItemsWithParams.Count > 0)
                    {
                        conditions =
                            centralDbClient.ReadDeviceConditions(deviceItemsWithParams[0].GeneralInfo.InternalID);
                        normatives =
                            centralDbClient.ReadDeviceNormatives(deviceItemsWithParams[0].GeneralInfo.InternalID);
                    }
                }
            }
            catch (Exception)
            {

                #region Local Database
                try
                {
                    m_DatabaseClient = new DatabaseCommunicationProxy(DATABASE_SERVER_ENDPOINT_NAME,
                        String.Format(Settings.Default.DatabaseServerAddressTemplate,
                            record.IPAddress));

                    m_DatabaseClient.Open();

                    try
                    {
                        var devices = m_DatabaseClient.ReadDevices(GroupName);

                        deviceItemsWithParams.AddRange(from deviceItem in devices
                                                       let error = m_DatabaseClient.ReadDeviceErrors(deviceItem.InternalID).FirstOrDefault()
                                                       let devParams = m_DatabaseClient.ReadDeviceParameters(deviceItem.InternalID)
                                                       where
                                                           (error == 0) && (Predicate == ReportSelectionPredicate.QC) ||
                                                           (error != 0) && (Predicate == ReportSelectionPredicate.Rejected) ||
                                                           (Predicate == ReportSelectionPredicate.All)
                                                       select new DeviceItemWithParams
                                                       {
                                                           GeneralInfo = deviceItem,
                                                           Parameters = devParams,
                                                           DefectCode = error
                                                       });

                        if (deviceItemsWithParams.Count > 0)
                        {
                            conditions =
                                m_DatabaseClient.ReadDeviceConditions(deviceItemsWithParams[0].GeneralInfo.InternalID);
                            normatives =
                                m_DatabaseClient.ReadDeviceNormatives(deviceItemsWithParams[0].GeneralInfo.InternalID);
                        }
                    }
                    finally
                    {
                        if (m_DatabaseClient.State != CommunicationState.Faulted)
                            m_DatabaseClient.Close();
                        else
                            m_DatabaseClient.Abort();
                    }
                }
                catch (FaultException<FaultData> ex)
                {
                    SystemHost.Journal.AppendLog("PrintingServer", LogJournalMessageType.Error,
                        String.Format("Error while retrieving information from MME local database: {0}", ex.Message));

                    return;
                }
                catch (Exception ex)
                {
                    SystemHost.Journal.AppendLog("PrintingServer", LogJournalMessageType.Error,
                        String.Format("Error while retrieving information from MME local database: {0}", ex.Message));

                    return;
                }
                #endregion
            }

            

            try
            {
                var templatePath = record.Use2PosTemplate ? Settings.Default.TemplateModDev : Settings.Default.TemplateTabDev;

                if (!Path.IsPathRooted(templatePath))
                    templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templatePath);

                var excelPrinter = new ExcelPrinter
                {
                    TemplateXlsFilePath = templatePath,
                    Copies = 1,
                    PrinterName = record.PrinterName,
                    SaveToFile = false,
                    SaveXlsFilePath = ""
                };

                excelPrinter.CreateXlsReport(record.Use2PosTemplate,
                    new ReportInfo
                    {
                        CustomerName = CustomerName,
                        GroupName = GroupName,
                        ModuleType = DeviceType,
                        Conditions = conditions,
                        Normatives = normatives
                    }, deviceItemsWithParams, false, false);

                SystemHost.Journal.AppendLog("PrintingServer", LogJournalMessageType.Info,
                    String.Format("Printing has been succeeded for {0}[{1}] on {2}", MMECode, record.IPAddress,
                        record.PrinterName));
            }
            catch (Exception ex)
            {
                SystemHost.Journal.AppendLog("PrintingServer", LogJournalMessageType.Error,
                    String.Format("Error while creating report: {0}", ex.Message));
            }
        }
    }
}
