using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace SCME.Agent
{
    internal static class Program
    {
        private static Supervisor ms_Supervisor;

        [STAThread]
        private static void Main()
        {
            Mutex mutex = null; 
            var mutexCreated = false;

            for (var i = 0; i < 3; ++i)
            {
                mutex = new Mutex(true, @"Global\SCME.AGENT.Mutex", out mutexCreated);

                if (mutexCreated)
                    break;
            }

            if (!mutexCreated)
            {
                Process.Start("explorer.exe");
                return;
            }

            using (mutex)
            {
                ms_Supervisor = new Supervisor();
                ms_Supervisor.Start();

                Application.Run();
            }
        }
    }
}