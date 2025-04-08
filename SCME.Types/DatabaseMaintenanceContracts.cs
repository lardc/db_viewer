using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace SCME.Types
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class GeneralTableRecord
    {
        [DataMember]
        public List<string> Values { get; set; }

        public GeneralTableRecord()
        {
            Values = new List<string>();
        }
    }
    
    [ServiceContract(Namespace = "http://proton-electrotex.com/SCME", SessionMode = SessionMode.Required)]
    public interface IDatabaseMaintenanceService
    {
        [OperationContract]
        [FaultContract(typeof (FaultData))]
        void Check();

        [OperationContract]
        [FaultContract(typeof (FaultData))]
        void RemoveGroup(string GroupName);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        List<GeneralTableRecord> GetTableData(string TableName, long LastID, int TransferSize);
    }
}