using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.ServiceProcess;
using System.Windows.Forms;
using SCME.DatabaseServer.Properties;

namespace SCME.DatabaseServer
{
    static class Program
    {
        private static NotifyIcon ms_TrayIcon;

        private static void Main(string[] Args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            if (Environment.UserInteractive)
            {
                var parameter = string.Concat(Args);

                switch (parameter)
                {
                    case "--install":
                        StartElevated("--installElevated");
                        break;
                    case "--uninstall":
                        StartElevated("--uninstallElevated");
                        break;
                    case "--installElevated":
                        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstallElevated":
                        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                    default:
                        StartAsApplication();
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new DatabaseService());
            }
        }

        private static void StartElevated(string Args)
        {
            var info = new ProcessStartInfo(Assembly.GetEntryAssembly().Location, Args)
            {
                Verb = @"runas",
            };

            using (var process = new Process { StartInfo = info })
            {
                process.Start();
            }
        }      

        private static void StartAsApplication()
        {
            SystemHost.StartService();

            var ico = Resources.ServiceIcon;
            ms_TrayIcon = new NotifyIcon
            {
                Text = @"SCME.DatabaseServer: " + SystemHost.GetPort().ToString(),
                Icon = new Icon(ico, ico.Width, ico.Height),
                ContextMenu = new ContextMenu(new[] { new MenuItem(@"Exit", OnExit) }),
                Visible = true
            };

            Application.ApplicationExit += Application_ApplicationExit;
            Application.Run();
        }

        static void Application_ApplicationExit(object Sender, EventArgs E)
        {
            SystemHost.StopService();
        }

        private static void OnExit(object Sender, EventArgs E)
        {
            ms_TrayIcon.Visible = false;
            Application.Exit();
        }

        private static void CurrentDomainUnhandledException(object Sender, UnhandledExceptionEventArgs E)
        {
            SystemHost.LogCriticalErrorMessage((Exception)E.ExceptionObject);
        }
    }
}
