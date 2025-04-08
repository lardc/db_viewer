using SCME.Types;
using SCME.Types.Profiles;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for ManualInputParamEditor.xaml
    /// </summary>
    public partial class ManualInputParamEditor : Window
    {
        public ManualInputParamEditor()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        private bool CheckData()
        {
            //выполняет проверку введённых пользователем данных с точки зрения возможности выполнения сохранения в базу данных
            if ((tbName.Text == null) || (tbName.Text.Trim() == string.Empty))
            {
                MessageBox.Show(string.Concat(Properties.Resources.ParameterNameNotDefined, " ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if ((tbUm.Text == null) || (tbUm.Text.Trim() == string.Empty))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UnitMeasureIsNotDefined, " ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(tbNormMin.Text) && (!Common.Routines.TryStringToDouble(tbNormMin.Text, out double dNormMin)))
            {
                MessageBox.Show(string.Concat(Properties.Resources.NormMinValue, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(tbNormMax.Text) && (!Common.Routines.TryStringToDouble(tbNormMax.Text, out double dNormMax)))
            {
                MessageBox.Show(string.Concat(Properties.Resources.NormMaxValue, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        public bool? ShowModal(int? manualInputParamID, int? profileID, string name, TemperatureCondition temperatureCondition, string um, string descrEN, string descrRU, double? normMin, double? normMax, IEnumerable<string> profileParameters)
        {
            this.ShowProfileParameters(profileParameters);

            return this.ShowModal(manualInputParamID, profileID, name, temperatureCondition, um, descrEN, descrRU, normMin, normMax);
        }

        private bool? ShowModal(int? manualInputParamID, int? profileID, string name, TemperatureCondition temperatureCondition, string um, string descrEN, string descrRU, double? normMin, double? normMax)
        {
            //данная реализация принимает на вход идентификатор manualInputParamID и реквизиты параметра, отображает их значения в данной форме
            //возвращает True - реквизиты принятого параметра были обновлены пользователем - пользователь нажал кнопку OK. на момент получения такого результата в базу данных уже было выполнено сохранение этих изменений;
            //возвращает False - пользователь закрыл форму, т.е. отказался от редактирования реквизитов параметра
            tbName.Text = name;
            cmbTemperatureCondition.Text = temperatureCondition.ToString();
            tbUm.Text = um;
            tbDescrEN.Text = descrEN;
            tbDescrRU.Text = descrRU;
            tbNormMin.Text = normMin.ToString();
            tbNormMax.Text = normMax.ToString();

            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                //пользователь хочет сохранить сделанные изменения
                if (this.CheckData())
                {
                    string editedName = tbName.Text;
                    TemperatureCondition editedTemperatureCondition = (TemperatureCondition)Enum.Parse(typeof(TemperatureCondition), cmbTemperatureCondition.Text.ToString());
                    string editedUm = tbUm.Text;
                    string editedDescrEN = tbDescrEN.Text;
                    string editedDescrRU = tbDescrRU.Text;

                    int iManualInputParamID = DbRoutines.SaveToManualInputParams(manualInputParamID, editedName, editedTemperatureCondition, editedUm, editedDescrEN, editedDescrRU);

                    double? editedNormMin = (!string.IsNullOrEmpty(tbNormMin.Text) && Common.Routines.TryStringToDouble(tbNormMin.Text, out double dEditedNormMin)) ? (double?)dEditedNormMin : null;
                    double? editedNormMax = (!string.IsNullOrEmpty(tbNormMax.Text) && Common.Routines.TryStringToDouble(tbNormMax.Text, out double dEditedNormMax)) ? (double?)dEditedNormMax : null;

                    if (profileID != null)
                        DbRoutines.SaveToManualInputParamNorms(iManualInputParamID, (int)profileID, editedNormMin, editedNormMax);
                }
            }

            return result;
        }

        private void ButtonProfileParameter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                tbName.Text = button.Content.ToString();
        }

        public void ShowProfileParameters(IEnumerable<string> profileParameters)
        {
            //строит в spProfileParameters столько кнопок, сколько элементов в принятом profileParameters, т.е. каждая кнопка есть параметр из принятого списка profileParameters
            if (profileParameters == null)
            {
                this.svProfileParameters.Visibility = Visibility.Hidden;
            }
            else
            {
                this.svProfileParameters.Visibility = Visibility.Visible;

                foreach (string parameter in profileParameters)
                {
                    Button button = new Button()
                    {
                        Content = parameter,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    button.Click += new RoutedEventHandler(ButtonProfileParameter_Click);
                    this.spProfileParameters.Children.Add(button);
                }
            }
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckData())
                this.DialogResult = true;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.DialogResult = false;
        }
    }

    public static class FilteredTemperatureConditions
    {
        //значение TemperatureCondition.None пользователю не нужно, избавляемся от него
        public static IEnumerable<TemperatureCondition> TemperatureConditions()
        {
            yield return TemperatureCondition.RT;
            yield return TemperatureCondition.TM;
        }
    }
}
