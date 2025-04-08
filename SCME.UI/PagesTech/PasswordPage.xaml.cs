using System.Windows;
using System.Windows.Navigation;
using SCME.UI.Properties;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for PasswordPage.xaml
    /// </summary>
    public partial class PasswordPage
    {
        public delegate void delegateAfterOkRoutine(NavigationService NavigationService);
        public delegateAfterOkRoutine AfterOkRoutine;

        public PasswordPage()
        {
            InitializeComponent();
            AfterOkRoutine = null;
        }

        private void BtnOk_Clicked(object Sender, RoutedEventArgs E)
        {
            if (tbPassword.Text == Settings.Default.TechPassword && NavigationService != null)
            {
                lblIncorrect.Content = "";

                if (AfterOkRoutine != null)
                {
                    AfterOkRoutine(NavigationService);
                    AfterOkRoutine = null;
                }
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