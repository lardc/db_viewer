using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SCME.CustomControls
{
    /// <summary>
    /// Interaction logic for DatePickerCustom.xaml
    /// </summary>
    public partial class DatePickerCustom : DatePicker
    {
        public DatePickerCustom() : base()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            //если этого не сделать - перехватить нажатие клавиши Enter на DatePicker не получится, стандартный DatePicker полностью закрывает в себе обработку нажатия клавиши Enter
            base.OnApplyTemplate();

            var datePickerTextBox = GetTemplateChild("PART_TextBox") as System.Windows.Controls.Primitives.DatePickerTextBox;
            datePickerTextBox.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(OnTextBoxKeyDown), true);
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                e.Handled = false;
        }

        public bool CheckDate()
        {
            //проверяем введённую пользователем дату
            //true - проверка пройдена
            bool result = DateTime.TryParse(this.SelectedDate.ToString(), out DateTime dt);

            if (result)
                result = (dt >= DateTime.Parse("01.01.1900"));

            return result;
        }
    }
}
