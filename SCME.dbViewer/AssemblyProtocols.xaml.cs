using AlphaChiTech.Virtualization;
using SCME.CustomControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SCME.Types;
using System.Collections.Concurrent;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for AssemblyProtocols.xaml
    /// </summary>
    public partial class AssemblyProtocols : Window
    {
        private DataProviderAssemblyProtocols FDataProvider;
        private DataSourceProxyAssemblyProtocols FDataSourceProxy;

        private Collection<DynamicObj> FDataSource = null;
        public Collection<DynamicObj> DataSource
        {
            get
            {
                if (this.FDataSource == null)
                {
                    this.FDataProvider = new DataProviderAssemblyProtocols(this.CacheEdit, this.AfterBuildingDataInCacheRoutines, this.AfterPortionDataLoadedRoutines);

                    this.FDataSourceProxy = new DataSourceProxyAssemblyProtocols(this.FDataProvider);
                    PaginationManager<DynamicObj> paginationManager = new PaginationManager<DynamicObj>(this.FDataSourceProxy, pageSize: 80, maxPages: 2);
                    this.FDataSource = new Collection<DynamicObj>(paginationManager);

                    this.FDataProvider.FCollection = this.FDataSource;
                }

                return this.FDataSource;
            }
        }

        private ConcurrentQueue<Action> FQueueManager = new ConcurrentQueue<Action>();
        private CustomControls.ActiveFilters FActiveFilters = null;

        //столбец из this.DgAssemblyProtocols по которому пользователь хочет выполнить сортировку
        private string FSortSourceFieldName = null;

        //дескриптор окна визуализации ожидания
        //создаётся в SCME.dbViewer.MainWindow
        private IntPtr FProcessWaitVisualizerHWnd = IntPtr.Zero;
        private IntPtr ProcessWaitVisualizerHWnd
        {
            get { return this.FProcessWaitVisualizerHWnd; }
        }

        public AssemblyProtocols(IntPtr processWaitVisualizerHWnd)
        {
            InitializeComponent();

            //создаём таймер, который будет анализировать очередь this.FQueueManager извлекать из неё сообщения и обрабатывать их
            this.CreateQueueWorker();

            //создаём место хранения списка фильтров в котором пользователь будет создавать, изменять и удалять свои фильтры
            this.FActiveFilters = new CustomControls.ActiveFilters() { OnChangedListOfFiltersHandler = null };

            //передаём в this.DgAssemblyProtocols ссылку на реализацию вызова редактора фильтров
            this.DgAssemblyProtocols.SetFilterHandler = this.SetFilter;

            this.FProcessWaitVisualizerHWnd = processWaitVisualizerHWnd;
        }

        private void CreateQueueWorker()
        {
            //создаём механизм исполнения отложенной очереди создания столбцов отображения conditions/parameters в потоке пользовательского интерфейса
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 0, 0, 333)
            };

            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            //обрабатываем отложенную очередь вызовов создания столбцов отображения conditions и parameters
            while (this.FQueueManager.TryDequeue(out Action act))
                act.Invoke();

            //вся очередь отложенных вызовов исполнена
            if (this.FQueueManager.Count == 0)
            {
                //при выполнении процедуры сортировки выполняется this.DgAssemblyProtocols.IsEnabled = false
                //делаем пользователю доступным DgAssemblyProtocols т.к. операция сортировки уже окончена
                //запрет на работу пользователя с this.DgAssemblyProtocols нужен для исключения взаимной блокировки транзакций, которая может возникуть если пользователь не дождавшись результата уже выполняющейся сортировки попытается выполнить новую сортировку
                this.DgAssemblyProtocols.IsEnabled = true;

                //больше не надо демонстрировать пользователю необходимость ожидания
                SCME.Common.Routines.HideProcessWaitVisualizerSortingFiltering(this.ProcessWaitVisualizerHWnd);
            }
        }

        public bool ShowModal(out int assemblyProtocolDescr)
        {
            bool result = false;

            switch (this.ShowDialog() == true)
            {
                case true:
                    object objAssemblyProtocolDescr = this.DgAssemblyProtocols.ValueFromSelectedRow(Common.Constants.Descr);
                    assemblyProtocolDescr = (objAssemblyProtocolDescr == null) ? -1 : int.TryParse(objAssemblyProtocolDescr.ToString(), out int id) ? id : -1;

                    if (assemblyProtocolDescr != -1)
                        result = true;

                    break;

                default:
                    assemblyProtocolDescr = -1;
                    break;
            }

            //работа с формой отображающей список протоколов сборки завершена - кеш который стоял за этой формой более не нужен
            DbRoutines.CacheAssemblyProtocolsFree();

            return result;
        }

        private string DataTypeByColumn(DataGridColumn column, out string soureFieldName)
        {
            //определение типа данных, отображаемых в столбце column
            //судить о типе всех данных по порции данных нельзя - в порции данных может не быть ни одного значения, тип данных не вычислим - поэтому никак не используем данные для вычисления типа данных
            //но нам всегда известно что за данные могут быть в данном столбце, т.к. они всегда грузятся только из базы данных, а типы данных в ней чётко заданы
            string result = null;
            soureFieldName = null;

            if (column != null)
            {
                soureFieldName = column.SortMemberPath;

                switch (soureFieldName)
                {
                    case Common.Constants.AssemblyProtocolID:
                    case Common.Constants.Descr:
                    case Common.Constants.AverageCurrent:
                    case Common.Constants.DeviceClass:
                    case Common.Constants.DUdt:
                    case Common.Constants.Qrr:
                    case Common.Constants.Omnity:
                        result = typeof(int).FullName;
                        break;

                    case Common.Constants.Ts:
                        result = "System.DateOnly";
                        break;

                    case Common.Constants.Usr:
                    case Common.Constants.AssemblyJob:
                    case Common.Constants.DeviceTypeRU:
                    case Common.Constants.DeviceTypeEN:
                    case Common.Constants.Constructive:
                    case Common.Constants.Tq:
                    case Common.Constants.Climatic:
                        result = typeof(string).FullName;
                        break;

                    case Common.Constants.DeviceModeView:
                    case Common.Constants.Export:
                        result = typeof(bool).FullName;
                        break;

                    case Common.Constants.Trr:
                    case Common.Constants.Tgt:
                        result = typeof(double).FullName;
                        break;

                    default:
                        result = typeof(string).FullName;
                        break;
                }
            }

            return result;
        }

        private void ApplyFilters()
        {
            //применение установленных пользователем фильтров к отображаемым данным

            //показываем пользователю: надо ждать результат фильтрации
            SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

            //фильтрация предполагает поиск нужных пользователю данных
            //чтобы фильтрованные данные всегда были актуальными уничтожаем кеш, в этом случае кеш будет построен на последних имеющихся данных - данные в кеше будут актуализированы
            DbRoutines.CacheAssemblyProtocolsFree();

            //применяем фильтры
            this.RefreshShowingData();
        }

        private void SetFilter(Point position, System.Windows.Controls.Primitives.DataGridColumnHeader columnHeader)
        {
            if (this.DgAssemblyProtocols.ItemsSource != null)
            {
                string filterType = this.DataTypeByColumn(columnHeader.Column, out string sourceFieldName);

                CustomControls.FilterDescription filter = new CustomControls.FilterDescription(this.FActiveFilters, sourceFieldName) { Type = filterType, TittlefieldName = columnHeader.Content.ToString(), Comparison = "=", Value = this.DgAssemblyProtocols.ValueFromSelectedRow(sourceFieldName) };
                this.FActiveFilters.Add(filter);

                FiltersInput fmFiltersInput = new FiltersInput(this.FActiveFilters, this);

                if (fmFiltersInput.Demonstrate(position) == true)
                    this.ApplyFilters();
            }
        }

        private int CacheSetOnDuty(byte comparison, string columnName, IEnumerable<string> values)
        {
            //процедура актуализации данных в кеше с целью выполнения фильтрации
            //возвращает количество отфильтрованных записей кеша (удалённых из кеша записей)
            return DbRoutines.CacheAssemblyProtocolsApplyFilter(columnName, comparison, values);
        }

        private void CacheSetOnDuty(string columnName, bool direction)
        {
            //процедура актуализации поля сортировки кеша
            DbRoutines.CacheAssemblyProtocolsSetSortingValue(columnName, direction);
        }

        private int CacheEdit()
        {
            //установка значений поля сортировки кеша и удаление из кеша записей не удовлетворяющих критериям фильтрации
            //данная реализация будет вызвана сразу после формирования данных кеша перед вызовом this.AfterBuildingDataInCacheRoutines
            //смотрим какие сортированные данные нам требуется получать: простые реквизиты, условия или параметры
            //возвращает сколько записей кеша удалил вызов данной реализации

            int deletedSummCount = 0;

            //требуется фильтрация данных                
            if (this.FActiveFilters.Count > 0)
            {
                byte comparison;
                string value;

                this.FActiveFilters.Correct();

                foreach (CustomControls.FilterDescription filter in this.FActiveFilters)
                {
                    comparison = Routines.ComparisonToByte(filter.ComparisonCorrected);

                    switch (filter.Type.ToString())
                    {
                        case "System.String":
                            value = filter.StringValueCorrected;
                            break;

                        case "System.Boolean":
                            if (filter.Value == null)
                            {
                                value = null;
                            }
                            else
                                value = ((bool)filter.Value) ? "1" : null;

                            break;

                        default:
                            value = filter.Value?.ToString();
                            break;
                    }

                    //удаляем из кеша записи которые не удовлетворяют условиям фильтрации
                    int deletedCount = this.CacheSetOnDuty(comparison, filter.FieldName, filter.Values.Values());

                    //вычисляем сколько было удалено записей (сумму)
                    deletedSummCount += deletedCount;

                    //фильтрация данных выполнена
                }
            }

            //требуется сортировка данных
            if (!string.IsNullOrEmpty(this.FSortSourceFieldName))
            {
                //вычисляем направление сортировки
                bool direction = (this.DgAssemblyProtocols.LastSortedDirection == ListSortDirection.Ascending) ? false : true;

                //устанавливаем значение поля сортировки в кеше
                this.CacheSetOnDuty(this.FSortSourceFieldName, direction);

                //сортировка данных выполнена
            }

            return deletedSummCount;
        }

        private void AfterBuildingDataInCacheRoutines(int cacheSize)
        {
            //показываем количество отображаемых записей

            //прокручиваем данные до первой записи не NULL значением в поле сортировки
            this.ScrollToFirstNotNullSortingValue();
        }

        private void ScrollToFirstNotNullSortingValue()
        {
            //прокручиваем данные в this.DgAssemblyProtocols до первой записи с не NULL значением в поле по которому выполнена сортировка
            //делаем это только один раз если это ещё не сделано
            if (this.DgAssemblyProtocols.ScrolledAfterSortingToNotNullValueRecord == false)
            {
                int rowNum = DbRoutines.FirstRowNumByNotNullSortingValueAssemblyProtocols();

                if (rowNum != -1)
                {
                    this.FQueueManager.Enqueue(
                                               delegate
                                               {
                                                   object item = this.DgAssemblyProtocols.Items[rowNum];

                                                   if (item != null)
                                                   {
                                                       this.DgAssemblyProtocols.UpdateLayout();
                                                       this.DgAssemblyProtocols.ScrollIntoView(item);

                                                       //устанавливаем флаг о прошедшем скроллинге
                                                       this.DgAssemblyProtocols.ScrolledAfterSortingToNotNullValueRecord = true;
                                                   }
                                               });
                }
            }
        }

        private void AfterPortionDataLoadedRoutines()
        {
            //вызывается по факту загрузки порции данных из кеша
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;
                this.RefreshData();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.DialogResult = false;
                    break;

                case Key.Enter:
                    this.DialogResult = true;
                    break;
            }
        }

        private void RefreshShowingData()
        {
            //обновление отображаемых данных из-за сортировки или фильтрации
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                //если у this.DgAssemblyProtocols на момент выполнения данной реализации выделена целая строка - данная реализация начинает бесконечно перегружать текущую страницу данных
                //почему так происходит не понял, избавится от этого можно сбросив выделение строки
                this.DgAssemblyProtocols.SelectedItem = null;

                //уничтожаем this.FDataSource для принудительного перечитывания данных в this.FDataSource
                this.FDataSource = null;
                this.DgAssemblyProtocols.ItemsSource = this.DataSource;
            }));
        }

        private void DgAssemblyProtocols_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (sender is DataGridSqlResultBigData dg)
            {
                //смотрим есть ли данные для сортировки
                if (this.FDataSource.Count <= 0)
                    return;

                //запрещаем пользователю выполнять сортировку пока не исполнена уже запрошенная им сортировка
                dg.IsEnabled = false;

                //показываем пользователю: надо ждать результат сортировки
                SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

                //запоминаем столбец по которому пользователь хочет выполнить сортировку отображаемого списка и режим изменения данных кеша
                this.FSortSourceFieldName = e.Column.SortMemberPath;

                //обновляем отображаемые данные чтобы увидеть результаты сортировки
                this.RefreshShowingData();

                e.Handled = true;
            }
        }

        private void DgAssemblyProtocols_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is System.Windows.Controls.Border))
                this.DialogResult = true;
        }

        private int? SelectedAssemblyProtocolID()
        {
            //возвращает текущий выбранный пользователем идентификатор протокола сборки
            //считываем идентификатор выбранного пользователем протокола сборки
            object objAssemblyProtocolID = this.DgAssemblyProtocols.ValueFromSelectedRow(Common.Constants.AssemblyProtocolID);

            return (objAssemblyProtocolID is int assemblyProtocolID) ? (int?)assemblyProtocolID : null;
        }

        private int? SelectedAssemblyProtocolDescr()
        {
            //возвращает текущий выбранный пользователем номер протокола сборки
            //считываем номер выбранного пользователем протокола сборки
            object objAssemblyProtocolDescr = this.DgAssemblyProtocols.ValueFromSelectedRow(Common.Constants.Descr);

            return int.TryParse(objAssemblyProtocolDescr.ToString(), out int assemblyProtocolDescr) ? (int?)assemblyProtocolDescr : null;
        }

        private void MnuAssemblyProtocolDestroyClick(object sender, RoutedEventArgs e)
        {
            //считываем идентификатор выбранного пользователем протокола сборки
            int? selectedAssemblyProtocolID = this.SelectedAssemblyProtocolID();

            if (selectedAssemblyProtocolID == null)
            {
                MessageBox.Show(Properties.Resources.NothingHasBeenSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                if (MessageBox.Show(string.Concat(Properties.Resources.Destroy, " ", Properties.Resources.AssemblyProtocol.ToLowerInvariant(), " № ", this.SelectedAssemblyProtocolDescr().ToString(), "?"), Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    int assemblyProtocolID = (int)selectedAssemblyProtocolID;

                    SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

                    try
                    {
                        DbRoutines.DestroyAssemblyProtocol(assemblyProtocolID);

                        //обновляем отображаемые данные чтобы из отображаемого списка пропал расформированный протокол сборки
                        this.RefreshShowingData();
                    }
                    finally
                    {
                        SCME.Common.Routines.HideProcessWaitVisualizer(this.ProcessWaitVisualizerHWnd);
                    }
                }
            }
        }

        private void MnuReportBuildClick(object sender, RoutedEventArgs e)
        {
            //считываем идентификатор выбранного пользователем протокола сборки
            int? selectedAssemblyProtocolID = this.SelectedAssemblyProtocolID();

            if (selectedAssemblyProtocolID == null)
            {
                MessageBox.Show(Properties.Resources.NothingHasBeenSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

                try
                {
                    int assemblyProtocolID = (int)selectedAssemblyProtocolID;

                    //считываем данные протокола сборки                    
                    DbRoutines.LoadAssemblyProtocol(assemblyProtocolID, out bool? deviceModeView, out string assemblyJob, out bool? export, out int? deviceTypeID, out int? averageCurrent, out string modification, out string constructive, out int? deviceClass, out int? dUdt, out double? trr, out string tq, out double? tgt, out int? qrr, out string climatic, out int? omnity);

                    int iDeviceTypeID = (deviceTypeID == null) ? -1 : (int)deviceTypeID;

                    if (!DbRoutines.DeviceTypeByDeviceTypeID(iDeviceTypeID, out string deviceTypeRU, out string deviceTypeEN))
                    {
                        deviceTypeRU = null;
                        deviceTypeEN = null;
                    }

                    Routines.GroupDescr tqGroup = (tq == null) ? null : Routines.TqGroups[tq];
                    Routines.GroupDescr dUdtGroup = (dUdt == null) ? null : Routines.DUDtGroups[(int)dUdt];
                    Routines.GroupDescr trrGroup = (trr == null) ? null : Routines.TrrGroups[(double)trr];
                    Routines.GroupDescr tgtGroup = (tgt == null) ? null : Routines.TgtOnGroups[(double)tgt];

                    string tqValue;
                    string dUdtValue;
                    string trrValue;
                    string tgtValue;

                    switch (deviceModeView == true)
                    {
                        case true:
                            tqValue = tqGroup?.Num;
                            dUdtValue = dUdtGroup?.Num;
                            trrValue = trrGroup?.Num;
                            tgtValue = tgtGroup?.Num;
                            break;

                        default:
                            tqValue = tqGroup?.Descr;
                            dUdtValue = dUdtGroup?.Descr;
                            trrValue = trrGroup?.Descr;
                            tgtValue = tgtGroup?.Descr;
                            break;
                    }

                    bool sExport = (export == null) ? false : (export == true) ? true : false;
                    string sItav = averageCurrent?.ToString();
                    string sDeviceClass = (deviceClass == null) ? "0" : deviceClass.ToString();
                    string deviceDescr = Routines.CalcDeviceDescr(deviceTypeRU, deviceTypeEN, sExport, constructive, dUdtValue, tqValue, trrValue, tgtValue, modification, climatic, sItav, sDeviceClass);

                    int itav = (averageCurrent == null) ? -1 : (int)averageCurrent;
                    string sOmnity = omnity?.ToString();

                    string sTrr = trr?.ToString();
                    string sQrr = qrr?.ToString();
                    string sdUdt = dUdt?.ToString();
                    string sTgt = tgt?.ToString();

                    double systemScale = Routines.SystemScale(this);
                    AssemblyProtocolReport.Build(assemblyProtocolID, null, systemScale, - 1, assemblyJob, deviceDescr, deviceTypeRU, sOmnity, tqGroup?.TrueValue, sTrr, sQrr, sdUdt, sTgt, itav, iDeviceTypeID, constructive, modification, sDeviceClass);
                }
                finally
                {
                    SCME.Common.Routines.HideProcessWaitVisualizer(this.ProcessWaitVisualizerHWnd);
                }
            }
        }

        private void RefreshData()
        {
            //данное действие возможно будет использовать фильтрацию данных
            SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

            //уничтожаем кеш для его принудительной перестройки на основе последних данных
            DbRoutines.CacheAssemblyProtocolsFree();

            this.RefreshShowingData();
        }
    }
}
