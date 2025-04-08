using System.ServiceModel;

namespace SCME.Types
{
    public class PrintingServiceProxy : ClientBase<IPrintingService>, IPrintingService
    {
        public PrintingServiceProxy(string ServerEndpointConfigurationName)
            : base(ServerEndpointConfigurationName)
        {
        }

        public bool RequestRemotePrinting(string MMECode, string GroupName, string CustomerName, string DeviceType,
            ReportSelectionPredicate Predicate)
        {
            return Channel.RequestRemotePrinting(MMECode, GroupName, CustomerName, DeviceType, Predicate);
        }
    }
}