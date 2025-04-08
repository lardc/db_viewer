using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SCME.Linker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Activated(object sender, EventArgs e)
        {
            //если форма DialogInputString была открыта, после чего пользователь щёлкнул по другому приложению в taskbar, а затем перешёл на данное приложение то форма DialogInputString будет не видна. для предотвращения этого ищем данную форму и вызываем Activate()
            foreach (Window w in Application.Current.Windows)
            {
                if (w is DialogInputString)
                    w.Activate();
            }
        }

    }
}
