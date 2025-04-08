using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using SCME.dbViewer.ForFilters;
using System.Text.RegularExpressions;
using System.Collections;
using SCME.dbViewer.ForSorting;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
using SCME.dbViewer.ForParameters;
using SCME.Types.BaseTestParams;
using SCME.Types.Profiles;
using System.Runtime.Serialization;
using SCME.InterfaceImplementations;
using System.Collections.Concurrent;
using SCME.Types;
using static SCME.dbViewer.Routines;

namespace SCME.dbViewer.CustomControl
{
    /// <summary>
    /// Interaction logic for DataGridSqlResult.xaml
    /// </summary>
    public partial class DataGridSqlResult : DataGrid
    {
        private ConcurrentQueue<Action> queueManager = new ConcurrentQueue<Action>();
        //private Mapper mapper = null;
        private DataGridColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastSortedDirection = ListSortDirection.Ascending;
        private Button btFilterClicked = null;
        private ActiveFilters activeFilters;

        public SqlConnection connection = null;
        public DataTable dtData = null;
        
        /*
        public delegate void UnFrozeMainForm();
        public UnFrozeMainForm UnFrozeMainFormHandler { get; set; }
        
        public delegate void CreateCalculatedFields();
        public CreateCalculatedFields CreateCalculatedFieldsHandler { get; set; }
        public delegate void LoadPCNHandler(SqlConnection connection, object[] values);
        public LoadPCNHandler LoadPCN { get; set; }
        */

        public delegate string GetDeviceType(object[] values);
        public GetDeviceType GetDeviceTypeHandler { get; set; }

        public delegate string GetCode(object[] values);
        public GetCode GetCodeHandler { get; set; }

        public delegate string GetGroupName(object[] values);
        public GetGroupName GetGroupNameHandler { get; set; }

        public delegate string GetProfileName(object[] values);
        public GetProfileName GetProfileNameHandler { get; set; }

        public delegate int? GetDeviceClass(object[] values);
        public GetDeviceClass GetDeviceClassHandler { get; set; }

        public delegate string GetStatus(object[] values);
        public GetStatus GetStatusHandler { get; set; }

        public MainWindow.delegateRefreshBottomRecordCount RefreshBottomRecordCountHandler { get; set; }

        //здесь храним индекс первого столбца набора conditions/patameters первого температурного режима в списке столбцов DataTable
        public int FirstCPColumnIndexInDataTable1 { get; set; }

        //здесь храним индекс последнего столбца набора conditions/patameters первого температурного режима в списке столбцов DataTable
        public int LastCPColumnIndexInDataTable1 { get; set; }

        //число столбцов в DataTable и в DataGrid отличается - в DataGrid нет Hidden столбцов, которые есть в DataTable. индексы столбцов в DataGrid и DataTable не совпадают
        //здесь храним индекс первого столбца набора conditions/patameters первого температурного режима в списке столбцов DataGrid
        public int FirstCPColumnIndexInDataGrid1 { get; set; }

        //здесь храним индекс последнего столбца набора conditions/patameters первого температурного режима в списке столбцов DataGrid
        public int LastCPColumnIndexInDataGrid1 { get; set; }

        public DataGridSqlResult() : base()
        {
            InitializeComponent();

            this.Sorting += new DataGridSortingEventHandler(SortHandler);
            this.activeFilters = new ActiveFilters();

            //применяем пользовательские шрифты
            this.SetCustomerFonts();
        }

        private void SetCustomerFonts()
        {
            IniFileIO iniFile = new IniFileIO();

            string fontName = iniFile.Read("Tables", "Font");
            string size = iniFile.Read("Tables", "Size");

            System.Windows.Media.FontFamily fontFamily;

            if ((fontName != null) && (size != null))
            {
                Style rowStyle = new Style();
                rowStyle.TargetType = typeof(DataGridRow);
                fontFamily = new System.Windows.Media.FontFamily(fontName);
                rowStyle.Setters.Add(new Setter() { Property = DataGridRow.FontFamilyProperty, Value = fontFamily });
                rowStyle.Setters.Add(new Setter() { Property = DataGridRow.FontSizeProperty, Value = Convert.ToDouble(size) });
                this.RowStyle = rowStyle;
            }

            fontName = iniFile.Read("Columns", "Font");
            size = iniFile.Read("Columns", "Size");

            if ((fontName != null) && (size != null))
            {
                Style columnHeaderStyle = FindResource("DataGridColumnHeaderCustomStyle") as Style;
                fontFamily = new System.Windows.Media.FontFamily(fontName);
                columnHeaderStyle.Setters.Add(new Setter() { Property = DataGridRow.FontFamilyProperty, Value = fontFamily });
                columnHeaderStyle.Setters.Add(new Setter() { Property = DataGridRow.FontSizeProperty, Value = Convert.ToDouble(size) });
                this.ColumnHeaderStyle = columnHeaderStyle;
            }
        }

        private bool IsMouseOverCheckBox(CheckBox cb)
        {
            //стандартная реализация cb.IsMouseOver не работает, поэтому делаем свою
            //проверяем, что курсор мыши над принятым cb
            Point mousePosition = Mouse.GetPosition(cb);

            return ((mousePosition.X >= 0) && (mousePosition.X <= cb.ActualWidth) && (mousePosition.Y >= 0) && (mousePosition.Y <= cb.ActualHeight));
        }

        public void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;

            if (cell != null)
            {
                CheckBox cb = cell.Content as CheckBox;

                if (cb != null)
                {
                    if (this.IsMouseOverCheckBox(cb))
                    {
                        //свойство cb.IsChecked сами ни в коем случае не изменяем и не устанавливаем e.Handled=true чтобы корректно сработал механизм Binding от столбца к источнику его данных

                        //чтобы отрабатывал xaml триггер на IsSelected
                        cell.IsSelected = true;

                        //чтобы вычислялось количество ниже стоящих записей
                        cell.Focus();

                        //this.CommitEdit();
                    }

                }
            }
        }

        public void RTStyle()
        {
            this.RowStyle = (Style)FindResource("DataGridRowRT");
        }

        public void TMStyle()
        {
            this.ColumnHeaderStyle = (Style)FindResource("DataGridColumnHeaderCustomTM");
            this.RowStyle = (Style)FindResource("DataGridRowTM");
        }

        private void btFilter_Click(object sender, RoutedEventArgs e)
        {
            //выставляем флаг о прошедшем нажатии кнопки
            btFilterClicked = (Button)sender;
        }

        public void ClearColumns()
        {
            this.Columns.Clear();
            this.FirstCPColumnIndexInDataGrid1 = -1;
        }

        private void ClearDynamicColumns()
        {
            //удаление столбцов, которые были созданы в результате обработки данных из XML описания (полученного в качестве результата выполнения SQL запроса) и столбцов, созданных CreateNotImportantColumns
            //данная реализация вызывается фоновым потоком, поэтому просто так её вызывать нельзя
            queueManager.Enqueue(delegate
                                 {
                                     if (this.FirstCPColumnIndexInDataGrid1 != -1)
                                     {
                                         while ((this.Columns.Count - 1) >= this.FirstCPColumnIndexInDataGrid1)
                                             this.Columns.RemoveAt(this.FirstCPColumnIndexInDataGrid1);

                                         this.FirstCPColumnIndexInDataGrid1 = -1;
                                     }
                                 }
                                );
        }

        private DataGridBoundColumn DataGridBoundColumnByColumnName(string columnNameInDataTable) //DataColumn columnInDataTable
        {
            //вычисление столбца в this по принятому имени столбца
            //string columnNameInDataTable = columnInDataTable.ColumnName;

            var result = this.Columns.Where(x => this.ColumnName((DataGridBoundColumn)x) == columnNameInDataTable).SingleOrDefault();

            return (result == null) ? null : result as DataGridBoundColumn;
        }

        private DataGridBoundColumn LastColumnIndexByTemperatureConditionTestColumnName(TemperatureCondition tc, string test, string columnName)
        {
            //ищет в this.dtData последний созданный столбец у которого температурный режим, тест и header без конечных цифр равны принятым tc, test и header без конечных цифр
            //при этом данный столбец обязан иметь представляющий его в this (DataGrid) столбец
            //именно этот столбец из this возвращается в качестве результата
            string cuttedColumnName = Routines.TrimEndNumbers(columnName);

            DataGridBoundColumn columnOnlyByTemperatureCondition = null;
            DataGridBoundColumn columnByTemperatureConditionAndTest = null;

            //начинаем искать от последнего столбца двигаясь к самому первому столбцу, который хранит самый первый/condition/parameter 
            int endIndex = (this.FirstCPColumnIndexInDataTable1 == -1) ? 0 : this.FirstCPColumnIndexInDataTable1;

            for (int i = this.dtData.Columns.Count - 1; i >= endIndex; i--)
            {
                DataColumn column = this.dtData.Columns[i];

                if (!Routines.IsColumnHidden(column.ColumnName))
                {
                    if (column.ExtendedProperties["TemperatureCondition"].ToString() == tc.ToString())
                    {
                        DataGridBoundColumn result = this.DataGridBoundColumnByColumnName(column.ColumnName);

                        //условие поиска выполнено по температурному режиму
                        if (columnOnlyByTemperatureCondition == null)
                        {
                            if (result != null)
                                columnOnlyByTemperatureCondition = result;
                        }

                        if ((column.ExtendedProperties["Test"].ToString() == test))
                        {
                            //условие поиска выполнено по температурному режиму и тесту
                            if (columnByTemperatureConditionAndTest == null)
                            {
                                if (result != null)
                                    columnByTemperatureConditionAndTest = result;
                            }

                            if (Routines.TrimEndNumbers(column.ColumnName) == cuttedColumnName)
                            {
                                //мы стоим на столбце, который полностью удовлетворяет всем условиям поиска
                                if (result != null)
                                    return result;
                            }
                        }
                    }
                }
            }

            //раз мы здесь - значит искомый столбец не найден
            //вернём последний столбец с температурным режимом tc и тестом test, а если и такой столбец не был найден - вернём последний столбец с температурным режимом tc
            return (columnByTemperatureConditionAndTest == null) ? columnOnlyByTemperatureCondition : columnByTemperatureConditionAndTest;
        }

        private DataGridTextColumn CreateColumn(string header, string bindPath)
        {
            DataGridTextColumn column = new DataGridTextColumn();
            column.Header = header;
            column.IsReadOnly = true;
            column.Binding = new Binding(bindPath);
            this.Columns.Add(column);

            return column;
        }

        private DataGridCheckBoxColumn CreateDataGridCheckBoxColumn(string header, string bindPath)
        {
            DataGridCheckBoxColumn column = new DataGridCheckBoxColumn();
            column.Header = header;
            Binding binding = new Binding(bindPath);
            binding.Mode = BindingMode.TwoWay;
            column.Binding = binding;
            column.IsThreeState = false;
            column.CellStyle = Resources["DataGridCheckBoxColumnStyle"] as Style;
            this.Columns.Add(column);

            return column;
        }

