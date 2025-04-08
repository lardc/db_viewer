using System.ServiceProcess;

namespace SCME.NetworkPrinting
{
    public partial class NetworkPrintingService : ServiceBase
    {
        public NetworkPrintingService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] Args)
        {
            base.OnStart(Args);

            SystemHost.StartService();
        }

        protected override void OnStop()
        {
            base.OnStop();

            SystemHost.StopService();
        }
    }
}
