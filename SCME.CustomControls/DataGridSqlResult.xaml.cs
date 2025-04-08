using SCME.Common;
using SCME.InterfaceImplementations;
using SCME.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace SCME.CustomControls
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class DataGridSqlResult : DataGrid
    {
        private ConcurrentQueue<Action> queueManager = new ConcurrentQueue<Action>();
        private DataGridColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastSortedDirection = ListSortDirection.Ascending;
        private Button btFilterClicked = null;

        public SqlConnection connection = null;
        public DataTable dtData = null;
        private ActiveFilters activeFilters;

        private ICollectionView collectionView = null;
        public ICollectionView CollectionView
        {
            get { return this.collectionView; }
        }

        public DataGridSqlResult() : base()
        {
            InitializeComponent();

            this.Sorting += new DataGridSortingEventHandler(SortingHandler);
            this.activeFilters = new ActiveFilters();
        }

        public void ViewSqlResultByThread(string queryString)
        {
            //выполнение реализации this.ViewSqlResultHandler в фоновом потоке, после выполнения которой система исполнит реализацию this.AfterFillDataTableHandler в главном потоке
            LongTimeRoutineWorker Worker = new LongTimeRoutineWorker(this.ViewSqlResultHandler, this.AfterFillDataTableHandler);

            Object[] args = { queryString };
            Worker.Run(args);
        }

        public void ViewSqlResultByThread(string queryString, SqlConnection connection)
        {
            //входной параметр connection позволяет работать с разными базами данных тз списка поддерживаемых DBConnections
            LongTimeRoutineWorker Worker = new LongTimeRoutineWorker(this.ViewSqlResultHandler, this.AfterFillDataTableHandler);

            object[] args = { queryString, connection };
            Worker.Run(args);
        }

        private void ViewSqlResultHandler(DoWorkEventArgs e)
        {
            //извлекаем из принятого workerParameters параметры, необходимые для вызова this.ViewSqlResult
            object[] arg = e.Argument as object[];
            string queryString = (string)arg[0];

            //по индексу 1 может располагаться connection, также индекс 1 может не быть использованным
            SqlConnection сonnection = (arg.Length >= 2) ? arg[1] as SqlConnection : null;

            this.ViewSqlResult(queryString, сonnection);
        }

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

        private delegate void delegateSetItemsSource(DataTable table);
        private void SetItemsSource(DataTable table)
        {
            if (table == null)
            {
                this.ItemsSource = null;
            }
            else
            {
                this.ItemsSource = table.DefaultView;

                //применяем набор фильтров, которые имели место до данной загрузки данных
                this.SetRowFilter();
            }
        }

        private void SetRowFilter()
        {
            if (this.ItemsSource is DataView dv)
            {
                try
                {
                    string rowFilter = this.activeFilters.WhereSection(false, out string joinSection, out string havingSection);
                    dv.RowFilter = rowFilter ?? string.Empty; //this.FiltersToString();
                }
                catch (Exception)
                {
                    MessageBox.Show("Введённое значение фильтра не соответствует типу фильтруемых данных.", "Filter error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
            else
            {
                this.collectionView = CollectionViewSource.GetDefaultView(this.ItemsSource);
                this.CollectionView.Filter = new Predicate<object>(this.Contains);
            }
        }

        public bool Contains(object obj)
        {
            return this.CheckActiveFilters(obj);
        }

        public void ShowSortDirectionIndicator(DataGridColumnHeader columnHeader, ListSortDirection sortDirection)
        {
            //для вызова с целью показать сортированный столбец и направление сортировки
            this.SetSortDirectionIndicator(this.lastHeaderClicked, columnHeader, sortDirection);
            this.lastHeaderClicked = columnHeader;
            this.lastSortedDirection = sortDirection;
        }

        public void UnVisibleSortIndicator()
        {
            //делаем не видимыми индикаторы сортировки
            if (this.lastHeaderClicked != null)
                this.UnVisibleSortIndicatorByColumnHeader(this.lastHeaderClicked);
        }

        private void UnVisibleSortIndicatorByColumnHeader(DataGridColumnHeader columnHeader)
        {
            if (columnHeader != null)
            {
                Path sortDirectionIndicator = FindChild<Path>(columnHeader, "PathArrowUp");

                if (sortDirectionIndicator != null)
                    sortDirectionIndicator.Visibility = Visibility.Collapsed;

                sortDirectionIndicator = FindChild<Path>(columnHeader, "PathArrowDown");

                if (sortDirectionIndicator != null)
                    sortDirectionIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void SetSortDirectionIndicator(DataGridColumnHeader forOffIndicator, DataGridColumnHeader forSetIndicator, ListSortDirection sortDirection)
        {
            //направление сортировки sortDirection:
            // Ascending - от малых значений к большим (прямая сортировка);
            // Descending - от больших значений к малым (обратная сортировка)
            Path sortDirectionIndicator = null;

            if (forOffIndicator != null)
                this.UnVisibleSortIndicatorByColumnHeader(forOffIndicator);

            switch (sortDirection)
            {
                case (ListSortDirection.Ascending):
                    sortDirectionIndicator = FindChild<Path>(forSetIndicator, "PathArrowUp");
                    break;

                case (ListSortDirection.Descending):
                    sortDirectionIndicator = FindChild<Path>(forSetIndicator, "PathArrowDown");
                    break;
            }

            if (sortDirectionIndicator != null)
                sortDirectionIndicator.Visibility = Visibility.Visible;
        }

        private void columnHeader_Click(object sender, RoutedEventArgs e)
        {
            //var columnHeader = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;

            if (btFilterClicked == null)
            {
                if (this.ItemsSource.OfType<object>().Count() > 0)
                {
                    //вычисляем поле сортировки и его направление
                    if (sender is DataGridColumnHeader columnHeader) //if (columnHeader != null)
                    {
                        if (columnHeader == this.lastHeaderClicked)
                        {
                            this.lastSortedDirection = (this.lastSortedDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;
                        }
                        else
                            this.lastSortedDirection = ListSortDirection.Ascending;

                        this.SetSortDirectionIndicator(this.lastHeaderClicked, columnHeader, this.lastSortedDirection);
                        this.lastHeaderClicked = columnHeader;
                    }
                }
            }
            else
            {
                //обработка нажатия кнопки фильтра
                if (sender is DataGridColumnHeader columnHeader)
                {
                    Point position = btFilterClicked.PointToScreen(new Point(0d, 0d));
                    position.Y += columnHeader.Height;

                    //сбрасываем флаг о прошедшем нажатии кнопки фильтра
                    btFilterClicked = null;

                    if (columnHeader.Column is DataGridTextColumn textColumn)
                    {
                        string filterType = DataTypeByColumn(textColumn, out string bindPath);
                        SetFilter(position, filterType, columnHeader.Content.ToString(), bindPath);
                    }
                }
            }
        }

        private void SortingHandler(object sender, DataGridSortingEventArgs e)
        {
            DataGridColumn column = e.Column;

            switch (column.SortMemberPath)
            {
                case Common.Constants.Code:
                case Common.Constants.GroupName:
                case Common.Constants.Item:
                    {
                        if (this.dtData != null)
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
                        }

                        break;
                    }
            }
        }

        /*
        private string FiltersToString()
        {
            string result = string.Empty;

            if (this.activeFilters == null)
                return null;

            this.activeFilters.Correct();
            int index = 0;

            while ((this.activeFilters.Count > 0) && (index < this.activeFilters.Count))
            {
                FilterDescription f = this.activeFilters[index];

                if (result != string.Empty)
                    result += " AND ";

                //если f.value=null - тип значения нам не важен
                if (f.Value == null)
                    result += string.Format("{0}{1}{2}", f.FieldName, f.ComparisonCorrected, f.ValueCorrected);
                else
                {
                    if (f.Type == typeof(string))
                        result += string.Format("{0}{1}'{2}'", f.FieldName, f.Comparison, f.ValueCorrected);

                    if (f.Type == typeof(DateTime))
                    {
                        DateTime value;

                        //извлекаем значение даты из DataTime - отбрасываем значение времени
                        if (DateTime.TryParse(f.Value.ToString(), out value))
                            //выполняем сравнение DateTime без миллисекунд
                            result += string.Format("Convert(Substring(Convert({0}, 'System.String'), 1, 19), 'System.DateTime'){1}'{2}'", f.FieldName, f.Comparison, value);
                        else
                            this.activeFilters.Remove(f);
                    }

                    if (f.Type == typeof(int))
                    {
                        int value;

                        if (int.TryParse(f.Value.ToString(), out value))
                            result += string.Format("{0}{1}{2}", f.FieldName, f.Comparison, value.ToString());
                        else
                            this.activeFilters.Remove(f);
                    }

                    if (f.Type == typeof(double))
                    {
                        double value;

                        if (SCME.Common.Routines.TryStringToDouble(f.Value.ToString(), out value))
                            result += string.Format("{0}{1}{2}", f.FieldName, f.Comparison, value.ToString().Replace(',', '.'));
                        else
                            this.activeFilters.Remove(f);
                    }
                }

                index++;
            }

            return result;
        }
        */
        private bool SimpleCompareString(string instance, string testedValue, string comparison)
        {
            int compareResult = instance.CompareTo(testedValue);

            switch (compareResult)
            {
                case 0:
                    if ((comparison != "=") && (comparison != "<=") && (comparison != ">="))
                        return false;

                    break;

                default:
                    if (compareResult < 0)
                    {
                        if ((comparison == "=") || (comparison == "<") || (comparison == "<="))
                            return false;
                    }
                    else
                    {
                        if ((comparison == "=") || (comparison == ">") || (comparison == ">="))
                            return false;
                    }

                    break;
            }

            return true;
        }

        private bool SimpleCompareDateTime(DateTime instance, DateTime testedValue, string comparison)
        {
            int compareResult = instance.CompareTo(testedValue);

            switch (compareResult)
            {
                case 0:
                    if ((comparison != "=") && (comparison != "<=") && (comparison != ">="))
                        return false;

                    break;

                default:
                    if (compareResult < 0)
                    {
                        if ((comparison == "=") || (comparison == "<") || (comparison == "<="))
                            return false;
                    }
                    else
                    {
                        if ((comparison == "=") || (comparison == ">") || (comparison == ">="))
                            return false;
                    }

                    break;
            }

            return true;
        }

        private bool CheckActiveFilters(object obj)
        {
            //проверяет проходит ли принятый obj через все имеющиеся на момент вызова фильтры
            //возвращает:
            //            true - принятый obj содержит данные, которые ищет пользователь;
            //            false - принятый obj не имеет данных, которые ищет пользователь
            this.activeFilters.Correct();

            foreach (FilterDescription f in this.activeFilters)
            {
                //из фильтра считываем поле по которому выполняется фильтрация
                string fieldName = f.FieldName;

                //из принятого obj считываем PropertyInfo чтобы прочитать значение поля fieldName
                Type objType = obj.GetType();
                System.Reflection.PropertyInfo pi = objType.GetProperty(fieldName);

                //считываем из obj значение поля с именем fieldName
                string sObjValue = pi.GetValue(obj).ToString();

                string sValue = f.Value.ToString(); //ValueCorrected

                if (sValue.Contains("%"))
                {
                    if (!SCME.Common.Routines.Like(sObjValue, sValue))
                        return false;
                }
                else
                {
                    if (f.Type == typeof(string).FullName)
                    {
                        if (!this.SimpleCompareString(sValue, sObjValue, f.Comparison))
                            return false;
                    }

                    switch (f.Type)
                    {
                        //в sValue есть время
                        case "System.DateTime":
                            if (DateTime.TryParse(sValue, out DateTime dtValue))
                            {
                                if (DateTime.TryParse(sObjValue, out DateTime dtObjValue))
                                    if (!this.SimpleCompareDateTime(dtValue, dtObjValue, f.Comparison))
                                        return false;
                            }

                            break;

                        //в sValue есть только дата
                        case "System.DateOnly":
                            DateTime? dValue = SCME.Common.Routines.StringToDateZeroTime(sValue);

                            if (dValue != null)
                            {
                                DateTime? dObjValue = SCME.Common.Routines.StringToDateZeroTime(sObjValue);

                                if (dObjValue != null)
                                {
                                    if (!this.SimpleCompareDateTime((DateTime)dValue, (DateTime)dObjValue, f.Comparison))
                                        return false;
                                }
                            }

                            break;
                    }
                }
            }

            return true;


            /*
                        this.activeFilters.Correct();

                        foreach (FilterDescription f in this.activeFilters)
                        {
                            //из фильтра считываем поле по которому выполняется фильтрация
                            string fieldName = f.fieldName;

                            //из принятого obj считываем PropertyInfo чтобы прочитать значение поля fieldName
                            Type objType = obj.GetType();
                            System.Reflection.PropertyInfo pi = objType.GetProperty(fieldName);

                            if (f.type == typeof(string))
                            {
                                //считываем из obj значение поля с именем fieldName
                                string sObjValue = pi.GetValue(obj).ToString();
                                string sValue = f.valueCorrected;


                                if (sValue.Contains("%"))
                                {
                                    if (!Routines.Like(sObjValue, sValue))
                                        return false;
                                }
                                else
                                {
                                    if (!sValue.Equals(sObjValue))
                                        return false;
                                }
                            }

                            if (f.type == typeof(DateTime))
                            {
                                string sValue = f.value.ToString();
                                DateTime value;

                                //извлекаем значение даты из DataTime - отбрасываем значение времени
                                if (DateTime.TryParse(sValue, out value))
                                {
                                    //считываем из obj значение поля с именем fieldName
                                    string sObjValue = pi.GetValue(obj).ToString();

                                    DateTime objValue;
                                    if (DateTime.TryParse(sObjValue, out objValue))
                                    {
                                        sObjValue = Routines.DateByDateTime(objValue);
                                        objValue = DateTime.Parse(sObjValue);

                                        int compareResult = DateTime.Compare(objValue, value);

                                        switch (compareResult)
                                        {
                                            //сравниваемые даты равны value=objValue
                                            case 0:
                                                if ((f.comparison == "<") || (f.comparison == ">"))
                                                    return false;

                                                break;

                                            default:
                                                if (compareResult < 0)
                                                {
                                                    //value<objValue
                                                    if ((f.comparison == "=") || (f.comparison == ">") || (f.comparison == ">="))
                                                        return false;
                                                }
                                                else
                                                {
                                                    //value>objValue
                                                    if ((f.comparison == "=") || (f.comparison == "<") || (f.comparison == "<="))
                                                        return false;
                                                }

                                                break;
                                        }

                                    }
                                }
                                else
                                    this.activeFilters.Remove(f);
                            }

                            if (f.type == typeof(int))
                            {
                                string sValue = f.value.ToString();

                                int iValue;
                                if (int.TryParse(sValue, out iValue))
                                {
                                    //считываем из obj значение поля с именем fieldName
                                    string sObjValue = pi.GetValue(obj).ToString();

                                    int iObjValue;

                                    if (int.TryParse(sObjValue, out iObjValue))
                                    {
                                        switch (iObjValue == iValue)
                                        {
                                            case true:
                                                //сравниваемые значения равны iObjValue = iValue
                                                if ((f.comparison == "<") || (f.comparison == ">"))
                                                    return false;

                                                break;

                                            default:
                                                if (iValue < iObjValue)
                                                {
                                                    if ((f.comparison == "=") || (f.comparison == ">") || (f.comparison == ">="))
                                                        return false;

                                                }
                                                else
                                                {
                                                    //iValue>iObjValue
                                                    if ((f.comparison == "=") || (f.comparison == "<") || (f.comparison == "<="))
                                                        return false;
                                                }

                                                break;
                                        }
                                    }
                                }
                                else
                                    this.activeFilters.Remove(f);
                            }

                            if (f.type == typeof(double))
                            {
                                string sValue = f.value.ToString();

                                double dValue;
                                if (double.TryParse(sValue, out dValue))
                                {
                                    //считываем из obj значение поля с именем fieldName
                                    string sObjValue = pi.GetValue(obj).ToString();

                                    double dObjValue;

                                    if (double.TryParse(sObjValue, out dObjValue))
                                    {
                                        switch (dObjValue == dValue)
                                        {
                                            case true:
                                                //сравниваемые значения равны dObjValue = dValue
                                                if ((f.comparison == "<") || (f.comparison == ">"))
                                                    return false;

                                                break;

                                            default:
                                                if (dValue < dObjValue)
                                                {
                                                    if ((f.comparison == "=") || (f.comparison == ">") || (f.comparison == ">="))
                                                        return false;

                                                }
                                                else
                                                {
                                                    //dValue>dObjValue
                                                    if ((f.comparison == "=") || (f.comparison == "<") || (f.comparison == "<="))
                                                        return false;
                                                }

                                                break;
                                        }
                                    }
                                }
                                else
                                    this.activeFilters.Remove(f);
                            }
                        }

                        //раз мы здесь - значит принятый obj успешно прошёл через все фильтры
                        return true;
                        */
        }

        private object SelectedRow() //DataRowView
        {
            object result = null;

            System.Collections.IList rows = this.SelectedItems;

            //различаем ситуации: выбрана строка и выбрана ячейка
            if (rows.Count > 0)
            {
                //выбрана строка
                result = rows[0];
            }
            else
            {
                //выбрана ячейка
                IList<DataGridCellInfo> selCells = this.SelectedCells;

                if (selCells != null)
                {
                    if (selCells.Count > 0)
                    {
                        DataGridCellInfo cellInfo = selCells[0];  //this.SelectedCells[0];
                        result = cellInfo.Item;
                    }
                }
            }

            return result;
        }

        public object ValueFromSelectedRow(string bindPath) //SelectedText
        {
            //возвращает значение из выбранной пользователем в this строки и столбца
            object result = null;

            if (this.ItemsSource.OfType<object>().Count() > 0)
            {
                object obj = this.SelectedRow();

                if (obj != null)
                {
                    if (obj is DataRowView row)
                    {
                        result = row?[bindPath];
                    }
                    else
                    {
                        Type elementType = this.ItemsSource.AsQueryable().ElementType;
                        System.Reflection.PropertyInfo pi = elementType.GetProperty(bindPath);

                        result = pi.GetValue(obj);
                    }
                }
            }

            return result;
        }

        private Window GetWindow()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);

            while (!(parent is Window))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as Window;
        }

        private void SetFilter(Point position, string type, string tittlefieldName, string bindPath)
        {
            if (this.ItemsSource != null)
            {
                FilterDescription filter = new FilterDescription(this.activeFilters, bindPath) { Type = type, TittlefieldName = tittlefieldName, Comparison = "=", Value = this.ValueFromSelectedRow(bindPath) };
                this.activeFilters.Add(filter);

                FiltersInput fmFiltersInput = new FiltersInput(this.activeFilters, this.GetWindow());

                if (fmFiltersInput.Demonstrate(position) == true)
                    this.SetRowFilter();

                if (this.Items.Count > 0)
                    this.SelectedIndex = 0;
                /*
                if (this.RefreshBottomRecordCountHandler != null)
                    this.RefreshBottomRecordCountHandler();
                */
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
        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
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

        private T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }

            return child;
        }

        private void ViewSqlResult(string queryString, SqlConnection сonnection)
        {
            //отображение результата выполнения запроса с текстом queryString
            //если принятый сonnection=null - требуется работа с центральной базой данных КИП СПП
            this.connection = сonnection ?? DBConnections.Connection;

            bool connectionOpened = false;

            if (!Types.DbRoutines.IsDBConnectionAlive(this.connection))
            {
                this.connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand(queryString, this.connection)
                {
                    CommandTimeout = 1000
                };

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    this.dtData = CreateDataTable(reader);
                }

                finally
                {
                    reader.Close();
                }

                this.FillDataTable(command);
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    this.connection.Close();
            }
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
                    string nameInDataTable = row["ColumnName"].ToString();

                    NewColumnInDataTable(result, nameInDataTable, (Type)(row["DataType"]), (bool)row["IsUnique"], (bool)row["AllowDBNull"], (bool)row["IsAutoIncrement"]);
                }
            }

            return result;
        }

        private int NewColumnInDataTable(DataTable dt, string columnName, Type columnType, bool unique, bool allowDBNull, bool autoIncrement)
        {
            //создание нового столбца в dt
            //возвращает индекс созданного столбца
            if (dt == null)
                return -1;

            DataColumn column = new DataColumn(columnName, columnType);
            column.Unique = unique;
            column.AllowDBNull = allowDBNull;
            column.AutoIncrement = autoIncrement;
            column.DefaultValue = DBNull.Value;
            dt.Columns.Add(column);

            return dt.Columns.IndexOf(column);
        }

        private void FillDataTable(SqlCommand command)
        {
            SqlDataReader reader = command.ExecuteReader();
            object[] values = new object[reader.FieldCount];

            while (reader.Read())
            {
                reader.GetValues(values);

                this.FillValues(values, this.dtData);
            }
        }

        private void ClearColumns()
        {
            //данная реализация вызывается фоновым потоком, поэтому просто так её вызывать нельзя
            queueManager.Enqueue(delegate
            {
                this.Columns.Clear();
            }
                                );
        }

        private void FillValues(object[] values, DataTable dtData)
        {
            //заливка данных values в dtData      
            DataRow dataRow = dtData.NewRow();

            dataRow.BeginEdit();

            try
            {
                for (int i = 0; i < values.Count(); i++)
                {
                    object value = values[i];
                    dataRow[i] = value;
                }
            }
            finally
            {
                dataRow.EndEdit();

            }

            dtData.Rows.Add(dataRow);
        }

        private void btFilter_Click(object sender, RoutedEventArgs e)
        {
            //выставляем флаг о прошедшем нажатии кнопки
            btFilterClicked = (Button)sender;
        }

        private string DataTypeByColumn(DataGridTextColumn column, out string bindPath)
        {
            //возвращает тип данных, который отображается в столбце Column

            string result = null;
            bindPath = null;

            if (column != null)
            {
                Binding b = (Binding)column.Binding;

                if (b != null)
                {
                    Type type = null;
                    bindPath = b.Path.Path;

                    if (this.ItemsSource is DataView dv)
                    {
                        type = dv.Table.Columns[bindPath]?.DataType;
                    }
                    else
                    {
                        if (this.ItemsSource != null)
                        {
                            Type elementType = this.ItemsSource.AsQueryable().ElementType;
                            type = elementType.GetProperty(bindPath).PropertyType;
                        }
                    }

                    switch (type == typeof(DateTime))
                    {
                        case true:
                            result = (b.StringFormat == "dd.MM.yyyy") ? "System.DateOnly" : "System.DateTime";
                            break;

                        default:
                            result = type.FullName;
                            break;
                    }
                }
            }

            return result;
        }

        public DataGridRow GetRow(int rowIndex)
        {
            //возвращает DataGridRow по индексу rowIndex
            if (!(this.ItemContainerGenerator.ContainerFromIndex(rowIndex) is DataGridRow result))
            {
                //rowIndex за пределами видимости
                this.UpdateLayout();
                this.ScrollIntoView(this.Items[rowIndex]);

                result = this.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            }

            return result;
        }

        public DataGridCell GetCell(int rowIndex, int columnIndex)
        {
            //возвращает DataGridCell по принятым индексам rowIndex, columnIndex
            DataGridRow row = this.GetRow(rowIndex);

            if (row != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);

                if (presenter == null)
                {
                    this.ScrollIntoView(row, this.Columns[columnIndex]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(row);
                }

                DataGridCell result = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;

                return result;
            }

            return null;
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            //если в нашем DataGrid выбран Item - будем на него ставить фокус ввода. сам DataGrid этого никогда не делает, что весьма погано
            if (this.SelectedIndex != -1)
            {
                object selectedItem = this.Items[this.SelectedIndex];

                if (selectedItem != null)
                {
                    //при включенной виртуализации DataGrid ItemContainerGenerator.ContainerFromItem будет возвращать Container только для видимых в нём items, а для items не видимых в DataGrid будет возвращать null. вызов UpdateLayout решает эту проблему
                    this.UpdateLayout();
                    this.ScrollIntoView(selectedItem);

                    DataGridRow selectedRow = this.ItemContainerGenerator.ContainerFromItem(selectedItem) as DataGridRow;

                    if (selectedRow != null)
                    {
                        FocusManager.SetIsFocusScope(selectedRow, true);
                        FocusManager.SetFocusedElement(selectedRow, selectedRow);
                    }
                }
            }
        }
    }

    /*
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

                switch (newValue == this.value.ToString())
                {
                    case true:
                        //this.comparisonCorrected = "=";
                        this.valueCorrected = this.value.ToString();
                        break;

                    default:
                        this.comparison = "=";
                        this.comparisonCorrected = " LIKE ";
                        this.valueCorrected = newValue;
                        break;
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

        public void DeleteEmptyFilters()
        {
            //удаляет фильтры, значения которых не определены - равны null
            this.Where(l => l.value == null).ToList().All(i => this.Remove(i));
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
    */

    /*
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
    */

    public class CustomComparer<T> : IComparer<T>
    {
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;

        private enum ChunkType
        {
            Alphanumeric,
            Numeric
        };

        public CustomComparer(ListSortDirection sortDirection)
        {
            this.SortDirection = sortDirection;
        }

        private bool InChunk(char ch, char otherCh)
        {
            ChunkType type = char.IsDigit(otherCh) ? ChunkType.Numeric : ChunkType.Alphanumeric;

            if ((type == ChunkType.Alphanumeric && char.IsDigit(ch)) || (type == ChunkType.Numeric && !char.IsDigit(ch)))
                return false;

            return true;
        }

        public int Compare(T x, T y)
        {
            if ((x == null) || (y == null))
            {
                if ((x == null) & (y != null))
                    return SortDirection == ListSortDirection.Ascending ? 1 : -1;

                if ((x != null) & (y == null))
                    return SortDirection == ListSortDirection.Ascending ? -1 : 1;

                return 0;
            }
            else
            {
                string xs = x as string;
                string ys = y as string;

                if ((xs == null) || (ys == null))
                    return 0;

                int xIndex = 0;
                int yIndex = 0;

                long xNumericChunk = 0;
                long yNumericChunk = 0;

                while ((xIndex < xs.Length) || (yIndex < ys.Length))
                {
                    if (xIndex >= xs.Length)
                        return SortDirection == ListSortDirection.Ascending ? -1 : 1;
                    else
                    {
                        if (yIndex >= ys.Length)
                            return SortDirection == ListSortDirection.Ascending ? 1 : -1;
                    }

                    char xCh = xs[xIndex];
                    char yCh = ys[yIndex];

                    StringBuilder xChunk = new StringBuilder();
                    StringBuilder yChunk = new StringBuilder();

                    while ((xIndex < xs.Length) && (xChunk.Length == 0 || InChunk(xCh, xChunk[0])))
                    {
                        xChunk.Append(xCh);
                        xIndex++;

                        if (xIndex < xs.Length)
                            xCh = xs[xIndex];
                    }

                    while ((yIndex < ys.Length) && (yChunk.Length == 0 || InChunk(yCh, yChunk[0])))
                    {
                        yChunk.Append(yCh);
                        yIndex++;

                        if (yIndex < ys.Length)
                            yCh = ys[yIndex];
                    }

                    int result = 0;

                    //оба куска записаны цифрами, сортируем их как цифры
                    if (char.IsDigit(xChunk[0]) && char.IsDigit(yChunk[0]))
                    {
                        xNumericChunk = Convert.ToInt64(xChunk.ToString());
                        yNumericChunk = Convert.ToInt64(yChunk.ToString());

                        if (xNumericChunk < yNumericChunk)
                            result = SortDirection == ListSortDirection.Ascending ? -1 : 1;

                        if (xNumericChunk > yNumericChunk)
                            result = SortDirection == ListSortDirection.Ascending ? 1 : -1;
                    }
                    else
                        result = SortDirection == ListSortDirection.Ascending ? xChunk.ToString().CompareTo(yChunk.ToString()) : yChunk.ToString().CompareTo(xChunk.ToString());

                    if (result != 0)
                        return result;
                }

                return 0;
            }
        }
    }

    public class ObjectForFilter
    {
        private string fieldName;
        public string FieldName
        {
            get { return fieldName; }

            set { fieldName = value; }
        }

        private string fieldValue;
        public string FieldValue
        {
            get { return fieldValue; }

            set { fieldValue = value; }
        }

        private object obj;
        public object Obj
        {
            get { return obj; }

            set { obj = value; }
        }
    }
}