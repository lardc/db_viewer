using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SCME.Types;
using SCME.Types.Clamping;

namespace SCME.UI.PagesUser
{
    /// <summary>
    /// Interaction logic for MeasureDialog.xaml
    /// </summary>
    public partial class MeasureDialog : Window
    {
        private readonly TestParameters _paramsClamp;

        public MeasureDialog(TestParameters paramsClamp)
        {
            
            InitializeComponent();
            _paramsClamp = paramsClamp;
           
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            MainWindow win = (MainWindow)Application.Current.MainWindow;
            win.ShowKeyboard(false, null);
        }


        private void BtnCorrect_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Cache.Net.Unsqueeze(_paramsClamp);
        }
    }
}
