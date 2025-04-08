using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SCME.Service.Properties;

namespace SCME.Service
{
    internal static class Program
    {
        private static NotifyIcon ms_TrayIcon;
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Mutex mutex = null; 
            var mutexCreated = false;

            for (var i = 0; i < 3; ++i)
            {
                mutex = new Mutex(true, @"Global\SCME.SERVICE.Mutex", out mutexCreated);

                if (mutexCreated)
                    break;
            }

            if (!mutexCreated)
                return;

            using (mutex)
            {
                Application.ApplicationExit += ApplicationOnExit;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                var ico = Resources.ServiceIcon;
                ms_TrayIcon = new NotifyIcon
                {
                    Text = @"SCME.Service",
                    Icon = new Icon(ico, ico.Width, ico.Height),
                    ContextMenu = new ContextMenu(new[] { new MenuItem(Resources.Program_Main_Exit, OnExitCommand) }),
                    Visible = true
                };

                if (SystemHost.Initialize())
                    Application.Run();
            }
        }

        static void CurrentDomain_UnhandledException(object Sender, UnhandledExceptionEventArgs Ex)
        {
            try
            {
                File.AppendAllText(@"SCME.Service error.txt",
                    String.Format("\n\n{0}\nEXCEPTION: {1}\nINNER EXCEPTION: {2}\n", DateTime.Now, Ex.ExceptionObject,
                        ((Exception) (Ex.ExceptionObject)).InnerException ??
                        new Exception("No additional information - InnerException is null")));
            }
            catch
            {
            }
        }

        static void Application_ThreadException(object Sender, ThreadExceptionEventArgs Ex)
        {
            try
            {
                File.AppendAllText(@"SCME.Service error.txt",
                    String.Format("\n\n{0}\nEXCEPTION: {1}\nINNER EXCEPTION: {2}\n", DateTime.Now, Ex.Exception,
                        Ex.Exception.InnerException ??
                        new Exception("No additional information - InnerException is null")));
            }
            catch
            {
            }
        }

        private static void OnExitCommand(object Sender, EventArgs E)
        {
            Application.Exit();
        }

        private static void ApplicationOnExit(object Sender, EventArgs Args)
        {
            ms_TrayIcon.Visible = false;
            SystemHost.Close();
        }
    }
}