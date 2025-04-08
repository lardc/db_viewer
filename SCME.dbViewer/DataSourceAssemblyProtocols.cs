using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using SCME.CustomControls;
using SCME.CustomControls.FilterAndSort;
using SortDescription = SCME.CustomControls.FilterAndSort.SortDescription;
using FilterDescription = SCME.CustomControls.FilterAndSort.FilterDescription;

namespace SCME.dbViewer
{
    public class DataProviderAssemblyProtocols : IFilteredSortedSourceProviderAsync
    {
        public DataProviderAssemblyProtocols(CacheEdit cacheEditHandler, CacheBuildCompleted cacheBuildCompletedHandler, PortionDataLoaded portionDataLoadedHandler)
        {
            this.FCacheEditHandler = cacheEditHandler;
            this.FCacheBuildCompletedHandler = cacheBuildCompletedHandler;
            this.FPortionDataLoadedHandler = portionDataLoadedHandler;

            //поставлять данные нам будет база данных - устанавливаем к ней подключение
            SqlConnection connection = SCME.Types.DBConnections.Connection;

            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        /*
        //последнее значение смещения
        private int FLastOffSet = 0;
        public int LastOffSet
        {
            get { return FLastOffSet; }
        }

        private int FPortionSize = 0;
        public int PortionSize
        {
            get { return FPortionSize; }
        }
        
        private List<DynamicObj> FLastDataPortion = null;
        public List<DynamicObj> LastDataPortion
        {
            get { return FLastDataPortion; }
        }
        */

        public Collection<DynamicObj> FCollection;       

        //механизм обратного вызова кода для изменения данных кеша (реализация сортировки и фильтрации)
        public delegate int CacheEdit();
        private readonly CacheEdit FCacheEditHandler;
        private CacheEdit CacheEditHandler { get { return this.FCacheEditHandler; } }

        //механизм обратного вызова кода, который может быть исполнен только по факту завершения формирования данных в кеше - например по факту завершения работы фильтров
        public delegate void CacheBuildCompleted(int cacheSize);
        private readonly CacheBuildCompleted FCacheBuildCompletedHandler;
        private CacheBuildCompleted CacheBuildCompletedHandler { get { return this.FCacheBuildCompletedHandler; } }

        //механизм обратного вызова кода, который может быть исполнен только по факту загрузки порции данных из кеша
        public delegate void PortionDataLoaded();
        private readonly PortionDataLoaded FPortionDataLoadedHandler;
        private PortionDataLoaded PortionDataLoadedHandler { get { return this.FPortionDataLoadedHandler; } }               

        public bool Contains(DynamicObj item)
        {
            return this.FItems.Contains(item);
        }

        #region fields

        private readonly List<DynamicObj> FItems = new List<DynamicObj>();
        private readonly List<DynamicObj> FOrderedItems = new List<DynamicObj>();
        private bool FIsFilteredItemsValid;
        private string FOrderByLinqExpression = "";
        private string FWhereLinqExpression = "";

        #endregion

        private void FillPortionData(System.Collections.IList listOfItems, System.Data.SqlClient.SqlDataReader reader)
        {
            //формируем запрошенный набор данных
            DynamicObj item = new DynamicObj();
            Routines.ValuesToRowAssemblyProtocols(reader, item);
            listOfItems.Add(item);
        }        

        public List<DynamicObj> GetItemsAt(int offSet, int portionSize)
        {
            //получение порции данных размером count записей со смещением относительно начальной записи pageOffSet
            List<DynamicObj> result = new List<DynamicObj>();

            lock (this)
            {
                //загрузка запрошенного списка протоколов сборки
                SCME.Types.DbRoutines.CacheAssemblyProtocolsReadData(this.FillPortionData, result, offSet, portionSize, Common.Constants.cString_AggDelimeter);
            }

            /*
            //вся порция данных успешно загружена из базы данных - запоминаем значение смещения offSet и размер порции данных portionSize
            //используется при сортировке данных
            this.FLastOffSet = offSet;
            this.FPortionSize = portionSize;
            this.FLastDataPortion = result;
            */

            //вся порция данных успешно загружена, запускаем реализацию которая должна отработать по факту загрузки порции данных
            this.PortionDataLoadedHandler?.Invoke();

            return result;
        }

        /*
        public void ReloadLastDataPortion()
        {
            //перечитываем из базы данных последнюю загруженную порцию данных
            SCME.Types.DbRoutines.CacheReadData(null, this.ReloadPortionData, this.FLastDataPortion, this.FLastOffSet, this.FPortionSize, Common.Constants.cString_AggDelimeter);
        }
        */

        public int Count()
        {
            //получение количества записей всего набора данных
            int result = -1;

            lock (this)
            {
                //получаем количество протоколов сборки кеша
                result = SCME.Types.DbRoutines.CacheAssemblyProtocolsBuild(true);

                //вызываем реализацию сортировки и фильтрации
                if (this.CacheEditHandler != null)
                {
                    //в случае фильтрации данных из кеша будут удалены записи, не удовлетворяющие критериям фильтрации - поэтому корректируем количество записей кеша
                    int deletedCount = this.CacheEditHandler.Invoke();
                    result -= deletedCount;
                }
            }

            //кеш сформирован - исполняем реализацию, которую надо исполнять по факту завершения формирования данных в кеше
            this.CacheBuildCompletedHandler?.Invoke(result);

            return result;
        }

        #region properties


        public IList<DynamicObj> FilteredOrderedItems
        {
            get
            {
                return null;                
            }

        }

        public string OrderByLinqExpression
        {
            get { return this.FOrderByLinqExpression; }
            set
            {
                if (!string.Equals(this.FOrderByLinqExpression, value))
                {
                    this.FOrderByLinqExpression = value;
                    this.FIsFilteredItemsValid = false;
                }
            }
        }

        public string WhereLinqExpression
        {
            get { return this.FWhereLinqExpression; }
            set
            {
                if (!string.Equals(this.FWhereLinqExpression, value))
                {
                    this.FWhereLinqExpression = value;
                    this.FIsFilteredItemsValid = false;
                }
            }
        }
        #endregion

        #region public members

        public void OrderBy(string orderByExpression)
        {
            if (!string.Equals(orderByExpression, this.OrderByLinqExpression))
                this.OrderByLinqExpression = orderByExpression;
        }

        public void Where(string whereExpression)
        {
            if (!string.Equals(whereExpression, this.WhereLinqExpression)) this.WhereLinqExpression = whereExpression;
        }

        #endregion

        #region filter & sort Description list

        public SortDescriptionList SortDescriptionList { get; } = new SortDescriptionList();

        public FilterDescriptionList FilterDescriptionList { get; } = new FilterDescriptionList();

        private void SortDescriptionListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            string sort = "";

            bool sortFound = false;
            foreach (SortDescription sortDescription in this.SortDescriptionList)
            {
                if (sortFound)
                    sort += ", ";

                sortFound = true;

                sort += sortDescription.PropertyName;
                sort += (sortDescription.Direction == ListSortDirection.Ascending) ? " ASC" : " DESC";
            }

            //if ((!sortFound) && (!string.IsNullOrWhiteSpace( primaryKey )))
            //  sort += primaryKey + " ASC";

            this.OrderByLinqExpression = sort;
        }

