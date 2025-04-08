using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SCME.Agent.Properties;

namespace SCME.Agent
{
    internal class Supervisor
    {
        private readonly NotifyIcon m_TrayIcon;
        private readonly Process m_PService, m_PUserInterface, m_PProxy;

        internal Supervisor()
        {
            var ico = Resources.TrayIconPE;
            m_TrayIcon = new NotifyIcon
                {
                    Text = @"SCME.Agent",
                    Icon = new Icon(ico, ico.Width, ico.Height),
                    ContextMenu = new ContextMenu(new[] {new MenuItem(@"Exit", OnExit)}),
                    Visible = true
                };

            m_PProxy = new Process
                {
                    StartInfo =
                        {
                            FileName = Settings.Default.ProxyAppPath,
                            WorkingDirectory =
                                Path.GetDirectoryName(Settings.Default.ProxyAppPath) ??
                                Environment.CurrentDirectory,
                            ErrorDialog = false
                        },
                    EnableRaisingEvents = true
                };
            m_PProxy.Exited += PExited;

            m_PService = new Process
                {
                    StartInfo =
                        {
                            FileName = Settings.Default.ServiceAppPath,
                            WorkingDirectory =
                                Path.GetDirectoryName(Settings.Default.ServiceAppPath) ??
                                Environment.CurrentDirectory,
                            ErrorDialog = false
                        },
                    EnableRaisingEvents = true
                };
            m_PService.Exited += PExited;

            if (Settings.Default.IsUserInterfaceEnabled)
            {
                m_PUserInterface = new Process
                {
                    StartInfo =
                    {
                        FileName = Settings.Default.UIAppPath,
                        WorkingDirectory =
                            (Path.GetDirectoryName(Settings.Default.UIAppPath)) ??
                            Environment.CurrentDirectory,
                        ErrorDialog = false
                    },
                    EnableRaisingEvents = true
                };

                m_PUserInterface.Exited += PExited;
            }

            Application.ApplicationExit += Application_ApplicationExit;
        }

        internal void Start()
        {
            if (Settings.Default.IsProxyEnabled)
            {
                StartProcess(m_PProxy);
                Thread.Sleep(Settings.Default.ProxyInitDelayMs);
            }
            
            StartProcess(m_PService);
            
            if (Settings.Default.IsUserInterfaceEnabled)
                StartProcess(m_PUserInterface);
        }

        private static void PExited(object Sender, EventArgs E)
        {
            var p = Sender as Process;
            
            StartProcess(p);
        }

        private void OnExit(object Sender, EventArgs E)
        {
            m_TrayIcon.Visible = false;
            Application.Exit();
        }

        private void Application_ApplicationExit(object Sender, EventArgs E)
        {
            m_TrayIcon.Visible = false;
        }

        private static void StartProcess(Process P)
        {
            try
            {
                var pname = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(P.StartInfo.FileName));

                if (pname.Length == 0)
                    P.Start();
            }
            catch (Exception ex)
            {
                var str = string.Format(Resources.Log_Message_Process_error, P.StartInfo.FileName, ex.Message);

                MessageBox.Show(str, Resources.Error_Caption_Supervisor_error, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                Environment.Exit(1);
            }
        }
    }
}