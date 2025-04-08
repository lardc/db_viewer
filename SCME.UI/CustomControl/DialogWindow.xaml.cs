using System.Windows;

namespace SCME.UI.CustomControl
{
    /// <summary>
    ///     Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow
    {
        public enum EbConfig
        {
            OK,
            Cancel,
            OKCancel
        }

        public DialogWindow(string Title, string Message)
        {
            InitializeComponent();

            lblTitle.Content = Title;
            tbMessage.Text = Message;
        }

        public void ButtonConfig(EbConfig Conf)
        {
            switch (Conf)
            {
                case EbConfig.OKCancel:
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(60, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(60, GridUnitType.Star);
                    break;
                case EbConfig.OK:
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(60, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                    break;
                case EbConfig.Cancel:
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[1].Width = new GridLength(60, GridUnitType.Star);
                    break;
            }
        }

        private void BtnOk_Click(object Sender, RoutedEventArgs E)
        {
            DialogResult = true;
        }
    }
}