        private void FilterDescriptionListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Reset)
            {
                string filter = "";

                bool filterFound = false;
                foreach (FilterDescription filterDescription in this.FilterDescriptionList)
                {
                    string subFilter = GetLinqQueryString(filterDescription);
                    if (!string.IsNullOrWhiteSpace(subFilter))
                    {
                        if (filterFound)
                            filter += " and ";
                        filterFound = true;
                        filter += " " + subFilter + " ";
                    }
                }

                this.WhereLinqExpression = filter;
            }
        }

        #region query builder

        private static readonly Regex _regexSplit = new Regex(@"(and)|(or)|(==)|(<>)|(!=)|(<=)|(>=)|(&&)|(\|\|)|(=)|(>)|(<)|(\*[\-_a-zA-Z0-9]+)|([\-_a-zA-Z0-9]+\*)|([\-_a-zA-Z0-9]+)", RegexOptions.IgnoreCase);

        private static readonly Regex _regexOp = new Regex(@"(and)|(or)|(==)|(<>)|(!=)|(<=)|(>=)|(&&)|(\|\|)|(=)|(>)|(<)", RegexOptions.IgnoreCase);

        private static readonly Regex _regexComparOp = new Regex(@"(==)|(<>)|(!=)|(<=)|(>=)|(=)|(>)|(<)", RegexOptions.None);

        private static string GetLinqQueryString(FilterDescription filterDescription)
        {
            string ret = "";

            if (!string.IsNullOrWhiteSpace(filterDescription.Filter))
            {
                // using user str + linq.dynamic
                try
                {
                    // xceed syntax : empty (contains), AND (uppercase), OR (uppercase), <>, * (end with), =, >, >=, <, <=, * (start with)
                    //    see http://doc.xceedsoft.com/products/XceedWpfDataGrid/Filter_Row.html 
                    // linq.dynamic syntax : =, ==, <>, !=, <, >, <=, >=, &&, and, ||, or, x.m(…) (where x is the attrib and m the function (ex: Contains, StartsWith, EndsWith ...)
                    //    see D:\DevC#\VirtualisingCollectionTest1\DynamicQuery\Dynamic Expressions.html 
                    // ex : RemoteOrDbDataSourceEmulation.Instance.Items.Where( "Name.Contains(\"e_1\") or Name.Contains(\"e_2\")" );

                    string exp = filterDescription.Filter;

                    // arrange expression

                    bool previousTermIsOperator = false;
                    foreach (Match match in _regexSplit.Matches(exp))
                    {
                        if (match.Success)
                        {
                            //TODO processing results
                            if (_regexOp.IsMatch(match.Value))
                            {
                                if (_regexComparOp.IsMatch(match.Value))
                                {
                                    // simple operator >, <, ==, != ...
                                    ret += " " + filterDescription.PropertyName + " " + match.Value;
                                    previousTermIsOperator = true;
                                }
                                else
                                {
                                    // and, or ...
                                    ret += " " + match.Value;
                                    previousTermIsOperator = false;
                                }
                            }
                            else
                            {
                                // Value
                                if (previousTermIsOperator)
                                {
                                    ret += " " + match.Value;
                                    previousTermIsOperator = false;
                                }
                                else
                                {
                                    if (match.Value.StartsWith("*"))
                                        ret += " " + filterDescription.PropertyName + ".EndsWith( \"" + match.Value.Substring(1) + "\" )";
                                    else if (match.Value.EndsWith("*"))
                                        ret += " " + filterDescription.PropertyName + ".StartsWith( \"" + match.Value.Substring(0, match.Value.Length - 1) + "\" )";
                                    else
                                        ret += " " + filterDescription.PropertyName + ".Contains( \"" + match.Value + "\" )";

                                    previousTermIsOperator = false;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return ret;
        }

        #endregion query builder

        #endregion filter & sort Descrioption list
    }
}
