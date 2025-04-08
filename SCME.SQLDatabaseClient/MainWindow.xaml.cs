using System.Windows;

namespace SCME.SQLDatabaseClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Cache.Main = this;
            Cache.ViewData = new ViewDataPage();
            Cache.Welcome = new WelcomePage();

            mainFrame.Navigate(Cache.Welcome);
        }
    }
}
