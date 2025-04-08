using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WaitVisualizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string Arguments { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.Arguments = string.Empty;

            foreach (string argument in e.Args)
                this.Arguments = string.Concat(argument);
        }
    }
}
