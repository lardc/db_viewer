using SCME.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for ManualInputParamValueEditor.xaml
    /// </summary>
    public partial class ManualInputParamValueEditor : Window
    {
        public ManualInputParamValueEditor()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        public bool? GetValue(ref double value)
        {
            //возвращает:
            // True - пользователь нажал кнопку OK;
            // False - пользователь закрыл форму, т.е. отказался от редактирования
            tbManualInputDevParamValue.Text = value.ToString();
            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                //пользователь хочет сохранить введённое значение параметра
                if (SCME.Common.Routines.TryStringToDouble(tbManualInputDevParamValue.Text.Trim(), out double settedValue))
                    value = settedValue;
            }

            return result;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.DialogResult = false;
        }

        /*
        private void FloatValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = (Routines.SystemDecimalSeparator() == ',') ? new Regex("[^0-9,-]+") : new Regex("[^0-9.-]+");

            e.Handled = regex.IsMatch(e.Text);
        }
        */

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //на пробел данное событие не реагирует - поэтому пользователь может ввести пробелы в значения параметров
            if (sender is TextBox tb)
            {
                string allEntered = tb.Text.Insert(tb.CaretIndex, e.Text);

                e.Handled = !Common.Routines.IsDouble(allEntered);
            }
        }

        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
