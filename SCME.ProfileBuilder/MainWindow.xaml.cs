using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using SCME.ProfileBuilder.Properties;

namespace SCME.ProfileBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {           
            InitializeComponent();           

            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.Default.CurrentCulture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.CurrentCulture);
            LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            Cache.Main = this;
            Cache.ConnectPage = new ConnectPage();
            mainFrame.Navigate(Cache.ConnectPage);
        }
    }
}