        private DataGridTextColumn CreateCorrectlyPositionedColumn(TemperatureCondition tc, string header, string bindPath, string test)
        {
            //создаёт столбец в this и устанавливает ему индекс так, чтобы столбцы одного теста, одного температурного режима стояли вместе

            //ищем в this последний созданный столбец с температурным режимом tc, тестом test и именем bindPath с вырезаныыми с конца цифрами
            DataGridBoundColumn columnInDataGrid = this.LastColumnIndexByTemperatureConditionTestColumnName(tc, test, bindPath);

            if (columnInDataGrid == null)
            {
                //столбец с принятыми tc и test не найден - создаём новый столбец, он будет первым для температурного режима tc и теста с именем test
                DataGridTextColumn column = this.CreateColumn(header, bindPath);

                return column;
            }
            else
            {
                int maxColumnIndexOfTestByTemperatureCondition = columnInDataGrid.DisplayIndex;

                //создаём новый столбец
                DataGridTextColumn column = this.CreateColumn(header, bindPath);

                //двигаем созданный столбец в списке столбцов DataGrid
                column.DisplayIndex = maxColumnIndexOfTestByTemperatureCondition + 1;

                return column;
            }
        }

        public DataGridColumn NewColumn(TemperatureCondition tc, TemperatureCondition temperatureCondition1, string header, string bindPath, string test)
        {
            //создание нового столбца
            //tc - температурный режим которому должен принадлежать создаваемый в DataGrid столбец
            //temperatureCondition1 - фактический температурный режим 1
            //header - то как пользователь будет видеть название этого столбца
            //bindPath - столбец будет отображать данные столбца binding из доступного списка столбцов
            //test - имя теста, которому принадлежит создаваемый столбец

            DataGridTextColumn column = null;

            if ((tc == TemperatureCondition.None) && (test == null))
            {
                //создаваемый столбец не принадлежит к температурному режиму и тесту - нет смысла управлять установкой DisplayIndex - столбец всегда будет создан самым последним в списке уже созданных столбцов
                column = this.CreateColumn(header, bindPath);
            }
            else
            {
                //столбец принадлежит температурному режиму - устанавливаем ему индекс в списке столбцов так, чтобы столбцы одного теста одного и того же температурного режима стояли вместе
                if (temperatureCondition1 != TemperatureCondition.None)
                {
                    //столбец принадлежит температурному режиму 1 - ставим его самым последним в списке столбцов температурного режима 1. при этом все столбцы, стоящие за ним автоматически пересчитывают свои индексы
                    if (tc == temperatureCondition1)
                    {
                        if (this.FirstCPColumnIndexInDataGrid1 == -1)
                        {
                            //случай создания самого первого столбца в первом температурном режиме
                            column = this.CreateColumn(header, bindPath);

                            this.FirstCPColumnIndexInDataGrid1 = this.Columns.IndexOf(column);
                            this.LastCPColumnIndexInDataGrid1 = this.FirstCPColumnIndexInDataGrid1;
                        }
                        else
                        {
                            //в первом температурном режиме есть хотя-бы один столбец
                            column = this.CreateCorrectlyPositionedColumn(tc, header, bindPath, test);

                            //мы создали столбец в первом температурном режиме - увеличиваем значение счётчика хранящего индекс последнего столбца набора conditions/parameters температурного режима 1
                            this.LastCPColumnIndexInDataGrid1++;
                        }
                    }
                    else
                    {
                        //имеем дело со вторым температурным режимом
                        column = this.CreateCorrectlyPositionedColumn(tc, header, bindPath, test);
                    }
                }
            }

            return column;
        }

        public DataGridCheckBoxColumn NewColumnCheckBox(string header, string bindPath)
        {
            DataGridCheckBoxColumn column = this.CreateDataGridCheckBoxColumn(header, bindPath);

            return column;
        }

