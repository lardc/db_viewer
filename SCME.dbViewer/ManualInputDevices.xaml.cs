using SCME.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for ManualInputDevices.xaml
    /// </summary>
    public partial class ManualInputDevices : Window
    {
        const string ColumnCodeName = Common.Constants.Code;
        const string ColumnDevID = Common.Constants.DevID;

        DataTable FDataSource = new DataTable();

        public DataView DataSource
        {
            get
            {
                return this.FDataSource.DefaultView;
            }

            set { this.FDataSource = value.Table; }
        }

        private int? AssemblyProtocolID { get; set; } = null;

        //при выборе профиля пользователь хочет видеть код Item, который обрабатывается по выбранному ПЗ 
        private string SelectedItem { get; set; } = null;

        //профиль для которого были загружены из базы данных либо руками введены данные (строки и столбцы отображаемые в данной форме)
        private string ProfileGUID { get; set; } = null;

        //последний столбец this.DgDevices который имел фокус ввода 
        private DataGridColumn LastFocusedColumn { get; set; } = null;

        //выбранный (по нажатию кнопки btSelectProfile) пользователем профиль из списка профилей
        public ProfileData ProfileData { get; set; } = null;

        //дескриптор окна визуализации ожидания
        private IntPtr FProcessWaitVisualizerHWnd = IntPtr.Zero;
        private IntPtr ProcessWaitVisualizerHWnd
        {
            get { return this.FProcessWaitVisualizerHWnd; }
        }

        public ManualInputDevices(IntPtr processWaitVisualizerHWnd)
        {
            InitializeComponent();

            this.FProcessWaitVisualizerHWnd = processWaitVisualizerHWnd;
            this.CreateDeviceCodeColumn();
            this.FDataSource.Columns.Add(ColumnDevID);
        }

        public bool? ShowModal(string groupName, string profileGUID, int? assemblyProtocolID)
        {
            //демонстрация формы для создания изделий (вручную)
            if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(profileGUID))
            {
                this.AssemblyProtocolID = assemblyProtocolID;
                this.tbGroupName.Text = groupName;
                this.RefreshItemsSourceOfCmbProfileName();

                //разыскиваем в списке загруженных профилей профиль с принятым profileGUID и делаем его выбранным
                ProfileData item = this.CmbProfileName.Items.Cast<ProfileData>().Where(x => x.ProfGUID == profileGUID).SingleOrDefault();

                if (item != null)
                {
                    int index = this.CmbProfileName.Items.IndexOf(item);
                    this.CmbProfileName.SelectedIndex = index;

                    this.LoadData(groupName, item.ProfGUID);
                }
            }

            bool? result = this.ShowDialog();

            return result;
        }

        private void AfterChangeGroupName()
        {
            //чтобы в форме выбора профиля можно было показать пользователю код изделия (по которому пользователь может ориентироваться при выборе профиля) вычисляем код изделия
            //в поле ввода ПЗ пользователь может ввести всё что угодно и вполне возможно, что введённое обозначение ПЗ таковым не окажется

            try
            {
                this.SelectedItem = string.IsNullOrEmpty(this.tbGroupName.Text) ? null : DbRoutines.ItemByGroupName(this.tbGroupName.Text, out string description);
            }
            catch (Exception)
            {
                this.SelectedItem = null;
            }

            //если пользователь ввёл ПЗ то независимо от того было оно успешно обработано выше стоящим вызовом или нет - всегда обновляем источник данных CmbProfileName 
            this.RefreshItemsSourceOfCmbProfileName();

            this.CmbProfileName.SelectedIndex = (this.CmbProfileName.Items.Count > 0) ? 0 : -1;
        }

        /*
        private void DeleteManuallyCreatedDevices()
        {
            //если форма отображает один единственный столбец с измерениями (изделиями) - те из них, которые были созданы вручную должны быть удалены
            if ((this.DgDevices.Items.Count > 0) && (this.DgDevices.Columns.Count == 1))
            {
                System.Data.SqlClient.SqlConnection connection = SCME.Types.DBConnections.Connection;

                bool connectionOpened = false;

                if (!DbRoutines.IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                System.Data.SqlClient.SqlTransaction transaction = connection.BeginTransaction();

                DataRow row = null;

                try
                {
                    foreach (object item in this.DgDevices.Items)
                    {
                        if (item is DataRowView dataRowView)
                        {
                            row = dataRowView.Row;

                            if (int.TryParse(row[Common.Constants.DevID].ToString(), out int devID))
                                DbRoutines.DeleteFromDevices(connection, transaction, devID);
                        }
                    }
                }
                catch (Exception exc)
                {
                    //если есть проблемы хотя-бы с одной записью - отменяем удаление всех записей
                    transaction.Rollback();

                    //в результате удаления была ошибка - покажем её пользователю
                    string errorsOnDeleted = string.Concat("'", row[Common.Constants.Code].ToString(), "'.", Constants.StringDelimeter, Constants.StringDelimeter, exc.Message);
                    MessageBox.Show(errorsOnDeleted, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                //раз мы здесь - удаление прошло без ошибок
                transaction.Commit();

                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если  оединение к БД было открыто в этой реализации - закрываем его
                if (connectionOpened)
                    connection.Close();
            }
        }
        */

        private bool CheckTemperatureMode(string selectedProfileGUID)
        {
            //входной параметр profileGUID - GUID выбранного пользователем профиля
            //если в this.dgDevices пользователь создал хотя-бы один столбец - смена выбранного профиля в CmbProfileName возможна только если температурные режимы профиля this.FProfileGUID и выбранного пользователем профиля одинаковы            
            //возвращает:
            // true - выбранный пользователем профиль имеет тот же температурный режим, что у профиля this.FProfileGUID
            // false - температурный режим выбранного пользователем профиля не совпадает с температурным режимом профиля this.FProfileGUID

            //пока пользователь не создал ни одного столбца - dgDevices содержит только один столбец обозначения изделий, принадлежность отображаемых данных к температурному режиму отсутствует
            if ((this.ProfileGUID != null) && (this.DgDevices.Columns.Count > 1))
            {
                double? temperature = DbRoutines.TemperatureByProfileGUID(this.ProfileGUID);
                if (temperature == null)
                    throw new Exception(string.Concat(Properties.Resources.ProfileOfDisplayedData, ". ", Properties.Resources.UnknownTemperatureCondition));

                Types.Profiles.TemperatureCondition temperatureCondition = Routines.TemperatureConditionByTemperature((double)temperature);

                double? selectedTemperature = DbRoutines.TemperatureByProfileGUID(selectedProfileGUID);
                if (selectedTemperature == null)
                    throw new Exception(string.Concat(Properties.Resources.SelectedProfile, ". ", Properties.Resources.UnknownTemperatureCondition));

                Types.Profiles.TemperatureCondition selectedTemperatureCondition = Routines.TemperatureConditionByTemperature((double)selectedTemperature);

                bool result = (temperatureCondition == selectedTemperatureCondition);

                return result;
            }

            //раз мы здесь - проверять нечего, проверка успешно завершена
            return true;
        }

        private string SelectGroupName(string groupName, out int? selectedGroupID, out string selectedItem)
        {
            //выбор ПЗ из списка всех ПЗ, зарегистрированных в справочнике SyteLine со статусом 'C' и префиксом '4-'
            //если принятый groupName не null - устанавливает курсор на строку с groupName
            SelectJob selectJob = new SelectJob
            {
                Owner = this
            };

            string selectedGroupName = groupName;

            if (selectJob.ShowModal("4", ref selectedGroupName, out selectedGroupID, out selectedItem))
                return selectedGroupName;

            //пользователь не выбрал ПЗ
            selectedGroupID = null;

            return null;
        }

        private string PartCode()
        {
            //вычисляем часть обозначения изделия
            //пример возвращаемого результата: "/4-00023508"
            string result = this.tbGroupName.Text;

            int index = result.IndexOf("-", 0);

            if (index != -1)
            {
                //выбрасываем суффикс
                index = result.IndexOf("-", index + 1);

                if (index != -1)
                    result = result.Remove(index);
            }

            return string.Concat("/", result);
        }

        private DataGridCodeColumn CreateDeviceCodeColumn()
        {
            DataGridCodeColumn column = null;

            if (this.DgDevices != null)
            {
                DataColumn tableColumn = new DataColumn
                {
                    ColumnName = ColumnCodeName,
                    DataType = typeof(int),
                    Unique = true,
                    AllowDBNull = false,
                    AutoIncrement = false,
                    DefaultValue = DBNull.Value
                };

                this.FDataSource.Columns.Add(tableColumn);

                column = new DataGridCodeColumn
                {
                    Header = Properties.Resources.Code,
                    Binding = new Binding(ColumnCodeName)
                };

                this.DgDevices.Columns.Add(column);
            }

            return column;
        }

        private DataGridNumericColumn CreateColumnInDataGrid(string header, string bindPath)
        {
            DataGridNumericColumn column = new DataGridNumericColumn
            {
                Header = header,
                Binding = new Binding(bindPath)
                {
                    //Converter = new Common.DoubleValueConverter(),
                    //ConverterParameter = 0,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            };

            this.DgDevices.Columns.Add(column);

            return column;
        }

        private DataGridNumericColumn CreateColumn(string header, string bindPath)
        {
            DataColumn tableColumn = new DataColumn
            {
                ColumnName = bindPath,
                DataType = typeof(string),//double
                DefaultValue = DBNull.Value,//0,
                AllowDBNull = true,//false
                Unique = false,
                AutoIncrement = false
            };

            this.FDataSource.Columns.Add(tableColumn);

            DataGridNumericColumn column = this.CreateColumnInDataGrid(header, bindPath);

            return column;
        }

        private void BtSelectGroupName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (this.IsDataGridHaveError())
                {
                    MessageBox.Show(Properties.Resources.DataContainsAnError, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                btn.IsEnabled = false;

                try
                {
                    string selectedGroupName = this.SelectGroupName(this.tbGroupName.Text, out int? selectedGroupID, out string selectedItem);

                    if (selectedGroupName != null)
                    {
                        this.tbGroupName.Text = selectedGroupName;
                        this.SelectedItem = selectedItem;

                        this.AfterChangeGroupName();
                    }
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private string BindPathByManualInputParamIDValue(int manualInputParamID)
        {
            return manualInputParamID.ToString();
        }

        private void LoadProfileParameters(List<string> profileParameters)
        {
            //загрузка списка параметров профиля
            if (this.CmbProfileName.SelectedItem is ProfileData profileData)
                Types.DbRoutines.LoadProfileParameters(profileData.ProfGUID, profileParameters);
        }

        private void BtNewParam_Click(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanCreateValueOfManuallyEnteredParameter(((MainWindow)this.Owner).PermissionsLo))
            {
                if (this.IsDataGridHaveError())
                {
                    MessageBox.Show(Properties.Resources.DataContainsAnError, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                if (this.FDataSource.Rows.Count == 0)
                {
                    MessageBox.Show(Properties.Resources.ErrorCreateColumnByListOfRecordsIsEmpty, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                //создание столбца в dgDevices
                //пока не выбрано ПЗ - не будет создан столбец для работы с кодом изделия
                //не выбрано ПЗ - не позволяем создание новых столбцов
                string groupName = string.IsNullOrEmpty(tbGroupName.Text) ? null : tbGroupName.Text.Trim();

                if (string.IsNullOrEmpty(groupName) || (groupName == Properties.Resources.NotSetted))
                {
                    MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.GroupName, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                int profID = -1;

                if (this.ProfileGUID == null)
                {
                    if ((this.CmbProfileName.SelectedItem != null) && (this.CmbProfileName.SelectedItem is ProfileData profileData))
                    {
                        this.ProfileGUID = profileData.ProfGUID;
                        profID = profileData.ProfID;
                    }
                }

                if (this.ProfileGUID == null)
                {
                    MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.ProfileName, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                if (profID == -1)
                {
                    profID = DbRoutines.ProfileIDByProfileGUID(this.ProfileGUID);
                }

                //убеждаемся, что по this.ProfileGUID корректно вычислен идентификатор профиля
                if (profID == -1)
                {
                    MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.ProfileName, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    
                    return;
                }

                //показываем пользователю содержимое справочника параметров, в котором он может создавать и выбирать созданный им параметр
                double? temperature = DbRoutines.TemperatureByProfileGUID(this.ProfileGUID);
                Types.Profiles.TemperatureCondition tc = (temperature == null) ? Types.Profiles.TemperatureCondition.None : Routines.TemperatureConditionByTemperature((double)temperature);
                List<Types.Profiles.TemperatureCondition> listTemperatureCondition = new List<Types.Profiles.TemperatureCondition>()
                {
                    //tc
                    
                    Types.Profiles.TemperatureCondition.RT,
                    Types.Profiles.TemperatureCondition.TM
                };

                ManualInputParams manualInputParams = new ManualInputParams(profID, listTemperatureCondition);

                if (manualInputParams.LoadProfileParametersHandler == null)
                    manualInputParams.LoadProfileParametersHandler = this.LoadProfileParameters;

                if (manualInputParams.GetManualParameterID(out string temperatureCondition, out int manualInputParamID, out string manualInputParamName) ?? false)
                {
                    //проверяем уникальность имени создаваемого столбца
                    if (this.DgDevices.Columns.FirstOrDefault(c => c.Header.ToString() == manualInputParamName) == null)
                    {
                        string bindPath = this.BindPathByManualInputParamIDValue(manualInputParamID);
                        this.CreateColumn(manualInputParamName, bindPath);

                        this.SetFocuseToFirstGridCell();
                    }
                    else
                        MessageBox.Show(string.Format(Properties.Resources.ParemeterIsInUse, manualInputParamName), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        public class DataGridCodeColumn : DataGridTextColumn
        {
            protected override object PrepareCellForEdit(System.Windows.FrameworkElement editingElement, System.Windows.RoutedEventArgs editingEventArgs)
            {
                if (editingElement is TextBox tb)
                {
                    //PreviewTextInput не отрабатывает при вставке из буфера обмена - поэтому вешаем свой обработчик
                    DataObject.AddPastingHandler(tb, OnPasteHandler);

                    tb.PreviewTextInput += DataGridCodeColumn_OnPreviewTextInput;
                    tb.PreviewKeyDown += DataGridCodeColumn_OnPreviewKeyDown;
                }

                return base.PrepareCellForEdit(editingElement, editingEventArgs);
            }

            private void OnPasteHandler(object sender, DataObjectPastingEventArgs e)
            {
                //значение поля идентификации измерения (изделия) может быть только целым числом 
                if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true))
                    return;

                bool parced = int.TryParse(e.SourceDataObject.GetData(DataFormats.UnicodeText).ToString(), out int devicecode);

                if (!parced)
                    e.CancelCommand();
            }

            private void DataGridCodeColumn_OnPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
            {
                //на пробел данное событие не реагирует - поэтому пользователь может ввести пробелы в имени измерения
                //имя мзмерения должно быть записано целым числом
                if (e.OriginalSource is TextBox tb)
                {
                    string allEntered = tb.Text.Insert(tb.CaretIndex, e.Text);
                    e.Handled = !int.TryParse(allEntered, out int iValue);
                }
            }

            private void DataGridCodeColumn_OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                //избавляемся от возможных пробелов в обозначении изделия (измерения)
                if (e.Key == Key.Enter)
                {
                    if (sender is TextBox tb)
                        tb.Text = tb.Text.Replace(" ", string.Empty);
                }
            }
        }

        public class DataGridNumericColumn : DataGridTextColumn
        {
            protected override object PrepareCellForEdit(System.Windows.FrameworkElement editingElement, System.Windows.RoutedEventArgs editingEventArgs)
            {
                if (editingElement is TextBox tb)
                {
                    //PreviewTextInput не отрабатывает при вставке из буфера обмена - поэтому вешаем свой обработчик
                    DataObject.AddPastingHandler(tb, this.OnPasteHandler);

                    tb.PreviewTextInput += this.NumericColumn_OnPreviewTextInput;
                    tb.PreviewKeyDown += this.NumericColumn_OnPreviewKeyDown;
                }

                return base.PrepareCellForEdit(editingElement, editingEventArgs);
            }

            private void OnPasteHandler(object sender, DataObjectPastingEventArgs e)
            {
                Common.Routines.TextBoxOnlyDoublePaste(sender, e);
            }

            private void NumericColumn_OnPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
            {
                Common.Routines.TextBoxOnlyDouble_PreviewTextInput(sender, e);
            }

            private void NumericColumn_OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                //избавляемся от возможных пробелов в значениях параметров
                if (e.Key == Key.Enter)
                {
                    if (sender is TextBox tb)
                        tb.Text = tb.Text.Replace(" ", string.Empty);
                }
            }
        }

        private void BtSave_Click(object sender, RoutedEventArgs e)
        {
            //если сохраняемые данные содержат ошибку - ругаемся и ничего не сохраняем
            if (IsDataGridHaveError())
            {
                MessageBox.Show(Properties.Resources.DataContainsAnError, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return;
            }

            string selectedProfGUID = null;

            if ((this.CmbProfileName.SelectedItem != null) && (this.CmbProfileName.SelectedItem is ProfileData selectedProfileData))
                selectedProfGUID = selectedProfileData.ProfGUID;

            if ((this.ProfileGUID == null) || (selectedProfGUID == null))
            {
                //профиль не выбран пользователем, либо его обозначение не корректно - ругаемся и прекращаем исполнение данной реализации
                MessageBox.Show(string.Concat(Properties.Resources.Profile, ". ", Properties.Resources.ValueIsNotGood, ". "), string.Concat(Properties.Resources.CheckValue, " ", Properties.Resources.Profile), MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return;
            }

            //заполнено оба поля: this.ProfileGUID, selectedProfGUID
            //проверяем, что нет конфликта по температурному режиму
            if (!this.CheckTemperatureMode(selectedProfGUID))
            {
                //температурные режимы текущего установленного профиля this.ProfileGUID и выбранного пользователем selectedProfGUID не совпадают - ругаемся
                MessageBox.Show(Properties.Resources.DifferentTemperatureModes, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return;
            }

            SCME.Common.Routines.ShowProcessWaitVisualizer(this, this.ProcessWaitVisualizerHWnd);

            try
            {
                string groupName = string.IsNullOrEmpty(this.tbGroupName.Text) ? null : this.tbGroupName.Text.Trim();
                int? groupID = null;

                if (!string.IsNullOrEmpty(groupName) && (groupName != Properties.Resources.NotSetted))
                {
                    //ПЗ выбран из списка ПЗ, существующих в SL, получаем от нашей базы данных идентификатор ПЗ имеющий обозначение groupName, если его не существует - он будет создан
                    groupID = Types.DbRoutines.CreateGroupID(groupName);
                }
                else
                {
                    //пользователь не выбрал ПЗ, либо его обозначение не корректно - ругаемся и прекращаем исполнение данной реализации
                    MessageBox.Show(string.Concat(Properties.Resources.GroupName, ". ", Properties.Resources.ValueIsNotGood, ". "), string.Concat(Properties.Resources.CheckValue, " ", Properties.Resources.GroupName), MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                //получаем значение для поля USR
                long userID = ((MainWindow)this.Owner).FUserID;
                Types.DbRoutines.FullUserNameByUserID(userID, out string fullUserName);

                //если что-то не было переписано в dgDevices.ItemsSource - сделаем это
                this.DgDevices.CommitEdit();

                //выполняем сохранение списка введённых измерений
                //проверки на уникальность обозначения кода изделия и не null значения обеспечивает dgDevices
                System.Data.SqlClient.SqlConnection connection = SCME.Types.DBConnections.Connection;

                bool connectionOpened = false;

                if (!DbRoutines.IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                string deviceCode = null;

                System.Data.SqlClient.SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    foreach (DataRow row in this.FDataSource.Rows)
                    {
                        //флаг - 'Место для хранения результата измерений создано сейчас'
                        bool devIDCreatedNow = false;
                        int devID = -1;

                        //извлекаем из row код изделия
                        int columnIndex = row.Table.Columns.IndexOf(ColumnCodeName);

                        if (columnIndex != -1)
                        {
                            //смотрим с чем мы имеем дело: с созданием нового изделия или редактированием уже существующего в базе данных    
                            deviceCode = row[columnIndex].ToString().Trim();

                            if (!string.IsNullOrEmpty(deviceCode))
                            {
                                //если пользователь ввёл порядковый номер изделия - выполняем сохранение
                                //дописываем к коду изделия часть обозначения ПЗ
                                deviceCode = string.Concat(deviceCode, this.PartCode());

                                columnIndex = row.Table.Columns.IndexOf(ColumnDevID);

                                if (columnIndex != -1)
                                {
                                    object objDevID = row[columnIndex];

                                    if ((objDevID == DBNull.Value) || (this.ProfileGUID != selectedProfGUID))
                                    {
                                        //изделие только что введено пользователем, значение его идентификатора DEV_ID нам не известно или значение идентификатора DEV_ID известно, выполняется копирование отображаемых параметров под другим профилем
                                        //может быть два варианта:
                                        // - изделие с кодом deviceCode, groupID, profileGUID в таблице DEVICES не существует - требуется его создание;
                                        // - изделие с кодом deviceCode, groupID, profileGUID в таблице DEVICES уже создано - используем его идентификатор
                                        devID = Types.DbRoutines.IsDeviceExist(connection, transaction, (int)groupID, deviceCode, selectedProfGUID);

                                        //если измерение с заданными реквизитами не существует и для него определены сохраняемые параметры - создаём его
                                        if ((devID == -1) && (this.DgDevices.Columns.Count > 1))
                                        {
                                            //изделие не существует - выполняем его создание
                                            devID = DbRoutines.CreateDevice(connection, transaction, (int)groupID, selectedProfGUID, deviceCode, fullUserName);

                                            //запоминаем в текущей записи идентификатор devID только что созданной в таблице DEVICES записи
                                            row[columnIndex] = devID;

                                            //запоминаем что мы только что создали место хранения результата измерения devID
                                            devIDCreatedNow = true;
                                        }
                                    }
                                    else
                                    {
                                        if (int.TryParse(objDevID.ToString(), out devID))
                                        {
                                            //devID существует
                                            bool deviceCreatedManually = (DbRoutines.IsDeviceCreatedManually(connection, transaction, devID));

                                            //проверим определил ли по данному измерению пользователь хотя бы один параметр
                                            if (this.DgDevices.Columns.Count > 1)
                                            {
                                                if (DbRoutines.DeviceCodeByDevID(connection, transaction, devID) != deviceCode)
                                                {
                                                    if (deviceCreatedManually)
                                                    {
                                                        //данное измерение создано вручную - пользователю можно изменять его наименование
                                                        DbRoutines.UpdateDeviceCode(connection, transaction, devID, deviceCode);
                                                    }
                                                    else
                                                    {
                                                        //но если это не так и пользователь пытается редактировать наименование - ругаемся и не разрешаем это
                                                        throw new Exception(string.Concat(Properties.Resources.ChangeProhibited, ". ", Properties.Resources.NotCreatedByHand, "."));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //по данному измерению пользователь не определил ни одного параметра - необходимость в данном измерении отсутствует
                                                if (deviceCreatedManually)
                                                    DbRoutines.DeleteFromDevices(connection, transaction, devID);
                                            }
                                        }
                                    }
                                }

                                if (devID != -1)
                                {
                                    //если измерение было создано не сейчас - в нём могут быть свои параметры созданные вручную, чтобы не было конфликта в уникальности имён - удаляем все параметры созданные вручную у давно созданного измерения с идентификатором devID
                                    if (!devIDCreatedNow)
                                        DbRoutines.DeleteFromManualInputDevParamByDevID(connection, transaction, devID);

                                    //извлекаем значения введённых параметров из текущего row
                                    for (int parameterColumnIndex = columnIndex + 1; parameterColumnIndex < row.Table.Columns.Count; parameterColumnIndex++)
                                    {
                                        //извлекаем идентификатор параметра
                                        if (int.TryParse(row.Table.Columns[parameterColumnIndex].ColumnName, out int manualInputParamID))
                                        {
                                            //извлекаем значение параметра
                                            string manualInputParamValue = Common.Routines.SimpleFloatingValueToFloatingValue(row[parameterColumnIndex].ToString());

                                            double? dManualInputParamValue = null;

                                            if (!string.IsNullOrEmpty(manualInputParamValue))
                                            {
                                                if (double.TryParse(manualInputParamValue, out double dValue))
                                                    dManualInputParamValue = dValue;
                                            }

                                            Types.DbRoutines.SaveToManualInputDevParam(connection, transaction, devID, manualInputParamID, dManualInputParamValue);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    transaction.Rollback();
                    MessageBox.Show(string.Concat("'", deviceCode, "'", Constants.StringDelimeter, Properties.Resources.SaveFailed, Constants.StringDelimeter, exc.Message), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                transaction.Commit();

                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если  оединение к БД было открыто в этой реализации - закрываем его
                if (connectionOpened)
                    connection.Close();
            }
            finally
            {
                SCME.Common.Routines.HideProcessWaitVisualizer(this.ProcessWaitVisualizerHWnd);
            }

            //если мы сюда добрались и было что сохранять - сохранение выполнено успешно
            if (this.FDataSource.Rows.Count != 0)
                MessageBox.Show(Properties.Resources.SaveWasSuccessful, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void BtSelectProfile_Click(object sender, RoutedEventArgs e)
        {
            //выбор профиля из списка всех профилей
            if (sender is Button btn)
            {
                if (this.IsDataGridHaveError())
                {
                    MessageBox.Show(Properties.Resources.DataContainsAnError, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                btn.IsEnabled = false;

                try
                {
                    SelectProfile selectProfile = new SelectProfile
                    {
                        Owner = this
                    };

                    int? selectedProfileID = null;

                    string tittle = string.Concat(Properties.Resources.SelectProfile, " ", this.SelectedItem);
                    if (selectProfile.ShowModal(tittle, ref selectedProfileID, out string selectedProfile, out string selectedProfileGUID))
                    {
                        this.CmbProfileName.SelectedIndex = -1;

                        if (this.ProfileData == null)
                            this.ProfileData = new ProfileData();

                        this.ProfileData.ProfGUID = selectedProfileGUID;
                        this.ProfileData.ProfID = (int)selectedProfileID;
                        this.ProfileData.ProfName = selectedProfile;

                        //обновляем this.CmbProfileName.ItemsSource
                        this.RefreshItemsSourceOfCmbProfileName();

                        //в результате обновления this.CmbProfileName.ItemsSource нулевым элементом списка в нём будет выбранный пользователем профиль
                        this.CmbProfileName.SelectedIndex = 0;
                    }
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private void BtLoadData_Click(object sender, RoutedEventArgs e)
        {
            /*
                        DataGridRow currentRow = this.DgDevices.GetRow(0); //DataGridRow.GetRowContainingElement(firstCell);
                        System.Collections.ObjectModel.ReadOnlyObservableCollection<ValidationError> errors = Validation.GetErrors(currentRow);
            */

            if (this.CmbProfileName.SelectedItem is ProfileData item)
            {
                string groupName = this.tbGroupName.Text;
                string profileGuid = item.ProfGUID;

                if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(profileGuid))
                {
                    if (string.IsNullOrEmpty(groupName))
                        MessageBox.Show(string.Concat(Properties.Resources.GroupName, " ", Properties.Resources.NotSetted, ".", Constants.StringDelimeter, Properties.Resources.DataLoadingFailed, "."), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    if (string.IsNullOrEmpty(profileGuid))
                        MessageBox.Show(string.Concat(Properties.Resources.Profile, " ", Properties.Resources.NotSetted, ".", Constants.StringDelimeter, Properties.Resources.DataLoadingFailed, "."), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                    this.LoadData(groupName, profileGuid);
            }
        }

        private void RemoveDgDevicesColumns()
        {
            //удаляет все столбцы в DgDevices кроме столбца ColumnCodeName
            for (int i = this.DgDevices.Columns.Count - 1; i >= 0; i--)
            {
                DataGridColumn column = this.DgDevices.Columns[i];

                if (!(column is DataGridCodeColumn))
                    this.DgDevices.Columns.Remove(column);
            }
        }

        private void RemoveDataSourceColumns()
        {
            for (int i = this.FDataSource.Columns.Count - 1; i >= 0; i--)
            {
                DataColumn tableColumn = this.FDataSource.Columns[i];

                if ((tableColumn.ColumnName != ColumnCodeName) && (tableColumn.ColumnName != ColumnDevID))
                    this.FDataSource.Columns.Remove(tableColumn);
            }
        }

        private void Init()
        {
            //восстанавливает this.DgDevices и this.FDataSource до состояния, которое они имели перед загрузкой данных 

            //удаляем все созданные пользователем столбцы в this.DgDevices кроме столбца ColumnCodeName
            this.RemoveDgDevicesColumns();

            //удаляем все записи в this.FDataSource
            this.FDataSource.Clear();

            //удаляем все столбцы в this.FDataSource кроме ColumnCodeName и ColumnDevID
            this.RemoveDataSourceColumns();

            //все загруженные и введённые данные удалены - поэтому
            this.ProfileGUID = null;
        }

        private void LoadData(string groupName, string profileGuid)
        {
            //считываем данные из базы данных
            if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(profileGuid))
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
                    this.Init();

                    List<Types.DbRoutines.ColumnBindingDescr> columnBindingList = Types.DbRoutines.LoadManualInputDevParam(this.FDataSource, groupName, profileGuid, this.AssemblyProtocolID);

                    switch (this.FDataSource.Rows.Count)
                    {
                        case 0:
                            //пользователь никогда не добавлял вручную созданные параметры для сочетания groupName, profileGuid
                            Types.DbRoutines.LoadCodeNumbers(this.FDataSource, groupName, profileGuid, this.AssemblyProtocolID);

                            //если данные загружены (список this.FDataSource не пуст) - запоминаем для какого профиля они были загружены
                            if (this.FDataSource.Rows.Count > 0)
                                this.ProfileGUID = profileGuid;

                            break;

                        default:
                            {
                                //данные загружены - запоминаем для какого профиля они были загружены
                                this.ProfileGUID = profileGuid;

                                if (columnBindingList != null)
                                {
                                    //проходим по полученным данным с целью построить столбцы в DataGrid для их отображения
                                    foreach (Types.DbRoutines.ColumnBindingDescr bindingDescr in columnBindingList)
                                        this.CreateColumnInDataGrid(bindingDescr.Header, bindingDescr.BindPath);
                                }
                            }

                            break;
                    }
                }

                this.SetFocuseToFirstGridCell();
            }
        }

        private void SetFocuseToFirstGridCell()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 400);

            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (sender is System.Windows.Threading.DispatcherTimer dispatcherTimer)
            {
                //делаем не видимым индикатор сортировки
                this.DgDevices.UnVisibleSortIndicator();

                if ((this.DgDevices.Items.Count != 0) && (this.DgDevices.Columns.Count != 0))
                {
                    DataGridCell gridCell = this.DgDevices.GetCell(0, 0);
                    gridCell.Focus();
                }

                dispatcherTimer?.Stop();
            }
        }

        private bool IsDataGridHaveError()
        {
            //в dgDevices установлено ограничение на уникальность содержимого поля обозначения изделия (нулевой столбец) и столбцы со значениями параметров не могут содержать null значения
            //данная реализация возвращает:
            // true - dgDevices имеет ошибки во введённых данных;
            // false - dgDevices не выявил ошибок во введённых данных

            bool errors = (from c in
                               from object i in this.DgDevices.ItemsSource
                               select this.DgDevices.ItemContainerGenerator.ContainerFromItem(i)
                           where c != null
                           select Validation.GetHasError(c)).FirstOrDefault(x => x);

            return errors;
        }

        private void DataGridCell_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //при обычном поведении DataGrid нажатие на клавишу Enter приводит к переходу курсора на следующую строку в текущем столбце
            //технологи хотят чтобы при нажатии на Enter курсор двигался в следующий справа столбец и только из последнего столбца курсор должен стать на новую запись в первом столбце
            if ((this.DgDevices.Items.Count != 0) && (sender is DataGridCell cell) && (e.OriginalSource is UIElement uiElement))
            {
                if (!this.IsDataGridHaveError())
                {
                    switch (e.Key)
                    {
                        case Key.Enter:
                            if (cell.Column.DisplayIndex == this.DgDevices.Columns.Count - 1)
                            {
                                //выбрана ячейка в последнем столбце
                                //делаем текущей ячейкой самую первую ячейку текущей строки и дальше полагаемся на стандартную обработку DataGrid клавиши 'Enter'
                                DataGridRow currentRow = DataGridRow.GetRowContainingElement(cell);
                                int currentRowIndex = currentRow.GetIndex();

                                if (currentRowIndex < this.DgDevices.Items.Count - 1)
                                    currentRowIndex++;

                                DataGridCellInfo firstCell = new DataGridCellInfo(this.DgDevices.Items[currentRowIndex], this.DgDevices.Columns[0]);
                                this.DgDevices.CurrentCell = firstCell;
                            }
                            else
                                uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                            //переводим DgDevices в режим редактирования - будет виден мигающий курсор в поле ввода значения в редактируемой ячейке
                            this.DgDevices.BeginEdit();

                            e.Handled = true;
                            break;

                        case Key.Left:
                            uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
                            e.Handled = true;
                            break;

                        case Key.Right:
                            uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
                            e.Handled = true;
                            break;

                        case Key.Up:
                            uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                            e.Handled = true;
                            break;

                        case Key.Down:
                            uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                            e.Handled = true;
                            break;

                        case Key.Home:
                            //перемещение фокуса в этом случае почему-то не работает
                            DataGridCellInfo homeCell = new DataGridCellInfo(this.DgDevices.Items[0], this.DgDevices.Columns[cell.Column.DisplayIndex]);
                            this.DgDevices.CurrentCell = homeCell;
                            e.Handled = true;
                            break;

                        case Key.End:
                            //перемещение фокуса в этом случае почему-то не работает
                            DataGridCellInfo endCell = new DataGridCellInfo(this.DgDevices.Items[this.DgDevices.Items.Count - 2], this.DgDevices.Columns[cell.Column.DisplayIndex]);
                            this.DgDevices.CurrentCell = endCell;
                            e.Handled = true;
                            break;
                    }
                }
            }
        }

        private void DataGridCell_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridCell dataGridCell)
            {
                dataGridCell.BorderThickness = new Thickness(2);
                this.LastFocusedColumn = dataGridCell.Column;
            }
        }

        private void DataGridCell_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridCell dataGridCell)
                dataGridCell.BorderThickness = new Thickness(0);
        }

        private void DgDevices_GotMouseCapture(object sender, MouseEventArgs e)
        {
            //если отображаеся пустой список, то при тыке мышкой в любое поле ввода dgDevices читаем список изделий, проходящих по выбранным ПЗ и профилю
            if (this.FDataSource.Rows.Count == 0)
            {
                if (this.CmbProfileName.SelectedItem is ProfileData selectedProfileData)
                    this.LoadData(tbGroupName.Text.TrimEnd(), selectedProfileData.ProfGUID);
            }
        }

        private void BtDeleteParam_Click(object sender, RoutedEventArgs e)
        {
            //удаление параметра (столбца в котором стоит курсор)
            //то есть удаляется множество мест хранения значений вручную введённых параметров
            if (Common.Routines.IsUserCanDeleteValueOfManuallyEnteredParameter(((MainWindow)this.Owner).PermissionsLo))
            {
                if (this.IsDataGridHaveError())
                {
                    MessageBox.Show(Properties.Resources.DataContainsAnError, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                //вычисляем идентификатор вручную введённого параметра
                if (this.LastFocusedColumn == null)
                {
                    MessageBox.Show(Properties.Resources.NoEditingObjectSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }
                else
                {
                    if (this.LastFocusedColumn is DataGridNumericColumn column)
                    {
                        Binding b = (Binding)column.Binding;

                        if (int.TryParse(b.Path.Path, out int manualInputParamID))
                        {
                            //спрашиваем разрешение пользователя на удаление столбца
                            if (MessageBox.Show(string.Concat(Properties.Resources.DeleteColumn, "?"), Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                foreach (DataRow row in this.FDataSource.Rows)
                                {
                                    if (int.TryParse(row[Common.Constants.DevID].ToString(), out int devID))
                                        DbRoutines.DeleteFromManualInputDevParam(devID, manualInputParamID);
                                }

                                //удаляем столбец
                                //перечитывать отображаемый список чтобы пользователь увидел результат не хорошо, т.к. в нём могут быть не сохранённые данные
                                this.DgDevices.Columns.Remove(column);
                                this.FDataSource.Columns.Remove(manualInputParamID.ToString());

                                //пересоздаём DataView по изменившемуся this.FDataSource
                                this.DgDevices.ItemsSource = null;
                                this.DgDevices.ItemsSource = this.DataSource;

                                this.SetFocuseToFirstGridCell();
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.SelectedDataIsNotForChange, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        this.SetFocuseToFirstGridCell();

                        return;
                    }
                }
            }
        }

        private void BtExchangeParam_Click(object sender, RoutedEventArgs e)
        {
            //замена вручную введённого параметра применительно к списку изделий
            //то есть для списка отображаемых изделий выполняется замена идентификатора параметра на другой идентификатор
            //если пользователю разрешено удаление создаваемого вручную параметра, то ему разрешено и его переименование
            if (Common.Routines.IsUserCanDeleteValueOfManuallyEnteredParameter(((MainWindow)this.Owner).PermissionsLo))
            {
                if (this.IsDataGridHaveError())
                {
                    MessageBox.Show(Properties.Resources.DataContainsAnError, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                //вычисляем идентификатор вручную введённого параметра
                if (this.DgDevices.SelectedCells.Count == 0)
                {
                    MessageBox.Show(Properties.Resources.NoEditingObjectSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    if (this.DgDevices.SelectedCells[0].Column is DataGridNumericColumn column)
                    {
                        Binding b = (Binding)column.Binding;

                        if ((this.CmbProfileName.SelectedItem is ProfileData profileData) && int.TryParse(b.Path.Path, out int oldManualInputParamID))
                        {
                            //спрашиваем пользователя какой новый параметр он хочет взамен старого
                            //показываем пользователю содержимое справочника параметров, в котором он может создавать и выбирать созданный параметр
                            double? temperature = DbRoutines.TemperatureByProfileGUID(profileData.ProfGUID);
                            Types.Profiles.TemperatureCondition tc = (temperature == null) ? Types.Profiles.TemperatureCondition.None : Routines.TemperatureConditionByTemperature((double)temperature);
                            List<Types.Profiles.TemperatureCondition> listTemperatureCondition = new List<Types.Profiles.TemperatureCondition>()
                            {
                               tc
                            };

                            ManualInputParams manualInputParams = new ManualInputParams(profileData.ProfID, listTemperatureCondition);

                            if (manualInputParams.LoadProfileParametersHandler == null)
                                manualInputParams.LoadProfileParametersHandler = this.LoadProfileParameters;

                            if (manualInputParams.GetManualParameterID(out string temperatureCondition, out int newManualInputParamID, out string newManualInputParamName) ?? false)
                            {
                                //проверяем уникальность имени создаваемого столбца
                                if (this.DgDevices.Columns.FirstOrDefault(c => c.Header.ToString() == newManualInputParamName) == null)
                                {
                                    foreach (DataRow row in this.FDataSource.Rows)
                                    {
                                        if (int.TryParse(row[Common.Constants.DevID].ToString(), out int devID))
                                            DbRoutines.ExchangeManualInputDevParam(devID, oldManualInputParamID, newManualInputParamID);
                                    }

                                    //меняем название столбца в котором выполнено переименование параметра
                                    DataColumn tableColumn = this.FDataSource.Columns[oldManualInputParamID.ToString()];

                                    if (tableColumn != null)
                                    {
                                        tableColumn.ColumnName = this.BindPathByManualInputParamIDValue(newManualInputParamID);

                                        column.Binding = new Binding(tableColumn.ColumnName);
                                        column.Header = newManualInputParamName;
                                    }
                                }
                                else
                                {
                                    MessageBox.Show(string.Format(Properties.Resources.ParemeterIsInUse, newManualInputParamName), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.SelectedDataIsNotForChange, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
        }

        private void DgDevices_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is DataGrid dg)
            {
                if (e.Command == DataGrid.DeleteCommand)
                {
                    //удаление записи на которой стоит курсор в dgDevices
                    if (Common.Routines.IsUserCanDeleteValueOfManuallyEnteredParameter(((MainWindow)this.Owner).PermissionsLo))
                    {
                        switch (dg.SelectedItems.Count > 1)
                        {
                            case true:
                                //удаление множества выбранных записей
                                if (MessageBox.Show(string.Concat(Properties.Resources.DeleteSelectedRecords, "?"), Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    System.Data.SqlClient.SqlConnection connection = SCME.Types.DBConnections.Connection;

                                    bool connectionOpened = false;

                                    if (!DbRoutines.IsDBConnectionAlive(connection))
                                    {
                                        connection.Open();
                                        connectionOpened = true;
                                    }

                                    System.Data.SqlClient.SqlTransaction transaction = connection.BeginTransaction();

                                    List<DataRow> listSelectedRows = new List<DataRow>();
                                    DataRow row = null;

                                    try
                                    {
                                        foreach (object item in dg.SelectedItems)
                                        {
                                            if (item is DataRowView dataRowView)
                                            {
                                                row = dataRowView.Row;

                                                if (int.TryParse(row[Common.Constants.DevID].ToString(), out int devID))
                                                {
                                                    DbRoutines.DeleteManualInputDevice(connection, transaction, devID);
                                                    listSelectedRows.Add(row);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        //если есть проблемы хотя-бы с одной записью - отменяем удаление всех записей
                                        transaction.Rollback();
                                        e.Handled = true;

                                        //при удалении есть ошибка - покажем её пользователю
                                        string errorsOnDeleted = string.Concat("'", row[Common.Constants.Code].ToString(), "'.", Constants.StringDelimeter, Constants.StringDelimeter, exc.Message);
                                        MessageBox.Show(errorsOnDeleted, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);

                                        return;
                                    }

                                    //раз мы здесь - удаление выполнено успешно
                                    transaction.Commit();

                                    //удаляем выбранные записи из FDataSource
                                    foreach (DataRow selectedRow in listSelectedRows)
                                        this.FDataSource.Rows.Remove(selectedRow);

                                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                                    //если  оединение к БД было открыто в этой реализации - закрываем его
                                    if (connectionOpened)
                                        connection.Close();

                                    MessageBox.Show(Properties.Resources.SelectedRecordsDeletedSuccessfully, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                                else
                                    e.Handled = true;

                                break;

                            default:
                                //удаление одной выбранной записи
                                if (MessageBox.Show(string.Concat(Properties.Resources.DeleteCurrentRecord, "?"), Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    //извлекаем значения идентификатора текущей записи
                                    object currentItem = this.DgDevices.CurrentItem;

                                    if ((currentItem != null) && (currentItem is DataRowView dataRowView))
                                    {
                                        DataRow row = dataRowView.Row;

                                        if (int.TryParse(row[Common.Constants.DevID].ToString(), out int devID))
                                        {
                                            //идентификатор DEV_ID успешно прочитан из выбранной записи - значит выбранная запись уже есть в наличии в базе данных
                                            System.Data.SqlClient.SqlConnection connection = SCME.Types.DBConnections.Connection;

                                            bool connectionOpened = false;

                                            if (!DbRoutines.IsDBConnectionAlive(connection))
                                            {
                                                connection.Open();
                                                connectionOpened = true;
                                            }

                                            System.Data.SqlClient.SqlTransaction transaction = connection.BeginTransaction();

                                            try
                                            {
                                                DbRoutines.DeleteManualInputDevice(connection, transaction, devID);
                                            }
                                            catch (Exception exc)
                                            {
                                                transaction.Rollback();
                                                e.Handled = true;

                                                MessageBox.Show(exc.Message, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);

                                                return;
                                            }

                                            transaction.Commit();

                                            //удаляем текущую запись из FDataSource
                                            this.FDataSource.Rows.Remove(row);

                                            //если данная реализация открыла соединение к БД, то она же его должна закрыть
                                            //если  оединение к БД было открыто в этой реализации - закрываем его
                                            if (connectionOpened)
                                                connection.Close();

                                            MessageBox.Show(Properties.Resources.RecordSuccessfullyDeleted, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                        }
                                    }
                                }
                                else
                                    e.Handled = true;

                                break;
                        }
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        e.Handled = true;
                    }
                }
            }
        }

        private void DeleteCurrentRecord_Click(object sender, RoutedEventArgs e)
        {
            //эмулируем нажатие кнопки Delete на выбранной пользователем записи в this.dgDevices
            //для того чтобы это работало выбранная запись должна иметь фокус ввода
            int currentIndex = this.DgDevices.Items.IndexOf(this.DgDevices.CurrentItem);

            if (currentIndex != -1)
            {
                DependencyObject obj = this.DgDevices.ItemContainerGenerator.ContainerFromIndex(currentIndex);

                if (obj is DataGridRow row)
                {
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    System.Windows.Forms.SendKeys.SendWait("{Delete}");
                }
            }
        }

        private void RefreshItemsSourceOfCmbProfileName()
        {
            object obj = FindResource("ListOfProfiles");

            if (obj is ObjectDataProvider objectDataProvider)
                objectDataProvider.Refresh();
        }
    }

    public class ProfileData
    {
        //GUID профиля
        public string ProfGUID { get; set; }

        //значение идентификатора профиля PROF_ID
        public int ProfID { get; set; }

        //обозначения профиля
        public string ProfName { get; set; }
    }

    public class ListOfProfileData
    {
        public List<ProfileData> GetListOfProfileData()
        {
            List<ProfileData> result = new List<ProfileData>();

            string groupName = null;

            //если пользователь выбрал профиль - всегда ставим его в начало формируемого списка - даём ему нулевой индекс
            foreach (Window window in Application.Current.Windows)
            {
                if (window is ManualInputDevices manualInputDevices)
                {
                    if (manualInputDevices.ProfileData != null)
                        result.Add(manualInputDevices.ProfileData);

                    groupName = manualInputDevices.tbGroupName.Text;

                    break;
                }
            }

            //считываем из базы данных список профилей которые связаны с ПЗ groupName в таблице DEVICES
            if ((groupName != null) && (groupName != Properties.Resources.NotSetted))
                DbRoutines.ProfilesByGroupName(groupName, result, this.AddProfileByGroupName);

            return result;
        }

        private void AddProfileByGroupName(System.Collections.IList listData, string profGUID, int profID, string profName)
        {
            //данную реализацию вызывает DbRoutines.ProfilesByGroupName
            //проверяем наличие описания добавляемого профиля в listData и если в listData уже есть item с profGUID - не добавляем принятое описание профиля в listData

            int count = listData.Cast<ProfileData>().Where(i => i.ProfGUID == profGUID).Count();

            if (count == 0)
            {
                ProfileData profileData = new ProfileData()
                {
                    ProfGUID = profGUID,
                    ProfID = profID,
                    ProfName = profName
                };

                listData.Add(profileData);
            }
        }
    }
}
