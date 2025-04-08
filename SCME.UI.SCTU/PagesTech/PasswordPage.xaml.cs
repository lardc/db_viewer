using System.Windows;
using SCME.UI.Properties;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for PasswordPage.xaml
    /// </summary>
    public partial class PasswordPage
    {
        public PasswordPage()
        {
            InitializeComponent();
        }

        private void BtnOk_Clicked(object Sender, RoutedEventArgs E)
        {
            if (tbPassword.Text == Settings.Default.TechPassword && NavigationService != null)
            {
                lblIncorrect.Content = "";
                NavigationService.Navigate(Cache.Technician);
            }
            else
                lblIncorrect.Content = Properties.Resources.PasswordIncorrect;

            tbPassword.Text = "";
        }

        private void BtnCancel_OnClick(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
            {
                lblIncorrect.Content = "";
                NavigationService.GoBack();
            }
        }
    }
}