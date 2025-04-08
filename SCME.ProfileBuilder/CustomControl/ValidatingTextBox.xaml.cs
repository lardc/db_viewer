using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SCME.ProfileBuilder.CustomControl
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
        private CultureInfo ci;

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

            //в качестве разделителя дробной части от целой всегда будем использовать запятую
            ci = CultureInfo.InvariantCulture.Clone() as CultureInfo;
            ci.NumberFormat.NumberDecimalSeparator = ",";
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
                                     ci, out floatValue);
            }
            else
            {
                int value;

                badEntry =
                    !int.TryParse(Text, NumberStyles.Number | NumberStyles.AllowDecimalPoint,
                                     ci, out value);
                floatValue = value;
            }

            if (!badEntry)
            {
                if (floatValue > Maximum)
                {
                    Text = Maximum.ToString(ci);
                    Select(Text.Length, 0);
                }
                else if (floatValue < Minimum)
                {
                    Text = Minimum.ToString(ci);
                    Select(Text.Length, 0);
                }
                else
                {
                    Text = floatValue.ToString(ci);
                    Select(Text.Length, 0);
                }
            }
            else
                Text = m_OldText;
        }       
    }
}