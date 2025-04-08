using System;
using System.Windows;
using System.Windows.Threading;
using SCME.UI.CustomControl;

namespace SCME.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs E)
        {
            base.OnStartup(E);

            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private static void Current_DispatcherUnhandledException(object Sender, DispatcherUnhandledExceptionEventArgs E)
        {
            var dw = new DialogWindow("Application crashed!",
                                      string.Format("{0}\n{1}", E.Exception.Message, E.Exception))
                {
                    Top = SystemParameters.WorkArea.Top,
                    Left = SystemParameters.WorkArea.Left,
                    Width = SystemParameters.WorkArea.Width,
                    Height = SystemParameters.WorkArea.Height,
                };

            dw.ButtonConfig(DialogWindow.EbConfig.OK);
            dw.ShowDialog();
        }
    }
}