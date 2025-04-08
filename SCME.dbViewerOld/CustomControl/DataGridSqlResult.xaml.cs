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

namespace SCME.dbViewer.CustomControl
{
    /// <summary>
    /// Interaction logic for DataGridSqlResult.xaml
    /// </summary>
    public partial class DataGridSqlResult : DataGrid
    {
        private DataTable dataTable = null;
        private DataGridColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastSortedDirection = ListSortDirection.Ascending;
        private Button btFilterClicked = null;
        private ActiveFilters activeFilters;

        public DataGridSqlResult() : base()
        {
            InitializeComponent();

            activeFilters = new ActiveFilters();
        }

        private void btFilter_Click(object sender, RoutedEventArgs e)
        {
            //выставляем флаг о прошедшем нажатии кнопки
            btFilterClicked = (Button)sender;
        }

        public void ClearColumns()
        {
            this.Columns.Clear();
        }

        public DataGridColumn NewColumn(string header, string bindPath)
        {
            //создание нового столбца
            //header - то как пользователь будет видеть название этого столбца
            //bindPath - столбец будет отображать данные столбца binding из доступного списка столбцов

            DataGridTextColumn textColumn = new DataGridTextColumn();
            textColumn.Header = header;
            textColumn.Binding = new Binding(bindPath);
            this.Columns.Add(textColumn);

            return textColumn;
        }

        private void SetFormatForColumns()
        {
            string temp;

            foreach (DataGridTextColumn column in this.Columns)
            {
                //для даты будем использовать свой формат вывода
                if (DataTypeByColumn(column, out temp) == typeof(DateTime))
                    column.Binding.StringFormat = ("dd.MM.yyyy HH:mm:ss");
            }
        }

        public void ViewSqlResult(SqlConnection connection, string SqlQuery)
        {
            //отображение результата выполнения запроса sqlQuery

            try
            {
                var dataAdapter = new SqlDataAdapter(SqlQuery, connection);
                var commandBuilder = new SqlCommandBuilder(dataAdapter);
                var ds = new System.Data.DataSet();
                dataAdapter.Fill(ds);

                bool NeedSetFormat = (dataTable == null);
                dataTable = ds.Tables[0];

                //формат отображения задаётся для столбцов один единственный раз
                if (NeedSetFormat)
                    SetFormatForColumns();

                DataView dv = dataTable.DefaultView;
                this.ItemsSource = dv;

                dv.RowFilter = this.FiltersToString();

                if (this.Items.Count > 0)
                    this.SelectedIndex = 0;
            }
            finally
            {
                connection.Close();
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
                    type = dataTable?.Columns[bindPath]?.DataType;
                }
            }

            return type;
        }

        private void columnHeader_Click(object sender, RoutedEventArgs e)
        {
            var columnHeader = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;

            if (btFilterClicked != null)
            {
                //мы начали обработку нажатия кнопки фильтра, поэтому сбрасываем флаг о прошедшем нажатии кнопки фильтра
                Point position = btFilterClicked.PointToScreen(new Point(0d, 0d));
                position.Y += columnHeader.Height;

                btFilterClicked = null;

                DataGridTextColumn textColumn = (DataGridTextColumn)columnHeader.Column;

                string bindPath;
                Type filterType = DataTypeByColumn(textColumn, out bindPath);

                SetFilter(position, filterType, columnHeader.Content.ToString(), bindPath);
            }
            else
            {
                //вычисляем поле сортировки и его направления
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
        }

        private string SelectedText(string bindPath)
        {
            //возвращает выделенный в DataGrid текст
            switch (this.SelectedItems == null)
            {
                case (true):
                    return string.Empty;

                default:
                    {
                        System.Collections.IList rows = this.SelectedItems;

                        string Result = "";

                        if (rows.Count != 0)
                        {
                            DataRowView row = (DataRowView)rows[0];
                            Result = row[bindPath].ToString();
                        }

                        return Result;
                    }
            }
        }

        private string FiltersToString()
        {
            string result = string.Empty;

            this.activeFilters.Correct();

            foreach (FilterDescription f in this.activeFilters)
            {
                if (result != string.Empty)
                {
                    if (f.type == typeof(string))
                    {
                        switch (this.activeFilters.FieldNameStoredMoreThanOnce(f.fieldName))
                        {
                            case true:
                                {
                                    //по полю f.fieldName определено более одного фильтра
                                    switch (f.valueCorrected.Contains("%"))
                                    {
                                        case true:
                                            result += " AND ";
                                            break;

                                        default:
                                            result += " OR ";
                                            break;
                                    }
                                }
                                break;

                            default:
                                //по полю f.fieldName определён один единственный фильтр
                                result += " AND ";
                                break;
                        }
                    }

                    if (f.type == typeof(DateTime))
                        result += " AND ";
                }

                if ((Type)f.type == typeof(string))
                    result += string.Format("{0}{1}'{2}'", f.fieldName, f.comparisonCorrected, f.valueCorrected);

                if ((Type)f.type == typeof(DateTime))
                {
                    DateTime date = DateTime.Parse(f.value.ToString());

                    switch (f.comparison)
                    {
                        case "<":
                        case "<=":
                            result += string.Format("{0}{1}'{2}'", f.fieldName, f.comparison, date.ToShortDateString()); //new DateTime(date.Year, date.Month, date.Day, 23, 59, 59));
                            break;

                        case "=":
                        case ">":
                        case ">=":
                            result += string.Format("{0}{1}'{2}'", f.fieldName, f.comparison, date.ToShortDateString()); //new DateTime(date.Year, date.Month, date.Day, 00, 00, 00));
                            break;

                        default:
                            throw new Exception(string.Format("FiltersToString(). No processing is provided for f.comparison='{0}'.", f.comparison));
                    }
                }
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
                    FilterDescription filter = new FilterDescription { type = type, tittlefieldName = tittlefieldName, fieldName = bindPath, comparison = "=", value = SelectedText(bindPath) };
                    this.activeFilters.Add(filter);

                    FiltersInput fmFiltersInput = new FiltersInput(this.activeFilters);
                    fmFiltersInput.Owner = Application.Current.MainWindow;

                    fmFiltersInput.Demonstrate(position);
                    dv.RowFilter = this.FiltersToString();

                    if (this.Items.Count > 0)
                        this.SelectedIndex = 0;
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
        public T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

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
                    if (foundChild != null) break;
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
            string newValue = value.ToString().Replace("*", "%");

            if (this.type == typeof(string))
            {
                switch (newValue == this.value.ToString())
                {
                    case true:
                        this.comparison = "=";
                        this.comparisonCorrected = "=";

                        this.valueCorrected = value.ToString();
                        break;

                    default:
                        this.comparisonCorrected = " LIKE ";
                        this.valueCorrected = newValue;
                        break;
                }
            }

            if (this.type == typeof(DateTime))
            {
                if (this.fieldName == "TS")
                    this.fieldName = "TSZEROTIME";
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

                switch (sValue)
                {
                    case "":
                        this.Remove(f);
                        break;

                    default:
                        f.Correct();
                        break;
                }
            }
        }
    }
}
