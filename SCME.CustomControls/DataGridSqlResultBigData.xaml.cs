using AlphaChiTech.Virtualization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SCME.CustomControls
{
    /// <summary>
    /// Interaction logic for DataGridSqlResultBigData.xaml
    /// </summary>
    public partial class DataGridSqlResultBigData : DataGrid
    {
        private Button FbtFilterClicked = null;

        //последний HeaderText по которому был клик
        //все HeaderText столбцов имеют уникальные значения - это обеспечивает реализация Routines.SetToQueueCreateColumnInDataGrid
        private string FLastHeaderTextClicked = null;
        public string LastHeaderTextClicked
        {
            get
            {
                return this.FLastHeaderTextClicked;
            }
        }

        //имя столбца в базе данных по которому был клик
        private string FLastSourceFieldNameClicked = null;
        public string LastSourceFieldNameClicked
        {
            get
            {
                return this.FLastSourceFieldNameClicked;
            }
        }

        private ListSortDirection FLastSortedDirection = ListSortDirection.Ascending;
        public ListSortDirection LastSortedDirection
        {
            get { return FLastSortedDirection; }
        }

        //флаг скроллинг после выполнения сортировки выполнен до записи с не NULL значенеим в поле сортировки
        //null   - сортировки нет, скроллинг не нужен
        // false - еще не выполнен, требуется его выполнение;
        // true  - был выполнен
        private bool? FScrolledAfterSortingToNotNullValueRecord = null;
        public bool? ScrolledAfterSortingToNotNullValueRecord
        {
            get { return FScrolledAfterSortingToNotNullValueRecord; }
            set { FScrolledAfterSortingToNotNullValueRecord = value; }
        }

        //механизм обратного вызова реализации вызова формы ввода описания фильтров данных
        public delegate void SetFilter(Point position, DataGridColumnHeader columnHeader);
        public SetFilter SetFilterHandler { get; set; }

        public DataGridSqlResultBigData()
        {
            InitializeComponent();

            //VirtualizationManager будет использовать тот же поток, что использует Application.Current.Dispatcher
            VirtualizationManager.Instance.UIThreadExcecuteAction = a => Application.Current.Dispatcher.Invoke(a);

            //создаём и запускаем таймер для освобождения памяти, которую захватил VirtualizationManager
            DispatcherTimer dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, delegate { VirtualizationManager.Instance.ProcessActions(); }, this.Dispatcher);
            dispatcherTimer.Start();
        }

        private DynamicObj SelItem()
        {
            System.Collections.IList items = this.SelectedItems;

            //различаем ситуации: выбрана строка и выбрана ячейка
            if (items != null)
            {
                if (items.Count > 0)
                {
                    return items[0] as DynamicObj;
                }
                else
                {
                    IList<DataGridCellInfo> selCells = this.SelectedCells;

                    if ((selCells != null) && (selCells.Count != 0))
                    {
                        DataGridCellInfo cellInfo = this.SelectedCells[0];

                        if (cellInfo != null)
                        {
                            FrameworkElement cellContent = cellInfo.Column.GetCellContent(cellInfo.Item);

                            return (cellContent == null) ? null : cellContent.DataContext as DynamicObj;
                        }
                    }
                }
            }

            return null;
        }

        public object ValueFromSelectedRow(string sourceFieldName)
        {
            //возвращает object из строки DataGrid которую выбрал пользователь
            DynamicObj item = this.SelItem();

            if (item != null)
            {
                if (item.GetMember(sourceFieldName, out object result))
                    return result;
            }

            return null;
        }

        public object ValueFromFirstRow(string sourceFieldName)
        {
            //возвращает object из первой строки которую отображает DataGrid
            if (this.Items.Count > 0)
            {
                if (this.Items[0] is DynamicObj item)
                {
                    if (item.GetMember(sourceFieldName, out object result))
                        return result;
                }
            }

            return null;
        }

        private DataGridColumn DataGridColumnByHeaderText(string headerText)
        {
            //вычисляет DataGridColumn по тексту который содержится в его Header
            if (
                (from column in this.Columns
                 where column.Header.ToString() == headerText
                 select column
                ).FirstOrDefault() is DataGridColumn dgColumn)
                return dgColumn;

            return null;
        }

        private DataGridColumnHeader GetColumnHeaderFromColumn(DataGridColumn column)
        {
            if (column != null)
            {
                List<DataGridColumnHeader> columnHeaders = GetVisualChildCollection<DataGridColumnHeader>(this);

                foreach (DataGridColumnHeader columnHeader in columnHeaders)
                {
                    if (columnHeader.Column == column)
                        return columnHeader;
                }
            }

            return null;
        }

        private DataGridColumnHeader DataGridColumnHeaderByHeaderText(string headerText)
        {
            //вычисляет DataGridColumnHeader по тексту который он содержит
            //все DataGridColumnHeader содержат уникальные текстовые значения
            DataGridColumn dgColumn = this.DataGridColumnByHeaderText(headerText);
            DataGridColumnHeader result = this.GetColumnHeaderFromColumn(dgColumn);

            return result;
        }

        private void HideVisualSorted(DataGridColumnHeader currentColumnHeader)
        {
            //сброс визуальных признаков сортировки отображаемых данных          
            //входной параметр currentColumnHeader:
            // если задан, то выполняет сброс визуальных признаков сортировки отображаемых данных именно в принятом currentColumnHeader
            // если не задан - данная реализация сама его вычисляет по тексту this.LastHeaderTextClicked
            if (this.LastHeaderTextClicked != null)
            {
                DataGridColumnHeader columnHeader = currentColumnHeader ?? this.DataGridColumnHeaderByHeaderText(this.LastHeaderTextClicked);

                if (columnHeader != null)
                {
                    Path founded = FindChild<Path>(columnHeader, "PathArrowUp");

                    if (founded != null)
                        founded.Visibility = Visibility.Collapsed;

                    founded = FindChild<Path>(columnHeader, "PathArrowDown");

                    if (founded != null)
                        founded.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void VisualizeSorting(DataGridColumnHeader currentColumnHeader)
        {
            //визуализация применённой сортировки
            //проверяем является ли принятый currentColumnHeader тем DataGridColumnHeader по которому пользователь выполнил сортироку
            if ((currentColumnHeader != null) && (currentColumnHeader.Content != null))
            {
                if ((this.LastHeaderTextClicked != null) && (this.LastHeaderTextClicked == currentColumnHeader.Content.ToString()))
                {
                    DataGridColumnHeader columnHeaderLastClicked = currentColumnHeader;
                    Path founded = null;

                    if (columnHeaderLastClicked != null)
                    {
                        founded = FindChild<Path>(columnHeaderLastClicked, "PathArrowUp");
                        if (founded != null)
                            founded.Visibility = Visibility.Collapsed;

                        founded = FindChild<Path>(columnHeaderLastClicked, "PathArrowDown");
                        if (founded != null)
                            founded.Visibility = Visibility.Collapsed;

                        if (this.Items.Count > 0)
                        {
                            switch (this.FLastSortedDirection)
                            {
                                case (ListSortDirection.Ascending):
                                    founded = FindChild<Path>(columnHeaderLastClicked, "PathArrowUp");
                                    break;

                                case (ListSortDirection.Descending):
                                    founded = FindChild<Path>(columnHeaderLastClicked, "PathArrowDown");
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
            }
        }

        private void BtFilter_Click(object sender, RoutedEventArgs e)
        {
            //выставляем флаг о прошедшем нажатии кнопки
            if (sender is Button button)
            {
                this.FbtFilterClicked = button;

                //после применения фильтрации в кеше могут появиться данные, которых там не было на момент сортировки (которая была выполнена до фильтрации) - данные будут уже не сортированными
                //убираем визуализацию ранее выполненной сортировки
                this.HideVisualSorted(null);

                //забываем данные последней сортировки
                this.FLastHeaderTextClicked = null;
                this.FLastSourceFieldNameClicked = null;
                this.FLastSortedDirection = ListSortDirection.Ascending;
            }
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridColumnHeader currentColumnHeader)
            {
                if (this.FbtFilterClicked == null)
                {
                    //случай сортировки                   
                    //запоминаем имя столбца сортировки и его направление
                    DataGridColumnHeader columnHeaderLastClicked = this.DataGridColumnHeaderByHeaderText(this.LastHeaderTextClicked);

                    if (currentColumnHeader == columnHeaderLastClicked)
                    {
                        this.FLastSortedDirection = (this.FLastSortedDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;
                    }
                    else
                    {
                        this.FLastSortedDirection = ListSortDirection.Ascending;
                        this.HideVisualSorted(columnHeaderLastClicked);
                    }

                    if (this.Items.Count > 0)
                    {
                        //запоминаем Header столбца по которому пользователь сделал клик мышью
                        this.FLastHeaderTextClicked = currentColumnHeader.Content.ToString();

                        //запоминаем SourceFieldName столбца по которому пользователь сделал клик мышью
                        DataGridColumn dgColumn = this.DataGridColumnByHeaderText(this.FLastHeaderTextClicked);
                        this.FLastSourceFieldNameClicked = Common.Routines.SourceFieldNameByColumn(dgColumn);

                        //показываем поле и направление сортировки
                        this.VisualizeSorting(currentColumnHeader);

                        //сбрасываем флаг о выполнении скроллинга до первой записи с не NULL значением в поле сортировки
                        this.FScrolledAfterSortingToNotNullValueRecord = false;
                    }
                }
                else
                {
                    //случай нажатия кнопки фильтра
                    Point position = this.FbtFilterClicked.PointToScreen(new Point(0d, 0d));
                    position.Y += currentColumnHeader.Height;

                    //сбрасываем флаг о прошедшем нажатии кнопки фильтра
                    this.FbtFilterClicked = null;

                    this.SetFilterHandler(position, currentColumnHeader);
                }
            }
        }

        private void ColumnHeader_Loaded(object sender, RoutedEventArgs e)
        {
            //показываем направление и признак сортировки в столбце по которому отсортированы отображаемые данные
            //чтобы иметь возможность найти DataGridColumnHeader используем данное событие
            //если искать DataGridColumnHeader сразу после создания столбца - DataGridColumnHeader никогда не будет найден
            if (sender is DataGridColumnHeader columnHeader)
                this.VisualizeSorting(columnHeader);
        }

        public List<T> GetVisualChildCollection<T>(object parent) where T : Visual
        {
            List<T> visualCollection = new List<T>();
            GetVisualChildCollection(parent as DependencyObject, visualCollection);

            return visualCollection;
        }

        private void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T)
                {
                    visualCollection.Add(child as T);
                }
                else
                {
                    if (child != null)
                        GetVisualChildCollection(child, visualCollection);
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
                if (!(child is T childType))
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null)
                        break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    // If the child's name is set for search
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
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
}
