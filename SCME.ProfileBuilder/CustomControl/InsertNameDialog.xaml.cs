using System;
using System.Windows;
using System.Windows.Media;
using SCME.ProfileBuilder.PagesTech;
using Color = System.Drawing.Color;

namespace SCME.ProfileBuilder.CustomControl
{
    /// <summary>
    ///     Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class InsertNameDialog
    {
        public InsertNameDialog(string Title, string Message)
        {
            InitializeComponent();
            lblTitle.Content = Title;
            tbMessage.Text = Message;
        }

        private void BtnOk_Click(object Sender, RoutedEventArgs E)
        {
            if (Connections.CheckForExistingName(Cache.ConnectionsPage.MmeCodes, tbMessage.Text))
            {
                lblTitle.Content = "Такое название уже существует!";
                return;
            }

            DialogResult = true;
        }
    }
}