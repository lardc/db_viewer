using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SCME.Linker
{
    /// <summary>
    /// Interaction logic for DialogInputString.xaml
    /// </summary>
    public partial class DialogInputString : Window
    {
        public DialogInputString()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        public delegate bool CheckValue(string value);
        private CheckValue FCheckValue { get; set; }

        public bool ShowModal(CheckValue checkValue, string editedTittle, string editedValue, out string stringValue)
        {
            this.FCheckValue = checkValue;

            lbTittleValue.Content = editedTittle;
            this.tbStringValue.Text = editedValue;

            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                stringValue = this.tbStringValue.Text.Trim();

                return true;
            }
            else
            {
                stringValue = null;

                return false;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    this.btCancel.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;

                case Key.Enter:
                    e.Handled = true;
                    this.btOK.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    break;
            }
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            string stringValue = this.tbStringValue.Text.Trim();

            if (this.FCheckValue != null)
            {
                if (this.FCheckValue(stringValue))
                {
                    this.DialogResult = true;
                }
                else
                    System.Windows.Forms.MessageBox.Show(string.Concat(Properties.Resources.GroupNameIsNotValid, "."), Properties.Resources.OperationError, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }
}
