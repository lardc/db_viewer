using SCME.Types;
using SCME.Types.Profiles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for ManualInputParams.xaml
    /// </summary>
    public partial class ManualInputParams : Window, INotifyPropertyChanged
    {
        public ManualInputParams(int? profID, List<TemperatureCondition> listTemperatureCondition)
        {
            //входной параметр profID нужен только для работы (чтения и редактирования) с нормами на вручную созданные параметры
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
            this.FProfID = profID;
            this.FListTemperatureCondition = listTemperatureCondition;

            mnuCreate.Visibility = Common.Routines.IsUserCanCreateParameter(((MainWindow)this.Owner).PermissionsLo) ? Visibility.Visible : Visibility.Collapsed;
            mnuEdit.Visibility = Common.Routines.IsUserCanEditParameter(((MainWindow)this.Owner).PermissionsLo) ? Visibility.Visible : Visibility.Collapsed;
            mnuDelete.Visibility = Common.Routines.IsUserCanDeleteParameter(((MainWindow)this.Owner).PermissionsLo) ? Visibility.Visible : Visibility.Collapsed;
        }

        DataTable FDataTable = new DataTable();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public DataView ManualInputParamsList
        {
            get
            {
                DbRoutines.GetManualInputParams(this.FDataTable, this.FProfID, this.FListTemperatureCondition);

                this.FDataTable.DefaultView.Sort = "TEMPERATURECONDITION, NAME";

                return this.FDataTable.DefaultView;
            }
        }

        private int? FProfID;
        private List<TemperatureCondition> FListTemperatureCondition = null;

        //для облегчения процесса создания нужных пользователю параметров создаём механизм обратного вызова
        public delegate void LoadProfileParameters(List<string> profileParameters);
        public LoadProfileParameters LoadProfileParametersHandler { get; set; }

        public bool? GetManualParameterID(out string temperatureCondition, out int manualInputParamID, out string manualInputParamName)
        {
            //демонстрация списка параметров, которые пользователи создали вручную            
            //возвращает:
            //           True - в out параметре manualInputParamID идентификатор выбранного параметра;
            //           False - в out параметре manualInputParamID=-1.
            bool? result = this.ShowDialog();

            //по умолчанию параметр не выбран
            temperatureCondition = null;
            manualInputParamID = -1;
            manualInputParamName = null;

            if (result ?? false)
            {
                //пользователь выбирает параметр из списка
                //если параметр в списке единственный - нет других вариантов выбора, он и должен быть выбран
                if (this.dgManualInputParams.SelectedRow() == null)
                    if (this.dgManualInputParams.Items.Count != 0)
                        this.dgManualInputParams.SelectedItem = this.dgManualInputParams.Items[0];

                if (this.dgManualInputParams.SelectedRow() == null)
                {
                    //список выбора пуст, выбирать нечего - ругаемся
                    MessageBox.Show(Properties.Resources.NothingHasBeenSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    temperatureCondition = this.dgManualInputParams.ValueFromSelectedRow("TEMPERATURECONDITION").ToString();
                    manualInputParamID = Convert.ToInt32(this.dgManualInputParams.ValueFromSelectedRow("MANUALINPUTPARAMID"));
                    manualInputParamName = this.dgManualInputParams.ValueFromSelectedRow("NAME").ToString();
                }
            }

            return result;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.DialogResult = false;
        }

        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void dgManualInputParams_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = true;
        }

        private IEnumerable<string> ActualProfileParameters()
        {
            //возвращает список параметров профиля, которых нет в справочнике параметров для ручного вввода
            //грузим список параметров в manualInputParamEditor
            if (this.LoadProfileParametersHandler == null)
            {
                return null;
            }
            else
            {
                List<string> profileParameters = new List<string>();
                this.LoadProfileParametersHandler.Invoke(profileParameters);

                //строим список имён параметров существующих в справочнике параметров для ручного ввода
                List<string> listManualParameters = (from row in this.FDataTable.AsEnumerable()
                                                     select row["NAME"].ToString()
                                                    ).ToList();

                //параметры, которые уже есть в справочнике исключаем из списка profileParameters - дубли нам не нужны
                return profileParameters.Except(listManualParameters);
            }
        }

        private void mnuCreateClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanCreateParameter(((MainWindow)this.Owner).PermissionsLo))
            {
                List<string> actualProfileParameters = this.ActualProfileParameters()?.ToList<string>();

                ManualInputParamEditor manualInputParamEditor = new ManualInputParamEditor();

                if (manualInputParamEditor.ShowModal(null, this.FProfID, string.Empty, TemperatureCondition.RT, string.Empty, string.Empty, string.Empty, null, null, actualProfileParameters) ?? false)
                {
                    //пользователь выполнил сохранение параметра
                    OnPropertyChanged();
                }
            }
        }

        private int SelectedManualInputParamID()
        {
            //считывает из выбранной пользователем записи идентификатор параметра
            //возвращает:
            //-1 - пользователь не выбрал запись в dgManualInputParams
            //иначе - идентификатор параметра
            int manualInputParamID = 0;

            //если ячейка в dgManualInputParams выбрана - считываем идентификатор параметра
            if (this.dgManualInputParams.CurrentCell.IsValid)
            {
                manualInputParamID = Convert.ToInt32(this.dgManualInputParams.ValueFromSelectedRow("MANUALINPUTPARAMID"));
            }

            return (manualInputParamID == 0) ? -1 : manualInputParamID;
        }

        private void mnuEditClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanEditParameter(((MainWindow)this.Owner).PermissionsLo))
            {
                int manualInputParamID = this.SelectedManualInputParamID();

                if (manualInputParamID == -1)
                {
                    MessageBox.Show(Properties.Resources.NoEditingObjectSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                string name = this.dgManualInputParams.ValueFromSelectedRow("NAME").ToString();
                TemperatureCondition temperatureCondition = (TemperatureCondition)Enum.Parse(typeof(TemperatureCondition), this.dgManualInputParams.ValueFromSelectedRow("TEMPERATURECONDITION").ToString());
                string um = this.dgManualInputParams.ValueFromSelectedRow("UM").ToString();
                string descrEN = this.dgManualInputParams.ValueFromSelectedRow("DESCREN").ToString();
                string descrRU = this.dgManualInputParams.ValueFromSelectedRow("DESCRRU").ToString();

                double? normMin = double.TryParse(this.dgManualInputParams.ValueFromSelectedRow("MINVAL").ToString(), out double dNormMin) ? (double?)dNormMin : null;
                double? normMax = double.TryParse(this.dgManualInputParams.ValueFromSelectedRow("MAXVAL").ToString(), out double dNormMax) ? (double?)dNormMax : null;

                ManualInputParamEditor manualInputParamEditor = new ManualInputParamEditor();

                //грузим список параметров в manualInputParamEditor
                List<string> actualProfileParameters = this.ActualProfileParameters().ToList<string>();

                if (manualInputParamEditor.ShowModal(manualInputParamID, this.FProfID, name, temperatureCondition, um, descrEN, descrRU, normMin, normMax, actualProfileParameters) ?? false)
                {
                    //пользователь выполнил сохранение параметра
                    OnPropertyChanged();
                }
            }
        }

        private void mnuDeleteClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanDeleteParameter(((MainWindow)this.Owner).PermissionsLo))
            {
                int manualInputParamID = this.SelectedManualInputParamID();

                if (manualInputParamID == -1)
                {
                    MessageBox.Show(Properties.Resources.ObjectForDeleteNotSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                string name = this.dgManualInputParams.ValueFromSelectedRow("NAME").ToString();
                TemperatureCondition temperatureCondition = (TemperatureCondition)Enum.Parse(typeof(TemperatureCondition), this.dgManualInputParams.ValueFromSelectedRow("TEMPERATURECONDITION").ToString());
                string fullName = string.Concat(name, " (", temperatureCondition.ToString(), ")");

                if (MessageBox.Show(string.Format(Properties.Resources.ConfirmationMessForDeleteManualInputParam, fullName), Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    DbRoutines.DeleteAllByManualInputParamID(manualInputParamID);

                    //удаление параметра выполнено
                    OnPropertyChanged();
                }
            }
        }

        private void DgManualInputParams_Loaded(object sender, RoutedEventArgs e)
        {
            if ((sender is DataGrid dg) && (this.dgManualInputParams.Items.Count > 0))
                this.dgManualInputParams.SelectedIndex = 0;
        }
    }
}
