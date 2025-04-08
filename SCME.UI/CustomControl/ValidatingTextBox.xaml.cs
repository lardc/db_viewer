using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace SCME.UI.CustomControl
{
    /// <summary>
    ///     Interaction logic for CTextBox.xaml
    /// </summary>
    public partial class ValidatingTextBox 
    {
        private string m_OldText = string.Empty;

        public bool IsNumeric { get; set; }
        public float Maximum { get; set; }
        public float Minimum { get; set; }
        public bool IsFloat { get; set; }
       
       

        public ValidatingTextBox()
        {
            Minimum = 0;
            Maximum = 100;
            IsNumeric = false;
            IsFloat = false;

            InitializeComponent();

            toolTip.PlacementTarget = this;
            toolTip.Placement = PlacementMode.Bottom;
            toolTip.Visibility = Visibility.Collapsed;
        }

        public void ShowTip(string Message)
        {
            toolTip.Content = Message;

            if (!toolTip.IsOpen)
            {
                toolTip.Visibility = Visibility.Visible;
                toolTip.IsOpen = true;
            }
        }

        public void HideTip()
        {
            if (toolTip.IsOpen)
            {
                toolTip.Visibility = Visibility.Collapsed;
                toolTip.IsOpen = false;
            }
        }

        private void TextBox_GotFocus(object Sender, RoutedEventArgs E)
        {
            m_OldText = Text;
            SelectAll();
        }

        private void TextBox_LostFocus(object Sender, RoutedEventArgs E)
        {
            if (!IsNumeric)
                return;

            double floatValue;
            bool badEntry;

            if (String.IsNullOrWhiteSpace(Text))
                Text = m_OldText;

            if (IsFloat)
            {
                badEntry =
                    !double.TryParse(Text, NumberStyles.Number | NumberStyles.AllowDecimalPoint,
                                     CultureInfo.InvariantCulture, out floatValue);
            }
            else
            {
                int value;

                badEntry =
                    !int.TryParse(Text, NumberStyles.Number | NumberStyles.AllowDecimalPoint,
                                     CultureInfo.InvariantCulture, out value);
                floatValue = value;
            }

            if (!badEntry)
            {
                if (floatValue > Maximum)
                {
                    Text = Maximum.ToString(CultureInfo.InvariantCulture);
                    Select(Text.Length, 0);
                }
                else if (floatValue < Minimum)
                {
                    Text = Minimum.ToString(CultureInfo.InvariantCulture);
                    Select(Text.Length, 0);
                }
                else
                {
                    Text = floatValue.ToString(CultureInfo.InvariantCulture);
                    Select(Text.Length, 0);
                }
            }
            else
                Text = m_OldText;
            
        }

        private void ValidatingTextBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var main = Application.Current.MainWindow as MainWindow;
            if (main != null) main.ShowKeyboard(true,this);
        }
    }
}