        private void SetRowFilter()
        {
            var dv = this.ItemsSource as DataView;

            if (dv != null)
            {
                try
                {
                    dv.RowFilter = this.FiltersToString();
                }
                catch (Exception)
                {
                    MessageBox.Show("Введённое значение фильтра не соответствует типу фильтруемых данных.", "Filter error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    //MessageBox.Show(string.Format("При установке фильтра возникла исключительная ситуация:\r\n{0}", ex.ToString()), "Filter error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }


/*
        private void CreateNotImportantColumns()
        {
            
            //создание не важных для пользователя столбцов - их создание выполняется в самом конце списка уже имеющихся на момент вызова столбцов
            //данная реализация вызывается фоновым потоком, поэтому просто так её вызывать нельзя
            queueManager.Enqueue(
                                  delegate
                                  {
                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.GroupName, Constants.GroupName, null);

                                      MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

                                      //комментарии к изделию можно читать только при наличии такого разрешения
                                      if (mainWindow.NeedReadComments())
                                      {
                                          DataGridColumn deviceCommentsColumn = this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.DeviceComments, Constants.DeviceComments, null);

                                          deviceCommentsColumn.CellStyle = new Style(typeof(DataGridCell), deviceCommentsColumn.CellStyle)
                                          {
                                              Setters = {
                                                          new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left)
                                                        }
                                          };
                                      }

                                      //ширину столбца с датой/временем регистрации изделия в БД делаем так, чтобы было видно только дату - время пользователь смотрит редко и если ему это понадобится - он сам сделает столбец шире
                                      DataGridColumn column = this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.Ts, Constants.Ts, null);
                                      column.Width = 58;
                                      ((DataGridTextColumn)column).Binding.StringFormat = ("dd.MM.yy HH:mm:ss");

                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.DeviceType, Constants.DeviceType, null);
                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.Constructive, Constants.Constructive, null);
                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.AverageCurrent, Constants.AverageCurrent, null);
                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.CodeOfNonMatch, Constants.CodeOfNonMatch, null);
                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.Reason, Constants.Reason, null);
                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.MmeCode, Constants.MmeCode, null);
                                      this.NewColumn(TemperatureCondition.None, TemperatureCondition.None, Properties.Resources.Usr, Constants.Usr, null);
                                  }
                                );
            
        }
*/

        /*
        private void ViewSqlResult(string queryString)
        {
            //отображение результата выполнения запроса с текстом queryString           
            this.connection = DBConnections.Connection;

            this.connection.Open();

            try
            {
                //инициализируем места хранения начального и конечного индексов набора condition/parameters первого температурного режима
                this.FirstCPColumnIndexInDataTable1 = -1;
                this.LastCPColumnIndexInDataTable1 = -1;

                SqlCommand command = new SqlCommand(queryString, this.connection);
                command.CommandTimeout = 1000;
                SqlDataReader reader = command.ExecuteReader();

                DataTable columnsDT = null;
                object[] startRecordValues = null;
                int? devIDFromStartRecord = null;

                try
                {
                    this.dtData = CreateDataTable(reader);

                    //создаём эту DataTable чтобы собрать в ней список всех столбцов с корректными значениями типов для каждого столбца, хранящегося в XML 
                    columnsDT = CreateDataTable(reader);

                    //ищем первую попавшуюся запись, в которой сохранены результаты измерений при RT                    
                    devIDFromStartRecord = this.FindStartRecord(reader, out startRecordValues);
                }

                finally
                {
                    reader.Close();
                }

                this.FillDataTable(columnsDT, devIDFromStartRecord, startRecordValues, command);

                this.CreateNotImportantColumns();
            }

            finally
            {
                this.connection.Close();
            }
        }
        */
        private int ColumnInDataTableByName(DataTable dtTable, string columnName)
        {
            return dtTable.Columns.IndexOf(columnName);
        }

        private TemperatureCondition TemperatureCondition1(TemperatureCondition tcByNewColumn)
        {
            //вычисляет температурный режим 1
            TemperatureCondition result = TemperatureCondition.None;

            if (this.FirstCPColumnIndexInDataTable1 == -1)
            {
                //ещё не создан ни один столбец температурного режима 1 - температурный режим 1 не определён, поэтому он должен быть определён по первому создаваемому столбцу
                result = tcByNewColumn;
            }
            else
            {
                //создан хотя-бы один столбец температурного режима 1 - считываем температурный режим 1
                if (this.dtData != null)
                {
                    DataColumn column = this.dtData.Columns[this.FirstCPColumnIndexInDataTable1];

                    result = ProfileRoutines.TemperatureConditionByProfileName(column.ColumnName);
                }
            }

            return result;
        }

        private int NewColumnInDataTable(DataTable dt, TemperatureCondition tc, string test, TemperatureCondition temperatureCondition1, string columnName, Type columnType, bool unique, bool allowDBNull, bool autoIncrement)
        {
            //создание нового столбца в dtTable
            //в tc принимается температурный режим, которому должен принадлежать создаваемый столбец, в temperatureCondition1 - фактическое значение температурного режима 1            
            //возвращает индекс созданного столбца
            if (dt == null)
                return -1;

            DataColumn column = new DataColumn(columnName, columnType);
            column.Unique = unique;
            column.AllowDBNull = allowDBNull;
            column.AutoIncrement = autoIncrement;
            column.DefaultValue = DBNull.Value;
            column.ExtendedProperties.Add("TemperatureCondition", tc);
            column.ExtendedProperties.Add("Test", test);

            dt.Columns.Add(column);

            //если создан столбец не имеющий принадлежности к температурному режиму (это не видимый в интерфейсе столбец, пример - столбец Pair с именем профиля) - не управляем установкой Ordinal - столбец всегда будет создан самым последним в списке уже созданных столбцов
            //столбец принадлежит температурному режиму - устанавливаем ему индекс в списке столбцов
            if (tc != TemperatureCondition.None)
            {
                //столбец принадлежит температурному режиму
                if (temperatureCondition1 != TemperatureCondition.None)
                {
                    //столбец принадлежит температурному режиму 1 - ставим его самым последним в списке столбцов температурного режима 1. при этом все столбцы, стоящие за ним автоматически пересчитывают свои индексы
                    if (tc == temperatureCondition1)
                    {
                        if (this.FirstCPColumnIndexInDataTable1 == -1)
                        {
                            if (dt == this.dtData)
                            {
                                //данная реализация запускается и для принятого dt=this.dtData и для принятого dt=columnsDT. выставляем счётчики индексов только для случая dt=this.dtData
                                this.FirstCPColumnIndexInDataTable1 = dt.Columns.IndexOf(column);
                                this.LastCPColumnIndexInDataTable1 = this.FirstCPColumnIndexInDataTable1;
                            }
                        }
                        else
                        {
                            //раз мы создали новый столбец в наборе conditions/parameters температурного режима 1 - увеличиваем значение счётчика хранящего индекс последнего столбца набора conditions/parameters температурного режима 1
                            if (dt == this.dtData)
                            {
                                //данная реализация запускается и для принятого dt=this.dtData и для принятого dt=columnsDT. выставляем счётчики индексов только для случая dt=this.dtData
                                this.LastCPColumnIndexInDataTable1++;
                            }

                            column.SetOrdinal(this.LastCPColumnIndexInDataTable1);
                        }
                    }
                }
            }

            return dt.Columns.IndexOf(column);
        }

        private void NewColumnInDataGrid(TemperatureCondition tc, TemperatureCondition temperatureCondition1, string nameInDataTable, string nameInDataGrid, string test)
        {
            //создание нового столбца в this
            queueManager.Enqueue(() => this.NewColumn(tc, temperatureCondition1, nameInDataGrid, nameInDataTable, test));
        }

        private DataTable CreateDataTable(SqlDataReader reader)
        {
            DataTable result = null;

            DataTable dtSchema = reader.GetSchemaTable();

            if (dtSchema != null)
            {
                result = new DataTable();

                //копируем все столбцы из dtSchema
                foreach (DataRow row in dtSchema.Rows)
                {
                    string nameInDataTable = System.Convert.ToString(row["ColumnName"]);

                    //смотрим какому температурному режиму принадлежит создаваемый столбец
                    TemperatureCondition tc = ProfileRoutines.TemperatureConditionByProfileName(nameInDataTable);

                    //смотрим с каким температурным режимом 1 мы имеем дело (может быть как RT, так и TM в зависимости от того какой встретится первым)
                    TemperatureCondition temperatureCondition1 = this.TemperatureCondition1(tc);

                    NewColumnInDataTable(result, tc, null, temperatureCondition1, nameInDataTable, (Type)(row["DataType"]), (bool)row["IsUnique"], (bool)row["AllowDBNull"], (bool)row["IsAutoIncrement"]);
                }
            }

            return result;
        }

        private DataTable SetTypeForData(DataTable table, DataTable columnsDT)
        {
            //изменить тип столбца для уже имеющихся в DataTable данных нельзя, но можно перелить эти же данные в пустую DataTable с нужными типами столбцов
            //именно это и делает данная реализация
            if (table.Columns.Count != columnsDT.Columns.Count)
                throw new Exception(string.Format("table.Columns.Count={0}, columnsDT.Columns.Count={1}. Ожидалось их равенство.", table.Columns.Count, columnsDT.Columns.Count));

            if (columnsDT.Rows.Count != 0)
                throw new Exception(string.Format("columnsDT.Rows.Count={0}. Ожидалось columnsDT.Rows.Count=0.", columnsDT.Rows.Count));

            columnsDT.MinimumCapacity = table.Rows.Count;

            foreach (DataRow row in table.Rows)
                columnsDT.ImportRow(row);

            return columnsDT;
        }
        /*
        private int? FindStartRecord(SqlDataReader reader, out object[] values)
        {
            //ищет в принятом reader первую попавшуюся запись с температурным режимом измерения RT и в качестве результата возвращает значение поля DevID. если же во множестве записей reader нет ни одной с температурным режимом измерения RT (например из-за фильтров) - возвращает null

            //нужда в такой записи возникает по следуюшим причинам:
            //1. технологи хотят чтобы первым температурном режимом был именно RT (а не TM) - им так удобно;
            //2. столбцы DataTable dtData, хранящие имена conditions/parameters формируются динамически при чтении данных;
            //3. первая прочитанная запись из reader определяет каким будет температурный режим 1: если первая просмотренная запись будет нести набор RT conditions/parameters - то и первый температурный режим будет RT. если первая просмотренная запись будет нести набор TM conditions/parameters - то и первый температурный режим будет TM (см. реализацию NewColumnInDataTable)
            //4. казалось бы можно просто выполнить сортировку набора данных возвращаемых SQL сервером по наименованию профиля. при этом обход данных в reader был бы таким как нам надо - сначала записи с температурным режимом RT, потом с TM. но технологи частенько нарушают свои же правила по формированию имён профилей и вытащить из имени профиля обозначение температурного режима нельзя. пример такого профиля '0.332A.х.024.016.0290.TR211.x'. поэтому от использования сортировки отказался

            values = new object[reader.FieldCount];

            //XML полей в каждой записи два: сначала столбец с conditions, за ним столбец с parameters
            int indexOfXMLColumnConditions = this.dtData.Columns.IndexOf(Constants.XMLConditions);

            TemperatureCondition temperatureCondition = TemperatureCondition.None;
            double temperatureValue;

            XmlDocument xmlDoc = new XmlDocument();

            while (reader.Read())
            {
                reader.GetValues(values);
                object value = values[indexOfXMLColumnConditions];

                //нам надо прочитать описание температурного режима, оно есть в conditions, формат хранения XML
                //убеждаемся, что value в формате XML
                string sXML = ValueIsXml(value);

                if (sXML != null)
                {
                    xmlDoc.LoadXml(sXML);
                    XmlElement documentElement = xmlDoc.DocumentElement;

                    XMLValues subject = (documentElement.Name == "CONDITIONS") ? XMLValues.Conditions : (documentElement.Name == "PARAMETERS") ? XMLValues.Parameters : XMLValues.UnAssigned;

                    //считываем при какой температуре проводятся измерения. эта информация есть только в описании условий измерения                        
                    if (subject == XMLValues.Conditions)
                    {
                        temperatureValue = TemperatureConditionFromXML(xmlDoc);
                        temperatureCondition = Routines.TemperatureConditionByTemperature(temperatureValue);

                        if (temperatureCondition == TemperatureCondition.RT)
                        {
                            //мы нашли запись с температурным режимом RT - извлекаем идентификатор DevID
                            int indexOfDevID = this.dtData.Columns.IndexOf(Constants.DevID);

                            return values[indexOfDevID] as int?;
                        }
                    }
                }
            }

            //мы просмотрели всё множество записей и не нашли нужную
            values = null;
            return null;
        }
        */
        /*
        public void ViewSqlResultByThread(string queryString)
        {
            //выполнение реализации this.ViewSqlResultHandler в фоновом потоке, после выполнения которой система исполнит реализацию this.AfterFillDataTableHandler в главном потоке
            LongTimeRoutineWorker Worker = new LongTimeRoutineWorker(this.ViewSqlResultHandler, this.AfterFillDataTableHandler);

            Object[] args = { queryString };
            Worker.Run(args);
        }

        private void ViewSqlResultHandler(DoWorkEventArgs e)
        {
            //извлекаем из принятого workerParameters параметры, необходимые для вызова this.ViewSqlResult
            Object[] arg = e.Argument as Object[];
            string queryString = (string)arg[0];

            this.ViewSqlResult(queryString);
        }
        */
        private void AfterFillDataTableHandler(string error)
        {
            //проверяем не завершилось ли исполнение потоковой функции ошибкой
            if (error == string.Empty)
            {
                //потоковая функция исполнена успешно
                this.SetItemsSource(this.dtData);

                //обрабатываем отложенную очередь вызовов
                while (queueManager.TryDequeue(out Action act))
                    act.Invoke();

                //загрузка данных завершилась - разблокируем форму
                //this.UnFrozeMainFormHandler();
            }
            else MessageBox.Show(error, Properties.Resources.LoadDataFromDataBaseFault, MessageBoxButton.OK, MessageBoxImage.Exclamation);

        }
        /*
        private void FillDataTable(DataTable columnsDT, int? devIDFromStartRecord, object[] startRecordValues, SqlCommand command)
        {
            this.ClearDynamicColumns();

            //создаём кеш для выполнения группировки данных (для объединения одинаково идентифицируемых данных в единой строке this.dtData)
            this.mapper = new Mapper();

            //если запись найдена - обрабатываем её
            if (devIDFromStartRecord != null)
                this.FillValues(columnsDT, startRecordValues, this.dtData);

            //создаём новый reader только потому, что при выполнении this.FindStartRecord мы смещаемся во множестве записей (выполняя reader.Read()) без возможности вернутся к начальной записи 
            //создавая новый reader мы просматриваем всё множество записей, в том числе и найденную StartRecord
            SqlDataReader reader = command.ExecuteReader();
            object[] values = new object[reader.FieldCount];

            while (reader.Read())
            {
                reader.GetValues(values);
                bool needProcessing = true;

                if (devIDFromStartRecord != null)
                {
                    //смотрим на идентификатор DevID текущей записи, чтобы не допустить повторной обработки уже обработанной записи StartRecord
                    int indexOfDevID = this.dtData.Columns.IndexOf(Constants.DevID);
                    int currentDevID = (int)values[indexOfDevID];

                    needProcessing = (currentDevID != devIDFromStartRecord);
                }

                if (needProcessing)
                    this.FillValues(columnsDT, values, this.dtData);
            }

            //все данные обработаны, проходим по записям, которые не приняли в себя данных от других записей и устанавливаем им значения реквизитов DeviceClass и Status равным DbNull
            this.mapper.SetDeviceClassAndStatusToDbNull();
            this.mapper = null;

            //в columnsDT собран список столбцов с корректными типами столбцов. данный список столбцов появился в результате обработки данных в XML полях, полученных от SQL сервера. выполняем типизацию хранящихся в this.dtData значений начиная со столбца с индексом lastColumnIndex в соответствии с вычисленными типами столбцов
            this.dtData = this.SetTypeForData(this.dtData, columnsDT);
        }
        */
        /*
        public void ViewSqlResultAsync(string SqlQuery)
        {
            SqlConnection connection = CreateConnection();
            connection.Open();

            SqlCommand cmd = new SqlCommand(SqlQuery, connection);

            cmd.BeginExecuteReader(new AsyncCallback(CallbackFunction), cmd);
        }

        private void CallbackFunction(IAsyncResult result)
        {
            SqlCommand cmd = (SqlCommand)result.AsyncState;
            SqlDataReader reader = cmd.EndExecuteReader(result);
            DataTable dtSchema = reader.GetSchemaTable();

            if (dtSchema != null)
            {
                this.dtData = new DataTable();

                foreach (DataRow row in dtSchema.Rows)
                {
                    string columnName = System.Convert.ToString(row["ColumnName"]);
                    DataColumn column = new DataColumn(columnName, (Type)(row["DataType"]));
                    column.Unique = (bool)row["IsUnique"];
                    column.AllowDBNull = (bool)row["AllowDBNull"];
                    column.AutoIncrement = (bool)row["IsAutoIncrement"];
                    dtData.Columns.Add(column);
                }

                this.CreateCalculatedFieldsHandler();

                Dispatcher.BeginInvoke(new delegateSetItemsSource(SetItemsSource), dtData);

                while (reader.Read())
                {
                    object[] values = new object[reader.FieldCount];
                    reader.GetValues(values);

                    Dispatcher.BeginInvoke(new delegateFillValues(FillValues), values, dtData);

                    LoadPCN(this.connection, values);
                }

                reader.Close();

                this.SetRowFilter();

                if (this.Items.Count > 0)
                    this.SelectedIndex = 0;
            }

            //if (cmd.Connection.State.Equals(ConnectionState.Open))
            //    cmd.Connection.Close();
        }
        */

        private Type TypeOfValue(string value, out string correctedValue)
        {
            //вычисляет тип принятого value по принципу: всё что не double - то string
            //не будем ограничивать пользователя в выборе системного разделителя дробной части от целой части - вернём в correctedValue корректное значение для любого значения системного разделителя дробной части от целой части
            double d;
            bool parsedAsDouble = double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out d);
            correctedValue = parsedAsDouble ? d.ToString() : value;

            return parsedAsDouble ? typeof(double) : typeof(string);
        }

        private bool ValueAsDouble(string value, out double correctedValue)
        {
            return double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out correctedValue);
        }

        private void StoreType(DataTable columnsDT, int columnIndex, Type columnDataType)
        {
            //выполняет сравнение принятого columnDataType с сохранённым ранее типом для столбца с индексом columnIndex. если в столбце columnIndex будет хотя бы одно значение с типом string - то тип всего столбца должент быть string
            DataColumn column = columnsDT.Columns[columnIndex];

            if (column != null)
            {
                Type storedDataType = column.DataType;

                if ((storedDataType != typeof(string)) && (columnDataType == typeof(string)))
                    column.DataType = columnDataType;
            }
        }

        private double TemperatureConditionFromXML(XmlDocument xmlConditionDoc)
        {
            //извлечение значения условия с именем 'CLAMP_Temperature' теста 'Clamping' из XML описания условий xmlConditionDoc
            XmlNode node = xmlConditionDoc.SelectSingleNode("//T[@Test='Clamping' and @Name='CLAMP_Temperature']");

            if (node == null)
            {
                //если в профиле нет описания температуры - значит все измерения по этому профилю проводятся при комнатной температуре (охладитель в столике КИПП СПП отсутствует, есть только нагреватель)
                //будем возвращать -1 чтобы показать ошибку в указании температуры
                return -1;
            }
            else
            {
                double temperatureValue;
                if (double.TryParse(node.Attributes["Value"].Value, out temperatureValue))
                {
                    return temperatureValue;
                }

                //в профиле значение температуры указано с ошибкой. прерывать выполнение и ругаться не будем, будем считать, что в профиле указана комнатная температура
                //будем возвращать -1 чтобы показать ошибку в указании температуры
                return -1;
            }
        }

        /*
        private TemperatureCondition TemperatureConditionFromColumnName(string columnName)
        {
            //извлекает значение температурного режима из принятого обозначения столбца columnName
            int endIndex = columnName.IndexOf(Constants.cStringDelimeter);

            if (endIndex > 0)
            {
                string sTC = columnName.Substring(0, endIndex).ToString();

                TemperatureCondition tc;
                return (Enum.TryParse(sTC, true, out tc) && (Enum.IsDefined(typeof(TemperatureCondition), tc))) ? tc : TemperatureCondition.None;
            }

            return TemperatureCondition.None;
        }
        */

        private int GetColumnIndexInDataTable(string nameInDataTable, string test, string nameInDataGrid, Type newColumnDataType, Type factColumnDataType, DataTable columnsDT, bool columnVisual)
        {
            int columnIndex = ColumnInDataTableByName(this.dtData, nameInDataTable);

            //столбец с именем name отсутствует в this.dtData - его надо создать
            if (columnIndex == -1)
            {
                //смотрим какому температурному режиму принадлежит создаваемый столбец
                TemperatureCondition tc = ProfileRoutines.TemperatureConditionByProfileName(nameInDataTable);

                TemperatureCondition temperatureCondition1 = TemperatureCondition.None;
                //смотрим с каким температурным режимом 1 мы имеем дело (может быть как RT, так и TM в зависимости от того какой встретится первым при загрузке данных)
                if (tc != TemperatureCondition.None)
                    temperatureCondition1 = this.TemperatureCondition1(tc);

                //создаём столбец в this.dtData всегда с типом columnDataType. создать сразу столбец с нужным типом можно только для случая чтения значений измеренных параметров, для случая conditions так делать нельзя т.к. нам не известны типы всех значений (они объявлены как string, но хранится в них могут как строки так и числа с плавающей запятой)
                NewColumnInDataTable(this.dtData, tc, test, temperatureCondition1, nameInDataTable, newColumnDataType, false, true, false);

                //создаём столбец в columnsDT с фактическим типом factColumnDataType. индекс созданного столбца в this.dtData и columnsDT одинаковый
                columnIndex = NewColumnInDataTable(columnsDT, tc, test, temperatureCondition1, nameInDataTable, factColumnDataType, false, true, false);

                //создаём столбец в DataGrid
                if (columnVisual)
                    NewColumnInDataGrid(tc, temperatureCondition1, nameInDataTable, nameInDataGrid, test);
            }

            //запоминаем/проверяем вычисленный тип значения value в columnsDT
            StoreType(columnsDT, columnIndex, factColumnDataType);

            return columnIndex;
        }

        private string CommentFromXML(XmlAttributeCollection commentAttributes)
        {
            //извлечение одного комментария из принятого commentAttributes
            string result = string.Empty;

            if (commentAttributes["RECORDDATE"] != null)
                result = Convert.ToDateTime(commentAttributes["RECORDDATE"].Value).ToString("dd.MM.yyyy");

            if (commentAttributes["USERID"] != null)
            {
                //считываем по идентификатору пользователя его полное имя
                string fullUserName = string.Empty;

                if (DbRoutines.FullUserNameByUserID(Convert.ToInt64(commentAttributes["USERID"].Value), out fullUserName))
                {
                    if ((fullUserName != null) && (fullUserName != string.Empty))
                    {
                        if (result != string.Empty)
                            result += " ";

                        result += fullUserName;
                    }
                }
            }

            if (commentAttributes["COMMENTS"] != null)
            {
                string comment = Convert.ToString(commentAttributes["COMMENTS"].Value);

                if ((result != null) && (comment != string.Empty))
                    result += " ";

                result += comment;
            }

            return result;
        }

        private string ColumnNameInDataGrid(string temperatureCondition, string test, string name)
        {
            //строит имя параметра для использования в DataGrid по принятым temperatureCondition, test и name
            //построение имени параметра с учётом его принадлежности к температурному режиму
            return string.Format("{0}/{1}{2}{3}", temperatureCondition, test, Constants.StringDelimeter, name);
        }

        private string ColumnNameInDataTable(string temperatureCondition, string test, string name)
        {
            //строит имя параметра для использования в DataTable по принятым temperatureCondition и name
            return string.Format("{0}{1}{2}{3}", temperatureCondition, test, SCME.Common.Constants.FromXMLNameSeparator, name);
        }

        private string CalcAlternativeColumnName(string columnName, DataTable dataTable, DataRow dataRow, out int columnNameCount)
        {
            //вычисление альтернативного имени столбца по принятому имени столбца columnName в dataTable
            //возвращает уникальное имя столбца в dataTable вида columnName + columnNameCount
            //в columnNameCount возвращает число повторений столбца с именем columnName в dataTable увеличенное на 1
            if ((dataTable != null) && (columnName != null) && (columnName != string.Empty))
            {
                columnNameCount = 1;
                string result = null;
                bool founded = false;

                while (!founded)
                {
                    columnNameCount++;
                    result = string.Concat(columnName, columnNameCount);

                    if (dataTable.Columns.IndexOf(result) == -1)
                    {
                        //вычисленное имя столбца отсутствует в списке существующих столбцов
                        founded = false;
                        break;
                    }
                    else
                        founded = (dataRow[result] == DBNull.Value);
                }

                if (founded)
                {
                    //мы нашли существующее и не знятое место хранения
                    return result;
                }
                else
                {
                    //все ранее созданные места хранения в dataRow просмотрены, но свободного места нет - надо сгененрировать новое имя столбца
                    return string.Concat(columnName, columnNameCount);
                }
            }

            //раз мы здесь - найти ничего не удалось
            columnNameCount = -1;

            return null;
        }

        /*
        private void ProcessingXmlData(DataTable columnsDT, XmlElement documentElement, Common.Routines.XMLValues subject, string tc, double temperatureValue, string deviceType, DataRow dataRow)
        {
            //анализирует принятый текст xmlValue (который точно является XML текстом)
            //создаём столбцы в this и this.dtData из принятого описания в XML формате
            string comments = null;

            foreach (XmlNode child in documentElement.ChildNodes)
            {
                XmlAttributeCollection attributes = child.Attributes;

                if (attributes != null)
                {
                    if (attributes["Test"] == null)
                    {
                        //это не conditions/parameters, т.к. атрибут Test отсутствует
                        //если мы имеем дело с комментариями к изделию - читаем эти комментарии
                        if (subject == Common.Routines.XMLValues.DeviceComments)
                        {
                            string comment = this.CommentFromXML(attributes);

                            if (comments != null)
                                comments += " ";

                            comments += comment;
                        }
                    }
                    else
                    {
                        //атрибут "Test" точно существует, имеем дело с conditions/parameters
                        string test = attributes["Test"].Value;

                        //поправляем значение test из-за не соответствия в базе данных и в коде
                        test = Routines.CorrectTestTypeName(test);

                        TestParametersType testType;

                        //перечисление TestParametersType никогда ни при каких условиях не может содержать тип теста Manually (вручную созданные параметры)
                        if (
                            (Enum.TryParse(test, true, out testType)) ||
                            ((test == DbRoutines.cManually) && (subject == Common.Routines.XMLValues.Parameters))
                           )
                        {
                            bool need = false;

                            string name = null;
                            string value = null;
                            string unitMeasure = null;

                            string columnNameInDataGrid = string.Empty;
                            string columnNameInDataTable = string.Empty;

                            Type factColumnDataType = null;
                            Type newColumnDataType = null;

                            double? dnrmMin = null;
                            double? dnrmMax = null;

                            if (attributes["Name"] != null)
                            {
                                name = attributes["Name"].Value;

                                if (name != string.Empty)
                                {
                                    switch (subject)
                                    {
                                        case (Common.Routines.XMLValues.Conditions):
                                            //читаем список условий, которые надо показать
                                            TemperatureCondition temperatureCondition = Routines.TemperatureConditionByTemperature(temperatureValue);
                                            List<string> conditions = ConditionNamesByDeviceType(testType, deviceType, temperatureCondition);

                                            //если conditions=null - не показываем данное условие. если же conditions не null, то в нём должно присутствовать условие с именем name
                                            need = ((conditions != null) && (conditions.IndexOf(name) != -1) && (attributes["Value"] != null));

                                            if (need)
                                            {
                                                //описания условий хранятся как строки, в них может быть всё что угодно                                    
                                                value = attributes["Value"].Value;

                                                newColumnDataType = typeof(string);
                                                factColumnDataType = TypeOfValue(value, out value);

                                                //строим имя условия для отображения в DataGrid
                                                test = Routines.TestNameInDataGridColumn(test);
                                                columnNameInDataGrid = this.ColumnNameInDataGrid(tc, test, Dictionaries.ConditionName(temperatureCondition, name));

                                                //строим имя условия для использования в DataTable
                                                columnNameInDataTable = this.ColumnNameInDataTable(tc, test, Dictionaries.ConditionName(temperatureCondition, name));
                                            }

                                            break;

                                        case (Common.Routines.XMLValues.Parameters):
                                            //строим список измеренных параметров, которые надо показать
                                            List<string> measuredParameters = Routines.MeasuredParametersByTestType(testType);

                                            //проверяем вхождение имени текущего измеренного параметра в список измеренных параметров, которые требуется показать для текущего типа теста. если measuredParameters=null - значит надо показать их все
                                            need = ((measuredParameters == null) || (measuredParameters.IndexOf(name) != -1) && (attributes["Value"] != null));

                                            if (need)
                                            {
                                                value = attributes["Value"].Value;

                                                //значения измеренных параметров - всегда числа с плавающей запятой. если это не так - ругаемся
                                                double dValue;
                                                if (!ValueAsDouble(value, out dValue))
                                                    throw new Exception(string.Format("При чтении значения измеренного параметра '{0}' из XML описания оказалось, что его значение '{1}' не преобразуется к типу Double.", name, value));

                                                value = dValue.ToString();
                                                newColumnDataType = typeof(double);
                                                factColumnDataType = newColumnDataType;

                                                //атрибут "TemperatureCondition" может быть только у параметров, которые созданы пользователем
                                                string tC = (test == DbRoutines.cManually) ? attributes["TemperatureCondition"].Value : tc;

                                                //строим имя параметра для отображения в DataGrid
                                                test = Routines.TestNameInDataGridColumn(test);
                                                columnNameInDataGrid = this.ColumnNameInDataGrid(tC, test, Dictionaries.ParameterName(name));

                                                //строим имя параметра для использования в DataTable
                                                columnNameInDataTable = this.ColumnNameInDataTable(tC, test, Dictionaries.ParameterName(name));

                                                //считываем единицу измерения
                                                unitMeasure = (attributes["Um"] == null) ? string.Empty : attributes["Um"].Value;

                                                //считываем значения норм
                                                XmlAttribute attribute = attributes["NrmMin"];
                                                if (attribute != null)
                                                {
                                                    string nrmMin = attribute.Value;
                                                    //значения норм - всегда числа с плавающей запятой. если это не так - ругаемся
                                                    if (!ValueAsDouble(nrmMin, out dValue))
                                                        throw new Exception(string.Format("При чтении значения нормы (min) измеренного параметра '{0}' из XML описания оказалось, что оно '{1}' не преобразуется к типу Double.", name, nrmMin));

                                                    dnrmMin = dValue;
                                                }

                                                attribute = attributes["NrmMax"];
                                                if (attribute != null)
                                                {
                                                    string nrmMax = attribute.Value;
                                                    //значения норм - всегда числа с плавающей запятой. если это не так - ругаемся
                                                    if (!ValueAsDouble(nrmMax, out dValue))
                                                        throw new Exception(string.Format("При чтении значения нормы (max) измеренного параметра '{0}' из XML описания оказалось, что оно '{1}' не преобразуется к типу Double.", name, nrmMax));

                                                    dnrmMax = dValue;
                                                }
                                            }

                                            break;

                                        default:
                                            throw new Exception(string.Format("Для принятого значения subject={0} обработка не предусмотрена.", subject.ToString()));
                                    }
                                }
                            }

                            if (need)
                            {
                                //запоминаем значение condition/parameter
                                int columnIndex = GetColumnIndexInDataTable(columnNameInDataTable, test, columnNameInDataGrid, newColumnDataType, factColumnDataType, columnsDT, true);

                                if (dataRow[columnIndex] != DBNull.Value)
                                {
                                    //в записи dataRow место хранения с индексом columnIndex занято - в dataRow заливаются данные из нескольких записей, считанных из БД. имена параметров/условий в этих записях могут совпадать
                                    //чтобы создать новое место хранения - будем использовать другой столбец - допишем к имени столбца число
                                    int columnNameCount;
                                    columnNameInDataTable = this.CalcAlternativeColumnName(columnNameInDataTable, this.dtData, dataRow, out columnNameCount);
                                    columnNameInDataGrid = string.Concat(columnNameInDataGrid, columnNameCount);

                                    columnIndex = GetColumnIndexInDataTable(columnNameInDataTable, test, columnNameInDataGrid, newColumnDataType, factColumnDataType, columnsDT, true);
                                }

                                dataRow[columnIndex] = value;

                                //запоминаем значение температуры                           
                                name = Routines.NameOfHiddenColumn(columnNameInDataTable);
                                int temperatureValuecolumnIndex = GetColumnIndexInDataTable(name, test, null, typeof(string), typeof(string), columnsDT, false);
                                dataRow[temperatureValuecolumnIndex] = string.Format("{0} °C", temperatureValue);

                                //запоминаем значение единицы измерения
                                name = Routines.NameOfUnitMeasure(columnNameInDataTable);
                                int unitMeasureIndex = GetColumnIndexInDataTable(name, test, null, typeof(string), typeof(string), columnsDT, false);
                                dataRow[unitMeasureIndex] = (unitMeasure == null) ? string.Empty : unitMeasure;

                                //запоминаем значения норм min и max
                                if (dnrmMin != null)
                                {
                                    name = Routines.NameOfNrmMinParametersColumn(columnNameInDataTable);
                                    int nrmMinColumnIndex = GetColumnIndexInDataTable(name, test, null, typeof(double), typeof(double), columnsDT, false);
                                    dataRow[nrmMinColumnIndex] = (double)dnrmMin;
                                }

                                if (dnrmMax != null)
                                {
                                    name = Routines.NameOfNrmMaxParametersColumn(columnNameInDataTable);
                                    int nrmMaxColumnIndex = GetColumnIndexInDataTable(name, test, null, typeof(double), typeof(double), columnsDT, false);
                                    dataRow[nrmMaxColumnIndex] = (double)dnrmMax;
                                }
                            }
                        }
                    }
                }

                if (comments != null)
                {
                    int columnIndex = ColumnInDataTableByName(this.dtData, Constants.DeviceComments);
                    dataRow[columnIndex] = comments;
                }
            }
        }
        */

        private string ValueIsXml(object value)
        {
            //отвечает на вопрос является ли принятый value Xml текстом
            //возвращает:
            //            null - принятый value не Xml текст
            //            not null - принятый value есть Xml текст
            string result = null;

            if (value != null)
            {
                string sValue = value.ToString();

                if (sValue != string.Empty)
                {
                    if (sValue.TrimStart().StartsWith("<"))
                        result = sValue;
                }
            }

            return result;
        }

        private int? MaxFromListOfValues(string listOfValues, string separatorForValues)
        {
            //принимает на вход строку listOfValues в которой через разделитель separatorForValues перечислены int значения и вычисляет по ним максимальное значение
            //возвращает:
            //           null - по принятому listOfValues нельзя вычислить максимальное значение класса
            //           максимальное int значение из принятого listOfValues
            switch ((listOfValues == null) || (listOfValues == string.Empty))
            {
                case true:
                    //по принятому listOfValues нельзя вычислить максимальное значение класса
                    return null;

                default:
                    //рассматриваем listOfValues как список значений int и вычисляем по нему максимальное значение
                    List<int> list = listOfValues.Split(new string[] { separatorForValues }, StringSplitOptions.None).Select(i => int.Parse(i)).ToList();

                    return list.Max();
            }
        }

        private object CalcStatus(string statusHistory, string separatorForValues)
        {
            //вычисление итогового статуса по истории изменения статуса statusHistory
            //statusHistory это все значения статусов из принимающей записи и из всех просмотренных Pair записей, записанных в строку через разделитель separatorForValues
            //если хотя-бы одно измерение отсутствует - итоговый статус имеет неопределённое (пустое) значение
            //если пара RT-TM не образовалась - итоговый статус имеет неопределённое (пустое) значение
            //итоговый статус будет "OK" только если пара RT-TM образована и все статусы, сохранённые в statusHistory есть "OK"
            //если все статусы имеют не пустые значения и хотя-бы один из статусов не "OK" - итоговый статус есть "Fault"
            List<string> list = statusHistory.ToString().Split(new string[] { separatorForValues }, StringSplitOptions.None).Select(s => s).ToList();
            foreach (string status in list)
            {
                if (status == string.Empty)
                {
                    //один из статусов в statusHistory не определённый
                    return string.Empty;
                }
                else
                {
                    if (status.Contains(Constants.FaultSatatus))
                    {
                        //одного не успешного статуса достаточно для вычисления итогового статуса в Fault
                        return Constants.FaultSatatus;
                    }
                    else
                    {
                        if (!status.Contains(Constants.GoodSatatus))
                            return string.Empty;
                    }
                }
            }

            //мы проверили все элементы списка list
            return Constants.GoodSatatus;
        }

        private string AddDeviceClassToList(int? deviceClass, string listDeviceClass, string separatorForValues)
        {
            //добавление значения deviceClass в список listDeviceClass
            string result = listDeviceClass;

            if ((deviceClass != null) && (result != null))
            {
                if (result != string.Empty)
                    result = string.Concat(result, separatorForValues);

                result = string.Concat(result, deviceClass.ToString());
            }

            return result;
        }

        private string listDeviceClassByTemperatureCondition(TemperatureCondition temperatureCondition, string rtListDeviceClass, int rtColumnIndex, string tmListDeviceClass, int tmColumnIndex, out int columnIndexListOfDeviceClass)
        {
            switch (temperatureCondition)
            {
                case TemperatureCondition.RT:
                    //обработка значения класса при RT
                    columnIndexListOfDeviceClass = rtColumnIndex;

                    return rtListDeviceClass;

                case TemperatureCondition.TM:
                    //обработка заначения класса для случая TM
                    columnIndexListOfDeviceClass = tmColumnIndex;

                    return tmListDeviceClass;
            }

            //для вычисления класса используются данные, полученные только при температурных режимах RT, TM
            //другие температурные режимы не используются
            columnIndexListOfDeviceClass = -1;
            return null;
        }

        /*
        private void ProcessingNotXmlPairData(int index, DataRow destRow, TemperatureCondition pairTemperatureCondition, object pairValue, DataTable columnsDT)
        {
            //запоминание pairValue в записи destRow по индексу index
            //для этого создаём не видимый в this столбец, чтение сохранённых в него данных будет реализовано через механизм hint's. тип создаваемого столбца - строка ибо для tooltip другого и не надо
            TemperatureCondition temperatureConditionDestRow;

            const string separatorForValues = ", ";
            string columnName = destRow.Table.Columns[index].ColumnName;

            switch (columnName)
            {
                case Constants.DeviceClass:
                    //при вычислении класса игнорируем значения NULL не зависимо от тестов, от которых они получены
                    //для вычисления класса создаём сразу 2-а скрытых столбца: для хранения списка значений класса при комнатной температуре (RT) и для хранения списка значений класса при максимальной температуре (TM). другие температурные режимы при вычислении класса нас не интересуют в принципе
                    string rtColumnNameDeviceClass = string.Concat(Constants.RT, Routines.NameOfHiddenColumn(columnName));
                    int rtColumnIndex = GetColumnIndexInDataTable(rtColumnNameDeviceClass, null, null, typeof(string), typeof(string), columnsDT, false);

                    string tmColumnNameDeviceClass = string.Concat(Constants.TM, Routines.NameOfHiddenColumn(columnName));
                    int tmColumnIndex = GetColumnIndexInDataTable(tmColumnNameDeviceClass, null, null, typeof(string), typeof(string), columnsDT, false);

                    //смотрим на температурный режим destRow
                    temperatureConditionDestRow = ProfileRoutines.TemperatureConditionByProfileName(this.GetProfileNameHandler(destRow.ItemArray));

                    int? destRowDeviceClass = this.GetDeviceClassHandler(destRow.ItemArray);

                    if (destRowDeviceClass != null)
                    {
                        //нам надо выполнить сохранение значения класса хранящегося в destRow только один раз, а не каждый раз, когда вызывается данная реализация
                        //определяемся со списком куда надо сохранить класс изделия хранящийся в записи destRow.
                        //записи с темепературными режимами, отличными от RT, TM в расчёте класса не используем - игнорируем их
                        switch (temperatureConditionDestRow)
                        {
                            case TemperatureCondition.RT:
                                if (destRow[rtColumnIndex] == DBNull.Value)
                                    destRow[rtColumnIndex] = destRowDeviceClass.ToString();

                                break;

                            case TemperatureCondition.TM:
                                if (destRow[tmColumnIndex] == DBNull.Value)
                                    destRow[tmColumnIndex] = destRowDeviceClass.ToString();

                                break;
                        }
                    }

                    if (pairValue != DBNull.Value)
                    {
                        int? pairClass = int.Parse(pairValue.ToString());
                        string listDeviceClass = null;
                        int deviceClassColumnIndex = -1;

                        //смотрим с каким температурным режимом (либо RT либо TM, другие не важны) мы имеем дело и считываем значение из соответствующего списка
                        listDeviceClass = this.listDeviceClassByTemperatureCondition(pairTemperatureCondition, destRow[rtColumnIndex].ToString(), rtColumnIndex, destRow[tmColumnIndex].ToString(), tmColumnIndex, out deviceClassColumnIndex);

                        if ((listDeviceClass != null) && (deviceClassColumnIndex != -1))
                        {
                            //добавляем в считанный список значение класса из добавляемой записи
                            listDeviceClass = this.AddDeviceClassToList(pairClass, listDeviceClass, separatorForValues);
                            destRow[deviceClassColumnIndex] = listDeviceClass;
                        }

                        //вычисляем итоговое значение класса. при этом используем сформированный на данный момент список классов RT, и список классов TM
                        //по списку классов одного и того же температурного режима вычисляем максимум
                        int? rtDeviceClassMax = this.MaxFromListOfValues(destRow[rtColumnIndex].ToString(), separatorForValues);
                        int? tmDeviceClassMax = this.MaxFromListOfValues(destRow[tmColumnIndex].ToString(), separatorForValues);

                        //итоговый класс есть минимум из двух значений: maxRT и maxTM
                        int? deviceClass = Routines.MinDeviceClass(rtDeviceClassMax, tmDeviceClassMax);
                        destRow[index] = (deviceClass == null) ? DBNull.Value : (object)deviceClass;
                    }

                    break;

                case Constants.CodeOfNonMatch:
                case Constants.Reason:
                    //объединяем значения реквизитов в одну строку
                    string sValue = destRow[index].ToString();
                    string sPairValue = pairValue.ToString();

                    if ((sValue != string.Empty) && (sPairValue != string.Empty))
                        sValue = string.Concat(sValue, "; ");

                    sValue = string.Concat(sValue, sPairValue);

                    if (sValue == string.Empty)
                    {
                        destRow[index] = DBNull.Value;
                    }
                    else
                        destRow[index] = sValue;

                    //для возможности понять что мы приобрели в данной записи от сохраняемых данных из других записей - будем писать это в отдельный столбец
                    string pairColumnName = Routines.NameOfHiddenColumn(columnName);
                    int columnIndex = GetColumnIndexInDataTable(pairColumnName, null, null, typeof(string), typeof(string), columnsDT, false);

                    sValue = destRow[columnIndex].ToString();

                    if ((sValue != string.Empty) && (sPairValue != string.Empty))
                        sValue = string.Concat(sValue, " + ");

                    sValue = string.Concat(sValue, sPairValue);

                    if (sValue == string.Empty)
                    {
                        destRow[columnIndex] = DBNull.Value;
                    }
                    else
                        destRow[columnIndex] = sValue;

                    break;

                case Constants.Status:
                    //итоговый статус записи destRow вычисляется по статусу записи destRow и каждому статусу записи Pair. т.е. при каждом сохранении данных из записи Pair в запись destRow выполняется пересчёт итогового статуса
                    //создаём скрытый столбец, который будет хранить признак образования пары RT-TM или TM-RT в результате сохранения данных Pair записи в destRow
                    string isPairCreatedColumnName = Routines.NameOfIsPairCreatedColumn();
                    int isPairCreatedColumnIndex = GetColumnIndexInDataTable(isPairCreatedColumnName, null, null, typeof(bool), typeof(bool), columnsDT, false);

                    temperatureConditionDestRow = ProfileRoutines.TemperatureConditionByProfileName(this.GetProfileNameHandler(destRow.ItemArray));

                    //выясняем образована ли пара в записи destRow
                    object isPairCreatedObject = destRow[isPairCreatedColumnIndex];
                    bool isPairCreated = (isPairCreatedObject == DBNull.Value) ? false : (bool)isPairCreatedObject;

                    //если пара не образована - смотрим на температурные условия принимающей данные destRow
                    if (!isPairCreated)
                    {
                        //смотрим на температурные условия сохраняемой в DestRow записи
                        string tc = string.Concat(temperatureConditionDestRow.ToString(), "-", pairTemperatureCondition.ToString());

                        if ((tc == "RT-TM") || (tc == "TM-RT"))
                        {
                            //пара образована, выставляем об этом флаг в DestRow
                            isPairCreated = true;
                            destRow[isPairCreatedColumnIndex] = true;
                        }
                    }

                    //сохраняем историю вычисления статуса
                    string statusHistoryColumnName = Routines.NameOfHiddenColumn(columnName);
                    int statusHistoryColumnIndex = GetColumnIndexInDataTable(statusHistoryColumnName, null, null, typeof(string), typeof(string), columnsDT, false);
                    string statusHistory = destRow[statusHistoryColumnIndex].ToString();

                    if (statusHistory == string.Empty)
                    {
                        statusHistory = destRow[index].ToString();

                        if (statusHistory != string.Empty)
                            statusHistory = string.Concat(temperatureConditionDestRow.ToString(), ": ", statusHistory);
                    }

                    string pairStatus = pairValue.ToString();

                    if ((statusHistory != string.Empty) && (pairStatus != string.Empty))
                        statusHistory = string.Concat(statusHistory, separatorForValues);

                    if (pairStatus != string.Empty)
                        pairStatus = string.Concat(pairTemperatureCondition.ToString(), ": ", pairStatus);

                    statusHistory = string.Concat(statusHistory, pairStatus);

                    destRow[statusHistoryColumnIndex] = statusHistory;

                    //вычисляем итоговый статус. температурная пара может быть образована когда угодно и только в этот момент значение итогового статуса перестаёт быть не определённым
                    //если температурная пара RT-TM образована - вычисляем и пишем итоговое значение статуса, иначе итоговый статус имеет не определённое значение
                    destRow[index] = (isPairCreated) ? this.CalcStatus(statusHistory, separatorForValues) : string.Empty;

                    break;

                default:
                    string nameOfHiddenColumn = Routines.NameOfHiddenColumn(columnName);
                    int indexOfHiddenColumn = GetColumnIndexInDataTable(nameOfHiddenColumn, null, null, typeof(string), typeof(string), columnsDT, false);

                    //дописываем pairValue к тому, что уже хранится в destRow[indexOfHiddenColumn]
                    string storedValues = destRow[indexOfHiddenColumn].ToString();

                    if (storedValues != string.Empty)
                        storedValues = string.Concat(storedValues, Constants.StringDelimeter);

                    storedValues = string.Concat(storedValues, pairValue);

                    destRow[indexOfHiddenColumn] = storedValues;
                    break;
            }
        }
        */
        /*
        private void FillValuesToDataRow(DataTable columnsDT, DataRow dataRow, object[] values, bool isPairData)
        {
            //isPairData: значение true говорит о заливке в принятый dataRow данных из другой записи (но это всегда данные от одного и того же изделия, обрабатываемого по одному и тому же ПЗ и имеющие одно и тоже тело профиля). т.е. после заливки данных dataRow будет содержать результат объединения нескольких записей: данные из принятого dataRow объединятся с данными из принятого values
            //извлекаем тип изделия из текущей строки, представленной множеством полей values
            XmlDocument xmlDoc = new XmlDocument();
            string deviceType = this.GetDeviceTypeHandler(values);

            //считываем значение профиля из принятого values, по нему определяем температурный режим
            string profileName = this.GetProfileNameHandler(values);
            string tc = ProfileRoutines.StringTemperatureConditionByProfileName(profileName).ToUpper();

            //мы считали обозначение температурного режима прямо из обозначения профиля, вполне возможно, что технологи ошиблись в обозначении профиля. нам надо проверить это и скорректировать считанное tc
            //если температурный режим не определёный - будем такие столбцы относить к температурному режиму RT
            TemperatureCondition tC;
            if ((!Enum.TryParse(tc, out tC)) || (!Enum.IsDefined(typeof(TemperatureCondition), tc)))
                tc = TemperatureCondition.RT.ToString();

            double temperatureValue = -1;

            dataRow.BeginEdit();

            try
            {
                for (int i = 0; i < values.Count(); i++)
                {
                    object value = values[i];

                    //проверяем представлено ли текущее значение поля в формате XML
                    string sXML = this.ValueIsXml(value);

                    switch (sXML == null)
                    {
                        case (true):
                            //обычное (не XML) значение требуется дописать в dataRow
                            if (isPairData)
                            {
                                //сохраняем реквизит value принадлежащий другой записи в dataRow
                                TemperatureCondition pairTemperatureCondition;
                                if ((!Enum.TryParse(tc, true, out pairTemperatureCondition)) || (!Enum.IsDefined(typeof(TemperatureCondition), tc)))
                                    pairTemperatureCondition = TemperatureCondition.RT;

                                this.ProcessingNotXmlPairData(i, dataRow, pairTemperatureCondition, value, columnsDT);
                            }
                            else
                                dataRow[i] = value;

                            break;

                        default:
                            //имеем дело с описанием множества значений либо условий измерений, либо значений параметров изделия, либо комментариев в формате XML
                            xmlDoc.LoadXml(sXML);
                            XmlElement documentElement = xmlDoc.DocumentElement;

                            XMLValues subject = (documentElement.Name == "CONDITIONS") ? XMLValues.Conditions : (documentElement.Name == "PARAMETERS") ? XMLValues.Parameters : (documentElement.Name == Common.Constants.DeviceComments) ? XMLValues.DeviceComments : XMLValues.UnAssigned;

                            //считываем при какой температуре проводятся измерения. эта информация есть только в описании условий измерения                        
                            if (subject == XMLValues.Conditions)
                                temperatureValue = TemperatureConditionFromXML(xmlDoc);

                            ProcessingXmlData(columnsDT, documentElement, subject, tc, temperatureValue, deviceType, dataRow);
                            break;
                    }
                }
            }

            finally
            {
                dataRow.EndEdit();
            }
        }
        */
        private static String WildCardToRegular(string value)
        {
            return "^" + System.Text.RegularExpressions.Regex.Escape(value).Replace("\\*", ".*") + "$";
        }
        /*
        private void BuildDataRow(DataTable columnsDT, object[] values, DataTable dtData, string code, string groupName, string profileBody)
        {
            //пытаемся найти запись, где хранятся данные, идентифицируемые полями: code, groupName, profileBody
            DataRow row = null;//this.mapper.Pop(code, groupName, profileBody);

            string columnNameRecordIsStorage = Routines.NameOfRecordIsStorageColumn();
            int recordIsStorageColumnIndex = GetColumnIndexInDataTable(columnNameRecordIsStorage, null, null, typeof(bool), typeof(bool), columnsDT, false);

            switch (row == null)
            {
                //запись в dtData найдена - дописываем в неё всё из принятого values
                case (false):
                    this.FillValuesToDataRow(columnsDT, row, values, true);

                    //запись row приняла в себя данные values - фиксируем этот факт в скрытом столбце, хранящем флаг о прошедшем принятии данных
                    row[recordIsStorageColumnIndex] = true;
                    break;

                //в dtData нет подходящей записи, чтобы сохранить в неё данные из values - создаём новую и сохраняем в неё принятые данные values
                default:
                    DataRow dataRow = dtData.NewRow();
                    this.FillValuesToDataRow(columnsDT, dataRow, values, false); //dataRow.ItemArray = values;

                    //чтобы различать записи, в которые выполнено сохранение принятых данных values от записей, в которые не были сохранены данные values - инициализируем скрытый столбец в созданной dataRow
                    //в последствии во всех записях, не принявших в себя данных values будут сброшены значения статусов
                    dataRow[recordIsStorageColumnIndex] = false;
                    dtData.Rows.Add(dataRow);

                    //запоминаем расположение обработанной dataRow в mapper
                    try
                    {
                        //this.mapper.Push(code, groupName, profileBody, dataRow);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("{0}. code={1}, groupName={2}, profileBody={3}", string.Concat("При выполнении реализации this.mapper.Push перехвачена исключительная ситуация. ", ex.ToString()), code, groupName, profileBody));
                    }

                    break;
            }
        }
        */
        private delegate void delegateFillValues(DataTable columnsDT, object[] values, DataTable dtData);
        /*
        private void FillValues(DataTable columnsDT, object[] values, DataTable dtData)
        {
            //заливка данных values в dtData          
            string profileName = this.GetProfileNameHandler(values);
            string groupName = this.GetGroupNameHandler(values);
            string code = this.GetCodeHandler(values);

            string profileBody = ProfileRoutines.ProfileBodyByProfileName(profileName);

            //ищем куда сохранить принятые данные values
            this.BuildDataRow(columnsDT, values, dtData, code, groupName, profileBody);
        }
        */
        private delegate void delegateSetItemsSource(DataTable table);
        private void SetItemsSource(DataTable table)
        {
            if (table == null)
            {
                this.ItemsSource = null;
            }
            else
            {
                this.ItemsSource = table.DefaultView; //AsDataView();

                //применяем набор фильтров, которые имели место до данной загрузки данных
                this.SetRowFilter();
            }
        }

        private Type DataTypeByColumn(DataGridTextColumn column, out string bindPath)
        {
            //возвращает тип данных, который отображается в столбце Column
            Type type = null;
            bindPath = null;

            if (column != null)
            {
                Binding b = (Binding)column.Binding;

                if (b != null)
                {
                    bindPath = b.Path.Path;
                    //type = dtData?.Columns[bindPath]?.DataType;

                    DataView dv = this.ItemsSource as DataView;
                    type = dv?.Table.Columns[bindPath]?.DataType;
                }
            }

            return type;
        }

        public string ColumnName(DataGridBoundColumn column) //DataGridTextColumn
        {
            string result = null;

            if (column != null)
            {
                if (column.Binding is Binding b)
                    result = b.Path.Path;
            }

            return result;
        }

        private void columnHeader_Click(object sender, RoutedEventArgs e)
        {
            var columnHeader = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;

            if (btFilterClicked == null)
            {
                //вычисляем поле сортировки и его направление
                if (columnHeader != null)
                {
                    if (columnHeader == lastHeaderClicked)
                    {
                        if (lastSortedDirection == ListSortDirection.Ascending)
                        {
                            lastSortedDirection = ListSortDirection.Descending;
                        }
                        else lastSortedDirection = ListSortDirection.Ascending;
                    }
                    else lastSortedDirection = ListSortDirection.Ascending;

                    Path founded = null;

                    if (lastHeaderClicked != null)
                    {
                        founded = FindChild<Path>(lastHeaderClicked, "PathArrowUp");
                        if (founded != null)
                            founded.Visibility = Visibility.Collapsed;

                        founded = FindChild<Path>(lastHeaderClicked, "PathArrowDown");
                        if (founded != null)
                            founded.Visibility = Visibility.Collapsed;
                    }

                    lastHeaderClicked = columnHeader;

                    switch (lastSortedDirection)
                    {
                        case (ListSortDirection.Ascending):
                            founded = FindChild<Path>(lastHeaderClicked, "PathArrowUp");
                            break;

                        case (ListSortDirection.Descending):
                            founded = FindChild<Path>(lastHeaderClicked, "PathArrowDown");
                            break;

                        default:
                            founded = null;
                            break;
                    }

                    if (founded != null)
                        founded.Visibility = Visibility.Visible;
                }
            }
            else
            {
                //обработка нажатия кнопки фильтра
                Point position = btFilterClicked.PointToScreen(new Point(0d, 0d));
                position.Y += columnHeader.Height;

                //сбрасываем флаг о прошедшем нажатии кнопки фильтра
                btFilterClicked = null;

                DataGridTextColumn textColumn = (DataGridTextColumn)columnHeader.Column;

                Type filterType = DataTypeByColumn(textColumn, out string bindPath);
                SetFilter(position, filterType, columnHeader.Content.ToString(), bindPath);
            }
        }

        void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            DataGridColumn column = e.Column;

            switch (column.SortMemberPath)
            {
                case Common.Constants.Code:
                case Common.Constants.GroupName:
                case Common.Constants.Item:
                    {
                        ListSortDirection sortDirection = (column.SortDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;

                        this.SetItemsSource(null);

                        try
                        {
                            this.dtData?.Dispose();
                            CustomComparer<object> customComparer = new CustomComparer<object>(sortDirection);
                            this.dtData = this.dtData.AsEnumerable().OrderBy(row => row.Field<string>(column.SortMemberPath), customComparer).CopyToDataTable();
                        }
                        finally
                        {
                            this.SetItemsSource(this.dtData);
                        }

                        column.SortDirection = sortDirection;
                        e.Handled = true;
                        break;
                    }
            }
        }

        public DataRowView SelectedRow()
        {
            DataRowView result = null;

            System.Collections.IList rows = this.SelectedItems;

            //различаем ситуации: выбрана строка и выбрана ячейка
            if (rows.Count > 0)
            {
                result = (DataRowView)rows[0];
            }
            else
            {
                IList<DataGridCellInfo> selCells = this.SelectedCells;

                if (selCells != null)
                {
                    if (selCells.Count != 0)
                    {
                        DataGridCellInfo cellInfo = this.SelectedCells[0];
                        FrameworkElement cellContent = cellInfo.Column.GetCellContent(cellInfo.Item);

                        result = (DataRowView)cellContent.DataContext;
                    }
                }
            }

            return result;
        }

        public object ValueFromSelectedRow(string bindPath) //SelectedText
        {
            //возвращает object из выбранной пользователем в DataGrid строки
            DataRowView row = this.SelectedRow();
            object result = row?[bindPath];

            return result;
        }

        private string FiltersToString()
        {
            string result = string.Empty;

            this.activeFilters.Correct();
            int index = 0;

            while ((this.activeFilters.Count > 0) && (index < this.activeFilters.Count))
            {
                FilterDescription f = this.activeFilters[index];

                if (result != string.Empty)
                    result += " AND ";

                //если f.value=null - тип значения нам не важен
                if (f.value == null)
                    result += string.Format("{0}{1}{2}", f.fieldName, f.comparisonCorrected, f.valueCorrected);
                else
                {
                    if (f.type == typeof(string))
                        result += string.Format("{0}{1}'{2}'", f.fieldName, f.comparisonCorrected, f.valueCorrected);

                    if (f.type == typeof(DateTime))
                    {
                        //извлекаем значение даты из DataTime (значение времени отбрасываем). возможности обработки значения поля 'TS' каждой строки ограничены возможностями DataColumn.Expression. ничего лучше, чем рассмотреть DataTime как строку и вырезать из неё первые 10 символов на данный момент не придумал
                        if (DateTime.TryParse(f.value.ToString(), out DateTime value))
                            result += string.Format("Substring(Convert({0}, 'System.String'), 1, 10){1}'{2}'", f.fieldName, f.comparison, value.ToShortDateString());
                        else
                            this.activeFilters.Remove(f);
                    }

                    if (f.type == typeof(int))
                    {
                        if (int.TryParse(f.value.ToString(), out int value))
                            result += string.Format("{0}{1}{2}", f.fieldName, f.comparison, value.ToString());
                        else
                            this.activeFilters.Remove(f);
                    }

                    if (f.type == typeof(double))
                    {
                        if (SCME.Common.Routines.TryStringToDouble(f.value.ToString(), out double value))
                            result += string.Format("{0}{1}{2}", f.fieldName, f.comparison, value.ToString().Replace(',', '.'));
                        else
                            this.activeFilters.Remove(f);
                    }

                    if (f.type == typeof(decimal))
                    {
                        if (SCME.Common.Routines.TryStringToDouble(f.value.ToString(), out double value))
                            result += string.Format("{0}{1}{2}", f.fieldName, f.comparison, value.ToString().Replace(',', '.'));
                        else
                            this.activeFilters.Remove(f);
                    }
                }

                index++;
            }

            return result;
        }

        private void SetFilter(Point position, Type type, string tittlefieldName, string bindPath)
        {
            if (this.ItemsSource != null)
            {
                var dv = this.ItemsSource as DataView;

                if (dv != null)
                {
                    FilterDescription filter = new FilterDescription { type = type, tittlefieldName = tittlefieldName, fieldName = bindPath, comparison = "=", value = this.ValueFromSelectedRow(bindPath) };
                    this.activeFilters.Add(filter);

                    FiltersInput fmFiltersInput = new FiltersInput(this.activeFilters);

                    if (fmFiltersInput.Demonstrate(position) == true)
                        this.SetRowFilter();

                    if (this.Items.Count > 0)
                        this.SelectedIndex = 0;

                    if (this.RefreshBottomRecordCountHandler != null)
                        this.RefreshBottomRecordCountHandler();
                }
            }
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null)
                return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null)
                        break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public CheckNrmStatus IsInNrm(DataRow row, string columnName)
        {
            //возвращает:
            //CheckNrmStatus.UnCheckable - проверка норм не имеет смысла
            //CheckNrmStatus.Good - значение в пределах нормы
            //CheckNrmStatus.Defective - значение вне нормы
            //CheckNrmStatus.NotSetted - нормы не установлены

            CheckNrmStatus result = CheckNrmStatus.UnCheckable;

            if (row != null)
            {
                //считываем проверяемое значение              
                int columnIndex = row.Table.Columns.IndexOf(columnName);

                if (columnIndex != -1)
                {
                    //нормы могут иметь столбцы с индексами начиная с this.FirstCPColumnIndexInDataTable1 и до упора
                    if (columnIndex >= this.FirstCPColumnIndexInDataTable1)
                    {
                        var value = row[columnIndex];

                        if (value != DBNull.Value)
                        {
                            //считываем значение нормы min
                            object nrmMin = DBNull.Value;

                            string nameOfNrmMinParametersColumn = Routines.NameOfNrmMinParametersColumn(columnName);
                            int columnNrmMinIndex = row.Table.Columns.IndexOf(nameOfNrmMinParametersColumn);

                            if (columnNrmMinIndex == -1)
                            {
                                result = CheckNrmStatus.NotSetted;
                            }
                            else
                            {
                                nrmMin = row[columnNrmMinIndex];

                                if (nrmMin == DBNull.Value)
                                {
                                    result = CheckNrmStatus.NotSetted;
                                }
                                else
                                {
                                    double dValue = double.Parse(value.ToString());
                                    double dNrmMin = double.Parse(nrmMin.ToString());

                                    result = (dNrmMin < dValue) ? CheckNrmStatus.Good : CheckNrmStatus.Defective;

                                    if (result == CheckNrmStatus.Defective)
                                        return result;
                                }
                            }

                            //считываем значения нормы max
                            string nameOfNrmMaxParametersColumn = Routines.NameOfNrmMaxParametersColumn(columnName);
                            int columnNrmMaxIndex = row.Table.Columns.IndexOf(nameOfNrmMaxParametersColumn);

                            if (columnNrmMaxIndex == -1)
                            {
                                if (result == CheckNrmStatus.NotSetted)
                                    return CheckNrmStatus.NotSetted;
                            }
                            else
                            {
                                var nrmMax = row[columnNrmMaxIndex];

                                if (nrmMax == DBNull.Value)
                                {
                                    if (result == CheckNrmStatus.NotSetted)
                                        return CheckNrmStatus.NotSetted;
                                }
                                else
                                {
                                    //проверяем не является ли описание нормы Max описанием для проверки Boolean значения
                                    if ((nrmMin == DBNull.Value) && (Routines.IsBoolean(nrmMax.ToString())))
                                    {
                                        //имеем дело с описанием норм на Boolean значение
                                        bool bValue = Convert.ToBoolean(value);
                                        bool bNrmMax = Convert.ToBoolean(nrmMax);

                                        result = (bValue == bNrmMax) ? CheckNrmStatus.Good : CheckNrmStatus.Defective;

                                        if (result == CheckNrmStatus.Defective)
                                            return result;
                                    }
                                    else
                                    {
                                        //имеем дело с описанием норм на float параметры
                                        double dValue = double.Parse(value.ToString());
                                        double dNrmMax = double.Parse(nrmMax.ToString());

                                        result = (dValue <= dNrmMax) ? CheckNrmStatus.Good : CheckNrmStatus.Defective;

                                        if (result == CheckNrmStatus.Defective)
                                            return result;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

    }

    public class FilterDescription
    {
        public Type type;
        public string tittlefieldName { get; set; }
        public string fieldName;
        public string comparison { get; set; }
        public string comparisonCorrected;
        public object value { get; set; }
        public string valueCorrected;

        public void Correct()
        {
            //корректировка описания фильтра
            if (this.value == null)
            {
                this.comparison = "=";
                this.comparisonCorrected = " IS ";
                this.valueCorrected = "NULL";
            }
            else
            {
                string newValue = this.value.ToString().Replace("*", "%");

                if (this.type == typeof(string))
                {
                    switch (newValue == this.value.ToString())
                    {
                        case true:
                            this.comparison = "=";
                            this.comparisonCorrected = "=";
                            this.valueCorrected = this.value.ToString();
                            break;

                        default:
                            this.comparisonCorrected = " LIKE ";
                            this.valueCorrected = newValue;
                            break;
                    }
                }
            }
        }
    }

    public class ActiveFilters : ObservableCollection<FilterDescription>
    {
        public bool FieldNameStoredMoreThanOnce(string fieldName)
        {
            //вычисляет сколько раз в this сохранено значений фильтров с принятым fieldName
            var linqResults = this.Where(fn => fn.fieldName == fieldName);

            return (linqResults.Count() > 1);
        }

        public void Correct()
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                FilterDescription f = this.Items[i];

                string sValue = f.value.ToString();
                sValue = Regex.Replace(sValue, @"\s+", "");

                if (sValue == string.Empty)
                    f.value = null;

                f.Correct();
            }
        }
    }

    public class NrmToBrushMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            CheckNrmStatus inNrm = CheckNrmStatus.UnCheckable;

            DataGridCell cell = values[0] as DataGridCell;
            DataRow row = values[1] as DataRow;
            DataGridSqlResult dataGrid = values[2] as DataGridSqlResult;

            if (dataGrid != null)
            {
                if ((cell != null) && (row != null))
                {
                    DataGridTextColumn column = cell.Column as DataGridTextColumn;

                    if (column != null)
                    {
                        string columnName = dataGrid.ColumnName(column);
                        inNrm = dataGrid.IsInNrm(row, columnName);
                    }
                }

                switch (inNrm)
                {
                    case CheckNrmStatus.UnCheckable:
                        //проверять нечего
                        return DependencyProperty.UnsetValue;

                    case (CheckNrmStatus.Good):
                        //в норме
                        return dataGrid.FindResource("ValueInNrm");

                    case (CheckNrmStatus.Defective):
                        //за пределами нормы
                        return dataGrid.FindResource("ValueOutSideTheNorm");

                    case (CheckNrmStatus.NotSetted):
                        //норма не установлена
                        return dataGrid.FindResource("NrmNotSetted");

                    default:
                        return DependencyProperty.UnsetValue;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class ToolTipMultiConverter : IMultiValueConverter
    {
        private object ValueByColumnName(string columnName, DataRow row)
        {
            /*
            if ((columnName != null) && (row != null))
            {
                int columnIndex = -1;

                switch (columnName)
                {
                    case Constants.DeviceClass:
                        //итоговый класс изделия вычисляется по двум значениям: min(maxRT, maxTM)
                        //покажем как вычисляется итоговое значение класса
                        string rtColumnNameDeviceClass = string.Concat(Constants.RT, Routines.NameOfHiddenColumn(columnName));
                        columnIndex = row.Table.Columns.IndexOf(rtColumnNameDeviceClass);
                        string rtDeviceClass = (columnIndex == -1) ? null : (row[columnIndex] == DBNull.Value) ? null : row[columnIndex].ToString();

                        string tmColumnNameDeviceClass = string.Concat(Constants.TM, Routines.NameOfHiddenColumn(columnName));
                        columnIndex = row.Table.Columns.IndexOf(tmColumnNameDeviceClass);
                        string tmDeviceClass = (columnIndex == -1) ? null : (row[columnIndex] == DBNull.Value) ? null : row[columnIndex].ToString();

                        if (((rtDeviceClass == null) || (rtDeviceClass == string.Empty)) && ((tmDeviceClass == null) || (tmDeviceClass == string.Empty)))
                        {
                            return Constants.noData;
                        }
                        else
                            return string.Concat(string.Format("RT=max[{0}]", rtDeviceClass, ";"), Constants.StringDelimeter, string.Format("TM=max[{0}]", tmDeviceClass), Constants.StringDelimeter, "Класс=min[RT, TM]");

                    default:
                        //получаем имя столбца в row.Table, который хранит скрытые данные
                        columnName = Routines.NameOfHiddenColumn(columnName);
                        columnIndex = row.Table.Columns.IndexOf(columnName);

                        object value = (columnIndex == -1) ? DBNull.Value : row[columnIndex];

                        return (value == DBNull.Value) ? Constants.noData : value;
                }
            }
            */

            return Constants.noData;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridCell cell = values[0] as DataGridCell;
            DataRow row = values[1] as DataRow;
            DataGridSqlResult dataGrid = values[2] as DataGridSqlResult;

            if ((cell != null) && (row != null) && (dataGrid != null))
            {
                DataGridTextColumn column = cell.Column as DataGridTextColumn;

                if (column != null)
                {
                    string columnName = dataGrid.ColumnName(column);

                    return this.ValueByColumnName(columnName, row);
                }
            }

            return Constants.noData;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class HeaderVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //не будем показывать пользователю Header если количество записей отображаемых в DataGridSqlResult равно нулю и это не вызвано специфически установленными значениями фильтра
            //но если пользователь так настроил свои фильтры что это привело к пустому множеству в отображаемых данных - показывать header необходимо ибо пользователю при этом обязятельно должны быть доступны средства управления значениями фильтров
            int count = (int)values[0];
            DataGridSqlResult dg = values[1] as DataGridSqlResult;

            if (dg != null)
            {
                var dv = dg.ItemsSource as DataView;

                return ((dg.Items.Count == 0) && ((dv == null) || (dv.RowFilter == null) || (dv.RowFilter == string.Empty))) ? Visibility.Hidden : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum CheckNrmStatus
    {
        [EnumMember]
        UnCheckable = 0,

        [EnumMember]
        Good = 1,

        [EnumMember]
        Defective = 2,

        [EnumMember]
        NotSetted = 3
    }

    /*
    [Flags]
    public enum XMLValues
    {
        //не определённое значение описания в формате XML
        UnAssigned = 0x00,

        //описание в формате XML условий измерений
        Conditions = 0x01,

        //описание в формате XML измеренных значений параметров
        Parameters = 0x02,

        //описание в формате XML комментариев к изделию
        DeviceComments = 0x03
    }
    */
}
