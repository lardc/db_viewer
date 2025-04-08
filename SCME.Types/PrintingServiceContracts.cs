using System.ServiceModel;

namespace SCME.Types
{
    [ServiceContract(Namespace = "http://proton-electrotex.com/SCME",
        SessionMode = SessionMode.Required)]
    public interface IPrintingService
    {
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        bool RequestRemotePrinting(string MMECode, string GroupName, string CustomerName, string DeviceType, ReportSelectionPredicate Predicate);
    }
}