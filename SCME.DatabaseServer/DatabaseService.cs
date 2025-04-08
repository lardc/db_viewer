using System.ServiceProcess;

namespace SCME.DatabaseServer
{
    public partial class DatabaseService : ServiceBase
    {
        public DatabaseService()
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
