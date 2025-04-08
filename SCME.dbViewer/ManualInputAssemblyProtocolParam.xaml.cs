using SCME.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static SCME.dbViewer.ManualInputDevices;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for ManualInputAssemblyProtocolParam.xaml
    /// </summary>
    public partial class ManualInputAssemblyProtocolParam : Window, INotifyPropertyChanged
    {
        int FProfID = -1;
        DataTable FDataSource = new DataTable();
        //const string AssemblyProtocolID = "ASSEMBLYPROTOCOLID";

        public DataView DataSource
        {
            get
            {
                return this.FDataSource.DefaultView;
            }

            set { this.FDataSource = value.Table; }
        }


        public ManualInputAssemblyProtocolParam(int profID)
        {
            InitializeComponent();

            this.FProfID = profID;
            this.DataSource.Table.Columns.Add(Common.Constants.AssemblyProtocolID);
            this.DataSource.Table.Columns.Add(Common.Constants.Descr);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LoadData(int asemblyProtocolID)
        {
            bool canReloadList;

            switch (this.FDataSource.Rows.Count)
            {
                case 0:
                    canReloadList = true;
                    break;

                default:
                    //отображаемый список не пуст - спрашиваем у пользователя разрешение на его разрушение и чтение его из базы данных
                    canReloadList = (MessageBox.Show(Properties.Resources.CanReloadList, Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes);
                    break;
            }

            if (canReloadList)
            {
                List<Types.DbRoutines.ColumnBindingDescr> columnBindingList = Types.DbRoutines.LoadManualInputAssemblyProtocolParam(this.FDataSource, asemblyProtocolID);

                if ((columnBindingList != null) && (this.FDataSource.Rows.Count != 0))
                {
                    //проходим по полученным данным с целью построить столбцы в DataGrid для их отображения
                    foreach (Types.DbRoutines.ColumnBindingDescr bindingDescr in columnBindingList)
                    {
                        this.CreateColumnInDataGrid(bindingDescr.Header, bindingDescr.BindPath);
                    }
                }
            }
        }

        public bool? ShowModal(int assemblyProtocolDescr)
        {
            //демонстрация формы создания, редатирования удаления (вручную) параметров протокола сборки с обозначением assemblyProtocolDescr
            if (assemblyProtocolDescr != 1)
            {
                int asemblyProtocolID = Types.DbRoutines.AssemblyProtocolIDByDescr(assemblyProtocolDescr);

                this.LoadData(asemblyProtocolID);
            }

            bool? result = this.ShowDialog();

            return result;
        }

        private bool IsDataGridHaveError()
        {
            //в dgAssemblyProtocolParam установлено ограничение на уникальность имени используемого параметра протокола испытаний (нулевой столбец)
            //данная реализация возвращает:
            // true - dgAssemblyProtocolParam имеет ошибку(и) во введённых данных
            // false - dgAssemblyProtocolParam не выявил ошибок во введённых данных

            bool errors = (from c in
                               from object i in dgAssemblyProtocolParam.ItemsSource
                               select dgAssemblyProtocolParam.ItemContainerGenerator.ContainerFromItem(i)
                           where c != null
                           select Validation.GetHasError(c)).FirstOrDefault(x => x);

            return errors;
        }

        private void DataGridCell_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //при обычном поведении DataGrid нажатие на клавишу Enter приводит к переходу курсора на следующую строку в текущем столбце
            //технологи хотят чтобы при нажатии на Enter курсор двигался в следующий справа столбец и только из последнего столбца курсор должен стать на новую запись в первом столбце
            if ((sender is DataGridCell cell) && (e.OriginalSource is UIElement uiElement))
            {
                if (e.Key == Key.Enter)
                {
                    if (!this.IsDataGridHaveError())
                    {
                        if (cell.Content is TextBox textBox)
                        {
                            string text = textBox.Text.Trim();

                            if (string.IsNullOrEmpty(text))
                            {
                                e.Handled = true;
                                return;
                            }
                        }

                        if (dgAssemblyProtocolParam.Columns.IndexOf(cell.Column) == dgAssemblyProtocolParam.Columns.Count - 1)
                        {
                            //выбрана ячейка в последнем столбце
                            //делаем текущей ячейкой самую первую ячейку текущей строки и дальше полагаемся на стандартную обработку DataGrid клавиши 'Enter'
                            DataGridCellInfo firstCell = new DataGridCellInfo(dgAssemblyProtocolParam.Items[dgAssemblyProtocolParam.Items.Count - 1], dgAssemblyProtocolParam.Columns[0]);
                            dgAssemblyProtocolParam.CurrentCell = firstCell;
                        }
                        else
                        {
                            e.Handled = true;
                            uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        }
                    }
                }
            }
        }

        private void DataGridCell_GotFocus(object sender, RoutedEventArgs e)
        {
            //при получении фокуса ввода ячейкой сразу переводим её в режим редактирования
            //если этого не делать - у пользователя будет возможность ввести в ячейку значение, которое будет проигнорировано обработчиком NumericColumn_OnPreviewTextInput
            if (e.OriginalSource is DataGridCell cell)
            {
                if (dgAssemblyProtocolParam.Columns.IndexOf(cell.Column) != 0)
                {
                    e.Handled = true;
                    dgAssemblyProtocolParam.BeginEdit();
                }
            }
        }

        private void BtDeleteParam(object sender, RoutedEventArgs e)
        {
            //удаление параметра (столбца в котором стоит курсор)
            //то есть удаляется множество мест хранения значений вручную введённого параметра (одного и того же)
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(((MainWindow)this.Owner).PermissionsLo))
            {
                //вычисляем идентификатор вручную введённого параметра
                if (dgAssemblyProtocolParam.SelectedCells[0].Column is DataGridNumericColumn column)
                {
                    Binding b = (Binding)column.Binding;

                    if (int.TryParse(b.Path.Path, out int manualInputParamID))
                    {
                        foreach (DataRow row in this.FDataSource.Rows)
                        {
                            object objAssemblyProtocolID = row[Common.Constants.AssemblyProtocolID];

                            if (objAssemblyProtocolID != DBNull.Value)
                            {
                                if (int.TryParse(objAssemblyProtocolID.ToString(), out int assemblyProtocolID))
                                    DbRoutines.DeleteFromManualInputAssemblyProtocolParam(assemblyProtocolID, manualInputParamID);
                            }
                        }

                        //удаляем столбец
                        //перегружать отображаемый список чтобы пользователь увидел результат не хорошо, т.к. в нём могут быть не сохранённые данные
                        dgAssemblyProtocolParam.Columns.Remove(column);
                        this.DataSource.Table.Columns.Remove(manualInputParamID.ToString());
                    }
                }
            }
        }

        private string BindPathByManualInputParamIDValue(int manualInputParamID)
        {
            return manualInputParamID.ToString();
        }

        private DataGridNumericColumn CreateColumnInDataGrid(string header, string bindPath)
        {
            DataGridNumericColumn column = new DataGridNumericColumn
            {
                Header = header,
                Binding = new Binding(bindPath)
            };

            this.dgAssemblyProtocolParam.Columns.Add(column);

            return column;
        }

        private DataGridNumericColumn CreateColumn(string header, string bindPath)
        {
            DataColumn tableColumn = this.DataSource.Table.Columns.Add(bindPath, typeof(double));
            tableColumn.Unique = false;
            tableColumn.AllowDBNull = true;
            tableColumn.AutoIncrement = false;
            tableColumn.DefaultValue = null;

            DataGridNumericColumn column = this.CreateColumnInDataGrid(header, bindPath);

            return column;
        }

        private void BtNewParam(object sender, RoutedEventArgs e)
        {
            //создание новой записи (нового места хранения значения параметра) в dgAssemblyProtocolParam
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(((MainWindow)this.Owner).PermissionsLo))
            {
                if (this.FProfID != -1)
                {
                    if (this.IsDataGridHaveError())
                    {
                        MessageBox.Show(Properties.Resources.DataContainsAnError, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                        return;
                    }

                    //показываем пользователю содержимое справочника параметров, в котором он может создавать и выбирать созданный параметр
                    //времянка исправить
                    Types.Profiles.TemperatureCondition tc = Types.Profiles.TemperatureCondition.None;
                    List<Types.Profiles.TemperatureCondition> listTemperatureCondition = new List<Types.Profiles.TemperatureCondition>()
                {
                    tc
                };

                    ManualInputParams manualInputParams = new ManualInputParams(this.FProfID, listTemperatureCondition)
                    {
                        //не показваем пользователю подсказку с параметрами профиля для выбора параметра, ибо профиль для протокола сборки не может быть однозначно определён
                        LoadProfileParametersHandler = null
                    };

                    if (manualInputParams.GetManualParameterID(out string temperatureCondition, out int manualInputParamID, out string manualInputParamName) ?? false)
                    {
                        //проверяем уникальность имени создаваемого столбца
                        if (this.dgAssemblyProtocolParam.Columns.FirstOrDefault(c => c.Header.ToString() == manualInputParamName) == null)
                        {
                            string bindPath = this.BindPathByManualInputParamIDValue(manualInputParamID);
                            this.CreateColumn(manualInputParamName, bindPath);
                        }
                        else
                            MessageBox.Show(string.Format(Properties.Resources.ParemeterIsInUse, manualInputParamName), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
        }

        private void DgAssemblyProtocolParam_GotMouseCapture(object sender, MouseEventArgs e)
        {

        }

        private void BtSave_Click(object sender, RoutedEventArgs e)
        {
            //если что-то не было переписано в dgAssemblyProtocolParam.ItemsSource - сделаем это
            dgAssemblyProtocolParam.CommitEdit();

            //выполняем сохранение введённых значений параметров
            foreach (DataRow row in this.FDataSource.Rows)
            {
                //извлекаем из row идентификатор протокола сборки
                int columnIndex = row.Table.Columns.IndexOf(Common.Constants.AssemblyProtocolID);

                if (columnIndex != -1)
                {
                    object objAssemblyProtocolID = row[columnIndex];

                    if (objAssemblyProtocolID != DBNull.Value)
                    {
                        if (int.TryParse(objAssemblyProtocolID.ToString(), out int assemblyProtocolID))
                        {
                            //протокол всегда создан, нам надо сохранить только его параметры
                            //извлекаем значения введённых параметров из текущего row
                            for (int parameterColumnIndex = columnIndex + 2; parameterColumnIndex < row.Table.Columns.Count; parameterColumnIndex++)
                            {
                                //извлекаем идентификатор параметра
                                if (int.TryParse(row.Table.Columns[parameterColumnIndex].ColumnName, out int manualInputParamID))
                                {
                                    //извлекаем значение параметра
                                    string manualInputParamValue = row[parameterColumnIndex].ToString();

                                    //если пользователь не ввёл значение параметра - будем заменять null на 0
                                    if (double.TryParse(manualInputParamValue ?? "0", out double dManualInputParamValue))
                                        Types.DbRoutines.SaveToManualInputAssemblyProtocolParam(assemblyProtocolID, manualInputParamID, dManualInputParamValue);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void BtExchangeParam(object sender, RoutedEventArgs e)
        {
            //переименование вручную введённого параметра применительно к протоколу сборки
            //то есть для отображаемого протокола сборки выполняется замена идентификатора параметра на другой идентификатор
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(((MainWindow)this.Owner).PermissionsLo))
            {
                if (this.FProfID != -1)
                {
                    //вычисляем идентификатор вручную введённого параметра
                    if (dgAssemblyProtocolParam.SelectedCells[0].Column is DataGridNumericColumn column)
                    {
                        Binding b = (Binding)column.Binding;

                        if (int.TryParse(b.Path.Path, out int oldManualInputParamID))
                        {
                            //спрашиваем пользователя какой новый параметр он хочет взамен старого
                            //показываем пользователю содержимое справочника параметров, в котором он может создавать и выбирать созданный параметр
                            Types.Profiles.TemperatureCondition tc = Types.Profiles.TemperatureCondition.None;
                            List<Types.Profiles.TemperatureCondition> listTemperatureCondition = new List<Types.Profiles.TemperatureCondition>()
                        {
                            tc
                        };

                            ManualInputParams manualInputParams = new ManualInputParams(this.FProfID, listTemperatureCondition)
                            {
                                LoadProfileParametersHandler = null
                            };

                            if (manualInputParams.GetManualParameterID(out string temperatureCondition, out int newManualInputParamID, out string newManualInputParamName) ?? false)
                            {
                                foreach (DataRow row in this.FDataSource.Rows)
                                {
                                    if (int.TryParse(row[Common.Constants.AssemblyProtocolID].ToString(), out int assemblyProtocolID))
                                        DbRoutines.ExchangeManualInputAssemblyProtocolParam(assemblyProtocolID, oldManualInputParamID, newManualInputParamID);
                                }

                                //меняем название столбца в котором выполнено переименование параметра
                                DataColumn tableColumn = this.DataSource.Table.Columns[oldManualInputParamID.ToString()];

                                if (tableColumn != null)
                                {
                                    tableColumn.ColumnName = this.BindPathByManualInputParamIDValue(newManualInputParamID);

                                    column.Binding = new Binding(tableColumn.ColumnName);
                                    column.Header = newManualInputParamName;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ContextMenuDeleteCurrentRecord_Click(object sender, RoutedEventArgs e)
        {
            //удаление записи на которой стоит курсор в dgAssemblyProtocolParam - удяляем все места хранения параметров выбранного протокола сборки
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(((MainWindow)this.Owner).PermissionsLo))
            {
                //извлекаем значения идентификаторов из текущей записи
                int currentRowIndex = dgAssemblyProtocolParam.Items.IndexOf(dgAssemblyProtocolParam.CurrentItem);
                DataRow row = this.FDataSource.Rows[currentRowIndex];

                if (int.TryParse(row[Common.Constants.AssemblyProtocolID].ToString(), out int assemblyProtocolID))
                {
                    //удаляем вручную созданные места хранения всех параметров протокола сборки assemblyProtocolID
                    DbRoutines.DeleteFromManualInputAssemblyProtocolParamByAssemblyProtocolID(assemblyProtocolID);

                    this.FDataSource.Rows.Remove(row);
                }
            }
        }
    }
}
