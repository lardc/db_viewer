using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using SCME.Types;

namespace SCME.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
        Namespace = @"http://proton-electrotex.com/SCME",
        ConcurrencyMode = ConcurrencyMode.Single)]
    public class MaintenanceServer : IDatabaseMaintenanceService
    {
        internal MaintenanceServer()
        {
        }

        #region Interface implementation

        void IDatabaseMaintenanceService.Check()
        {

        }

        void IDatabaseMaintenanceService.RemoveGroup(string GroupName)
        {
            try
            {
                SystemHost.Results.RemoveGroup(GroupName);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }            
        }

        List<GeneralTableRecord> IDatabaseMaintenanceService.GetTableData(string TableName, long LastID, int TransferSize)
        {
            try
            {
                return SystemHost.Results.GetTableData(TableName, LastID, TransferSize);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }       
        }

        #endregion
    }
}