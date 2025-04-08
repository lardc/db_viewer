using AlphaChiTech.Virtualization;
using SCME.CustomControls;
using SCME.dbViewer.ForParameters;
using SCME.dbViewer.Properties;
using SCME.Types;
using SCME.Types.Profiles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //табельный номер, идентификатор аутентифицированного в данном приложении пользователя
        private string FTabNum;
        public string TabNum
        {
            get { return this.FTabNum; }

            set
            {
                if (value != this.FTabNum)
                {
                    this.FTabNum = value;
                    this.NotifyPropertyChanged();

                    //чтобы при изменении пользователя это отразилось в Tittle формы
                    this.TittleValue = "refresh";
                }
            }
        }

        public long FUserID = -1;

        private string FTittleValue = Properties.Resources.MainWindowTitle;
        public string TittleValue
        {
            get { return FTittleValue; }

            set
            {
                if (value != this.FTittleValue)
                {
                    this.FTittleValue = string.Concat(Properties.Resources.MainWindowTitle, string.IsNullOrEmpty(this.TabNum) ? string.Empty : string.Concat(" ", Properties.Resources.User, ": '", this.TabNum, "'"));
                    this.NotifyPropertyChanged();
                }
            }
        }

        //используется для реализации отложенной очереди создания столбцов, отображающих значения condions и parameters - создание этих столбцов прямо при построении данных нельзя из-за потоковых ограничений
        private ConcurrentQueue<Action> FQueueManager = new ConcurrentQueue<Action>();

        /*
        //число использований столбца this.dgDevices.LastHeaderClicked по которому выполнена последняя сортировка данных в this.dgDevices
        public int FLastNumberOfUses = 0;        
            
        //запрос для извлечения количества записей во всей выборке. использовать string.Format, единственный параметр должен содержать строку вида " AND ...."
        private const string FSQLSelectCount = "SELECT COUNT(*) AS ROWSCOUNT" +
                                               " FROM" +
                                               "      (" +
                                               "       SELECT D.PROFILE_ID, ROW_NUMBER() OVER(PARTITION BY D.GROUP_ID, D.CODE, D.MME_CODE, D.PARTPROFILENAME ORDER BY D.DEV_ID DESC) AS RN" +
                                               "       FROM DEVICES D WITH(NOLOCK)" +
                                               "       WHERE (" +
                                               "               NOT(D.GROUP_ID=0) AND" +
                                               "               NOT(D.PARTPROFILENAME IS NULL) {0}" +
                                               "             )" +
                                               "      ) x" +
                                               "  LEFT JOIN PROFILES P WITH(NOLOCK) ON (" +
                                               "                                         (x.PROFILE_ID=P.PROF_GUID) AND" +
                                               "                                         (ISNULL(P.IS_DELETED, 0)=0)" +
                                               "                                       )" +
                                               " WHERE (x.RN=1)";

        //запрос для чтения данных. использовать string.Format, параметры: 0-ой вида " AND ...." будет добавлен к существующему условию WHERE, 1-ый параметр это смещение отностительно начальной записи, 2-ой параметр это количество требуемых записей, 3-ий параметр это выражение ORDER BY ...
        private const string FSQLSelectData = "SELECT z.DEV_ID, RTRIM(G.GROUP_NAME) AS GROUP_NAME, z.GROUP_ID, z.CODE, z.MME_CODE, z.TS, z.USR, z.DEVICETYPE, z.AVERAGECURRENT, z.CONSTRUCTIVE, z.ITEM, z.SITYPE, z.SIOMNITY, z.DEVICECLASS, ISNULL(z.SAPID, 0) AS SAPID, z.STATUS, z.REASON, z.CODEOFNONMATCH, P.PROF_ID, z.PROFILE_ID AS PROF_GUID, P.PROF_NAME," +
                                              "       (" +
                                              "        SELECT T.TEST_TYPE_NAME AS Test, RTRIM(C.COND_NAME) AS Name, RTRIM(CAST(PC.VALUE AS VARCHAR(10))) AS Value" +
                                              "        FROM PROF_COND PC WITH(NOLOCK)" +
                                              "         INNER JOIN PROF_TEST_TYPE PTT WITH(NOLOCK) ON (PC.PROF_TESTTYPE_ID=PTT.PTT_ID)" +
                                              "         INNER JOIN TEST_TYPE T WITH(NOLOCK) ON (PTT.TEST_TYPE_ID=T.TEST_TYPE_ID)" +
                                              "         INNER JOIN CONDITIONS C WITH(NOLOCK) ON (PC.COND_ID=C.COND_ID)" +
                                              "        WHERE (P.PROF_ID=PC.PROF_ID)" +
                                              "        ORDER BY PC.PROF_TESTTYPE_ID, PC.COND_ID" +
                                              "        FOR XML AUTO, ROOT('CONDITIONS')" +
                                              "       ) AS PROFCONDITIONS," +
                                              "       (" +
                                              "        SELECT *" +
                                              "        FROM" +
                                              "             (" +
                                              "              SELECT TT.TEST_TYPE_NAME AS Test, RTRIM(P.PARAM_NAME) AS Name, NULL AS TemperatureCondition, ISNULL(P.PARAMUM, '') AS Um, DP.DEV_PARAM_ID, CAST(DP.VALUE AS VARCHAR(10)) AS Value, CAST(PP.MIN_VAL AS VARCHAR(10)) AS NrmMin, CAST(PP.MAX_VAL AS VARCHAR(10)) AS NrmMax" +
                                              "              FROM DEV_PARAM DP WITH(NOLOCK)" +
                                              "               INNER JOIN PROF_TEST_TYPE PTTD WITH(NOLOCK) ON (DP.TEST_TYPE_ID=PTTD.PTT_ID)" +
                                              "               INNER JOIN TEST_TYPE TT WITH(NOLOCK) ON (PTTD.TEST_TYPE_ID=TT.TEST_TYPE_ID)" +
                                              "               INNER JOIN PARAMS P WITH(NOLOCK) ON (DP.PARAM_ID=P.PARAM_ID)" +
                                              "               LEFT JOIN PROF_PARAM PP WITH(NOLOCK) ON (" +
                                              "                                                        (DP.TEST_TYPE_ID=PP.PROF_TESTTYPE_ID) AND" +
                                              "                                                        (DP.PARAM_ID=PP.PARAM_ID)" +
                                              "                                                       )" +
                                              "              WHERE (z.DEV_ID=DP.DEV_ID)" +
                                              "              UNION" +
                                              "              SELECT 'Manually', MIP.NAME, MIP.TemperatureCondition, MIP.UM, NULL AS Dev_Param_ID, MDP.VALUE, NULL AS NrmMin, NULL AS NrmMax" +
                                              "              FROM MANUALINPUTDEVPARAM MDP WITH(NOLOCK)" +
                                              "               INNER JOIN MANUALINPUTPARAMS MIP WITH(NOLOCK) ON (MDP.MANUALINPUTPARAMID=MIP.MANUALINPUTPARAMID)" +
                                              "              WHERE (z.DEV_ID=MDP.DEV_ID)" +
                                              "             ) AS DEVICEPARAMETERS" +
                                              "        ORDER BY DEV_PARAM_ID" +
                                              "        FOR XML AUTO, ROOT('PARAMETERS')" +
                                              "       ) AS DEVICEPARAMETERS," +
                                              "       (" +
                                              "         SELECT TOP 1 DC.COMMENTS" +
                                              "         FROM DEVICECOMMENTS DC WITH(NOLOCK)" +
                                              "         WHERE (z.DEV_ID=DC.DEV_ID)" +
                                              "         ORDER BY DC.RECORDDATE DESC" +
                                              "       ) AS DEVICECOMMENTS" +
                                              " FROM" +
                                              "      (" +
                                              "       SELECT x.DEV_ID, x.GROUP_ID, x.CODE, x.MME_CODE, x.TS, x.USR, x.DEVICETYPE, x.AVERAGECURRENT, x.CONSTRUCTIVE, x.ITEM, x.SITYPE, x.SIOMNITY, x.DEVICECLASS, x.SAPID, x.STATUS, x.REASON, x.CODEOFNONMATCH, x.PROFILE_ID" +
                                              "       FROM" +
                                              "            (" +
                                              "             SELECT D.DEV_ID, D.GROUP_ID, D.CODE, D.MME_CODE, D.TS, D.USR, D.DEVICETYPE, D.AVERAGECURRENT, D.CONSTRUCTIVE, D.ITEM, D.SITYPE, D.SIOMNITY, D.DEVICECLASS, D.SAPID, D.STATUS, D.REASON, D.CODEOFNONMATCH, D.PROFILE_ID, ROW_NUMBER() OVER(PARTITION BY D.GROUP_ID, D.CODE, D.MME_CODE, D.PARTPROFILENAME ORDER BY D.DEV_ID DESC) AS RN" +
                                              "             FROM DEVICES D WITH(NOLOCK)" +
                                              "             WHERE" +
                                              "                   (" +
                                              "                     NOT(D.GROUP_ID=0) AND" +
                                              "                     NOT(D.PARTPROFILENAME IS NULL) {0}" +
                                              "                   )" +
                                              "            ) x" +
                                              "       WHERE (x.RN=1)" +
                                              "       ORDER BY x.CODE" +
                                              "       OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY" +
                                              "      ) z" +
                                              "  LEFT JOIN GROUPS G WITH(NOLOCK) ON (z.GROUP_ID=G.GROUP_ID)" +
                                              "  LEFT JOIN PROFILES P WITH(NOLOCK) ON (" +
                                              "                                         (z.PROFILE_ID=P.PROF_GUID) AND" +
                                              "                                         (ISNULL(P.IS_DELETED, 0)=0)" +
                                              "                                       )" +
                                              " {3}";
        */

        /*
                private const string FSQLSelectData = "SELECT z.DEV_ID, RTRIM(z.GROUP_NAME) AS GROUP_NAME, z.GROUP_ID, z.CODE, z.MME_CODE, z.TS, z.USR, z.DEVICETYPE, z.AVERAGECURRENT, z.CONSTRUCTIVE, z.ITEM, z.SITYPE, z.SIOMNITY, z.DEVICECLASS, ISNULL(z.SAPID, 0) AS SAPID, z.STATUS, z.REASON, z.CODEOFNONMATCH, z.PROF_ID, z.PROF_GUID, z.PROF_NAME," +
                                                      "       (" +
                                                      "        SELECT T.TEST_TYPE_NAME AS Test, RTRIM(C.COND_NAME) AS Name, RTRIM(CAST(PC.VALUE AS VARCHAR(10))) AS Value" +
                                                      "        FROM PROF_COND PC WITH(NOLOCK)" +
                                                      "         INNER JOIN PROF_TEST_TYPE PTT WITH(NOLOCK) ON (PC.PROF_TESTTYPE_ID=PTT.PTT_ID)" +
                                                      "         INNER JOIN TEST_TYPE T WITH(NOLOCK) ON (PTT.TEST_TYPE_ID=T.TEST_TYPE_ID)" +
                                                      "         INNER JOIN CONDITIONS C WITH(NOLOCK) ON (PC.COND_ID=C.COND_ID)" +
                                                      "        WHERE (z.PROF_ID=PC.PROF_ID)" +
                                                      "        ORDER BY PC.PROF_TESTTYPE_ID, PC.COND_ID" +
                                                      "        FOR XML AUTO, ROOT('CONDITIONS')" +
                                                      "       ) AS PROFCONDITIONS," +
                                                      "       (" +
                                                      "        SELECT *" +
                                                      "        FROM" +
                                                      "             (" +
                                                      "              SELECT TT.TEST_TYPE_NAME AS Test, RTRIM(P.PARAM_NAME) AS Name, NULL AS TemperatureCondition, ISNULL(P.PARAMUM, '') AS Um, DP.DEV_PARAM_ID, CAST(DP.VALUE AS VARCHAR(10)) AS Value, CAST(PP.MIN_VAL AS VARCHAR(10)) AS NrmMin, CAST(PP.MAX_VAL AS VARCHAR(10)) AS NrmMax" +
                                                      "              FROM DEV_PARAM DP WITH(NOLOCK)" +
                                                      "               INNER JOIN PROF_TEST_TYPE PTTD WITH(NOLOCK) ON (DP.TEST_TYPE_ID=PTTD.PTT_ID)" +
                                                      "               INNER JOIN TEST_TYPE TT WITH(NOLOCK) ON (PTTD.TEST_TYPE_ID=TT.TEST_TYPE_ID)" +
                                                      "               INNER JOIN PARAMS P WITH(NOLOCK) ON (DP.PARAM_ID=P.PARAM_ID)" +
                                                      "               LEFT JOIN PROF_PARAM PP WITH(NOLOCK) ON (" +
                                                      "                                                        (DP.TEST_TYPE_ID=PP.PROF_TESTTYPE_ID) AND" +
                                                      "                                                        (DP.PARAM_ID=PP.PARAM_ID)" +
                                                      "                                                       )" +
                                                      "              WHERE(z.DEV_ID=DP.DEV_ID)" +
                                                      "              UNION" +
                                                      "              SELECT 'Manually', MIP.NAME, MIP.TemperatureCondition, MIP.UM, NULL AS Dev_Param_ID, MDP.VALUE, NULL AS NrmMin, NULL AS NrmMax" +
                                                      "              FROM MANUALINPUTDEVPARAM MDP WITH(NOLOCK)" +
                                                      "               INNER JOIN MANUALINPUTPARAMS MIP WITH(NOLOCK) ON(MDP.MANUALINPUTPARAMID=MIP.MANUALINPUTPARAMID)" +
                                                      "              WHERE (z.DEV_ID=MDP.DEV_ID)" +
                                                      "             ) AS DEVICEPARAMETERS" +
                                                      "        ORDER BY DEV_PARAM_ID" +
                                                      "        FOR XML AUTO, ROOT('PARAMETERS')" +
                                                      "       ) AS DEVICEPARAMETERS," +
                                                      "       (" +
                                                      "         SELECT TOP 1 DC.COMMENTS" +
                                                      "         FROM DEVICECOMMENTS DC WITH(NOLOCK)" +
                                                      "         WHERE (z.DEV_ID=DC.DEV_ID)" +
                                                      "         ORDER BY DC.RECORDDATE DESC" +
                                                      "       ) AS DEVICECOMMENTS" +
                                                      " FROM" +
                                                      "      (" +
                                                      "       SELECT x.DEV_ID, x.GROUP_NAME, x.GROUP_ID, x.CODE, x.MME_CODE, x.TS, x.USR, x.DEVICETYPE, x.AVERAGECURRENT, x.CONSTRUCTIVE, x.ITEM, x.SITYPE, x.SIOMNITY, x.DEVICECLASS, x.SAPID, x.STATUS, x.REASON, x.CODEOFNONMATCH, x.PROF_ID, x.PROF_GUID, x.PROF_NAME" +
                                                      "       FROM" +
                                                      "            (" +
                                                      "             SELECT G.GROUP_NAME, D.DEV_ID, D.GROUP_ID, D.CODE, D.MME_CODE, D.TS, D.USR, D.DEVICETYPE, D.AVERAGECURRENT, D.CONSTRUCTIVE, D.ITEM, D.SITYPE, D.SIOMNITY, D.DEVICECLASS, D.SAPID, D.STATUS, D.REASON, D.CODEOFNONMATCH, P.PROF_ID, P.PROF_GUID, P.PROF_NAME, ROW_NUMBER() OVER(PARTITION BY D.GROUP_ID, D.CODE, D.MME_CODE, D.PARTPROFILENAME ORDER BY D.DEV_ID DESC) AS RN" +
                                                      "             FROM DEVICES D WITH(NOLOCK)" +
                                                      "              INNER JOIN PROFILES P WITH(NOLOCK) ON (" +
                                                      "                                                     NOT(D.PARTPROFILENAME IS NULL) AND" +
                                                      "                                                     (D.PROFILE_ID=P.PROF_GUID) AND" +
                                                      "                                                     (ISNULL(P.IS_DELETED, 0)=0)" +
                                                      "                                                    )" +
                                                      "              INNER JOIN GROUPS G WITH(NOLOCK) ON(D.GROUP_ID=G.GROUP_ID)" +
                                                      "             WHERE (D.DEV_ID>1) {0}" +
                                                      "            ) x" +
                                                      "       WHERE (x.RN=1)" +
                                                      "       ORDER BY x.CODE" +
                                                      "       OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY" +
                                                      "      ) z" +
                                                      " {3}";
        */

        //столбец из this.dgDevices по которому пользователь хочет выполнить либо сортировку данных
        private string FSortSourceFieldName = null;

        private CustomControls.ActiveFilters FActiveFilters = null;

        //флаг 'Включен режим просмотра протокола сборки'
        private bool FAssemblyProtocolMode = false;
        public bool AssemblyProtocolMode
        {
            get { return this.FAssemblyProtocolMode; }

            set
            {
                if (value != this.FAssemblyProtocolMode)
                {
                    //при выходе из режима протокола сборки очистим все ComboBox, которые дают возможность фильтрации данных т.к. после просмотра протокола сборки в них останутся данные из шапки протокола сборки)
                    if (value == false)
                        this.ClearSelectedItemInComboBoxFilters();

                    this.FAssemblyProtocolMode = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        //здесь храним список столбцов, которые были динамически построены (conditions/parameters) в dgDevices в результате загрузки данных (страницы данных из базы данных)
        //информация здесь всегда поддерживается в актуальном состоянии
        //используется для упорядочивания динамически созданных столбцов в режиме протокола сборки в соответствии с типом изделия
        private List<DataGridColumnSourceData> FDataGridColumnSourceData = new List<DataGridColumnSourceData>();

        //битовая маска разрешений аутентифицированного в данном приложении пользователя
        private ulong FPermissionsLo = 0;
        public ulong PermissionsLo
        {
            get { return this.FPermissionsLo; }

            set
            {
                this.FPermissionsLo = value;
                this.NotifyPropertyChanged();
            }
        }

        //дескриптор окна визуализации ожидания
        private IntPtr FProcessWaitVisualizerHWnd = IntPtr.Zero;
        private IntPtr ProcessWaitVisualizerHWnd
        {
            get { return this.FProcessWaitVisualizerHWnd; }
        }

        private bool FTextBoxDeviceCommentsTextChanged = false;

        private object FColumnsLocker = new object();


        /*
        //часть JOIN SQL запроса, которая получена для цели сортировки данных
        private string JoinSortSection { get; set; } = null;

        //часть JOIN SQL запроса, которая получена для цели фильтрации данных 
        private string JoinFiltersSection { get; set; } = null;

        private string WhereSection { get; set; } = null;
        private string HavingSection { get; set; } = null;
        private string SortingExpression { get; set; } = null;
        */

        /*
        private const int cDevID = 0;
        private const int cGroupName = 1;
        private const int cCode = 2;
        private const int cTsZeroTime = 4;
        private const int cUser = 6;
        private const int cDeviceType = 7;
        private const int cAverageCurrent = 8;
        private const int cConstructive = 9;
        private const int cItem = 10;
        private const int cProfileID = 13;
        private const int cProfileName = 14;

        //временно отсутствуют
        private const int cDeviceClass = -1;
        private const int cEquipment = -1;
        private const int cStatus = -1;
        private const int cReason = -1;
        private const int cCodeOfNonMatch = -1;

        private List<DataTableParameters> listOfDeviceParameters = new List<DataTableParameters>();
        */

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Localization);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Localization error");
            }

            InitializeComponent();

            //создаём таймер, который будет анализировать очередь this.FQueueManager для возможности построения столбцов отображения значений conditions и parameters
            this.CreateQueueWorker();

            //загружаем список статусов изделий по протоколу сборки
            //this.PrepareStatusesByAssemblyProtocol();

            KeyPreview();

            /*
            dgDevices.UnFrozeMainFormHandler = this.UnFrozeMainForm;
            
            //dgDevices.CreateCalculatedFieldsHandler = CreateCalculatedFields;
            dgDevices.GetDeviceTypeHandler = this.DeviceType;
            dgDevices.GetCodeHandler = this.Code;
            dgDevices.GetGroupNameHandler = this.GroupName;
            dgDevices.GetProfileNameHandler = this.ProfileName;
            dgDevices.GetDeviceClassHandler = this.DeviceClass;
            dgDevices.GetStatusHandler = this.Status;

            dgDevices.RefreshBottomRecordCountHandler = this.RefreshBottomRecordCount;
            */

            //this.SetVisibleFiltersOfSourceData();

            //создаём место хранения списка фильтров в котором пользователь будет создавать, изменять и удалять свои фильтры
            //передаём в this.FActiveFilters ссылку на реализацию, которая будет вызываться всякий раз когда выполняется удаление фильтра/фильтров
            this.FActiveFilters = new CustomControls.ActiveFilters() { OnChangedListOfFiltersHandler = this.OnChangedListOfFiltersHandler };

            //передаём в this.dgDevices ссылку на реализацию вызова редактора фильтров
            this.dgDevices.SetFilterHandler = this.SetFilter;

            //создаём процесс визуализации ожидания (чтобы пользователь понимал, что система занята выполнением его запроса, а не зависла)
            //данный процесс создаём на всё время выполнения данного приложения, управляем видимостью его главной формы и уничтожаем этот процесс при завершении работы приложения в реализации this.MainForm_Closing
            this.FProcessWaitVisualizerHWnd = Common.Routines.StartProcessWaitVisualizer();
        }

        private static void DispatcherUnhandledException(object Sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs E)
        {
            MessageBox.Show(E.Exception.ToString(), "Unhandled exception");
            Application.Current.Shutdown();
        }

        private static void CurrentDomainOnUnhandledException(object Sender, UnhandledExceptionEventArgs Args)
        {
            MessageBox.Show(Args.ExceptionObject.ToString(), "Unhandled exception");
            Application.Current.Shutdown();
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
                //при выполнении процедуры сортировки выполняется this.dgDevices.IsEnabled = false
                //делаем пользователю доступным dgDevices т.к. операция сортировки уже окончена
                //запрет на работу пользователя с this.dgDevices нужен для исключения взаимной блокировки транзакций, которая может возникуть если пользователь не дождавшись результата уже выполняющейся сортировки попытается выполнить новую сортировку
                this.dgDevices.IsEnabled = true;

                //больше не надо демонстрировать пользователю необходимость ожидания
                SCME.Common.Routines.HideProcessWaitVisualizerSortingFiltering(this.ProcessWaitVisualizerHWnd);
            }
        }

        private DataProvider FDataProvider;
        private DataSourceProxy FDataSourceProxy;

        private Collection<DynamicObj> FDataSource = null;
        public Collection<DynamicObj> DataSource
        {
            get
            {
                if (this.FDataSource == null)
                {
                    this.FDataProvider = new DataProvider(this.CacheEdit, this.AfterBuildingDataInCacheRoutines, this.AfterPortionDataLoadedRoutines);

                    //передаём необходимое для добывания данных из базы данных
                    this.FDataProvider.Init(this.BuildColumnInDataGrid, this.RemoveXMLDataGridColumns);

                    this.FDataSourceProxy = new DataSourceProxy(this.FDataProvider);
                    PaginationManager<DynamicObj> paginationManager = new PaginationManager<DynamicObj>(this.FDataSourceProxy, pageSize: 80, maxPages: 2);
                    this.FDataSource = new Collection<DynamicObj>(paginationManager);

                    this.FDataProvider.FCollection = this.FDataSource;
                }

                return this.FDataSource;
            }
        }

        /*
        private void UnFrozeMainForm()
        {
            this.IsEnabled = true;
        }
      
        private void SetVisibleFiltersOfSourceData()
        {
            //установка видимости фильтров исходных данных в зависимости от наличия прав на их использование
            //все фильтры, кроме указанных ниже доступны любому пользователю по уиолчанию
            if (Routines.IsUserCanUseAllFiltersOfSourceData(this.PermissionsLo))
            {
                dpBegin.Visibility = Visibility.Visible;
                dpEnd.Visibility = Visibility.Visible;
                cmb_StatusByAssemblyProtocol.Visibility = Visibility.Visible;
                tb_ProfName.Visibility = Visibility.Visible;
                tb_MmeCode.Visibility = Visibility.Visible;
                tb_Usr.Visibility = Visibility.Visible;
            }
            else
            {
                dpBegin.Visibility = Visibility.Hidden;
                dpEnd.Visibility = Visibility.Hidden;
                cmb_StatusByAssemblyProtocol.Visibility = Visibility.Hidden;
                tb_ProfName.Visibility = Visibility.Hidden;
                tb_MmeCode.Visibility = Visibility.Hidden;
                tb_Usr.Visibility = Visibility.Hidden;
            }
        }
        */

        /*
        private int DevID(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.DevID);
            return int.Parse(itemArray?[index].ToString());
        }

        private string ProfileID(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.ProfileID);
            return itemArray?[index].ToString();
        }

        private DateTime Ts(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.Ts);
            return DateTime.Parse(itemArray?[index].ToString());
        }

        private string GroupName(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.GroupName);
            return itemArray?[index].ToString();
        }

        private string Item(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.Item);
            return itemArray?[index].ToString();
        }

        private string Code(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.Code);
            return itemArray?[index].ToString();
        }

        private string ProfileName(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.ProfileName);
            return itemArray?[index].ToString();
        }

        private string ProfileBody(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.ProfileBody);
            return itemArray?[index].ToString();
        }

        private string DeviceType(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.DeviceType);
            return itemArray?[index].ToString();
        }

        private string Constructive(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.Constructive);
            return itemArray?[index].ToString();
        }

        private int? AverageCurrent(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.AverageCurrent);
            int? averageCurrent = itemArray?[index] as int?;

            return (averageCurrent == null) ? null : int.Parse(itemArray?[index].ToString()) as int?;
        }

        private int? DeviceClass(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.DeviceClass);
            int? deviceClass = itemArray?[index] as int?;

            return (deviceClass == null) ? null : int.Parse(itemArray?[index].ToString()) as int?;
        }

        private string Equipment(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.MmeCode);
            return itemArray?[index].ToString();
        }

        private string User(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.Usr);
            return itemArray?[index].ToString();
        }

        private string Status(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.Status);
            return itemArray?[index].ToString();
        }

        private string CodeOfNonMatch(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.CodeOfNonMatch);
            return itemArray?[index].ToString();
        }

        private string Reason(object[] itemArray)
        {
            int index = this.dgDevices.dtData.Columns.IndexOf(Constants.Reason);
            return itemArray?[index].ToString();
        }
        */

        private void RemoveXMLDataGridColumns()
        {
            //удаление всех столбцов this.dgDevices, которые отображают значения conditions/parameters
            Routines.DeleteAllColumnsFromXML(this.dgDevices, this.FQueueManager, this.FColumnsLocker);

            //удаляем все исходные данные для построения списка столбцов в dgDevices
            //полученные от обработки последней загруженной порции данных (страницы данных)
            this.FDataGridColumnSourceData.Clear();
        }

        private void BuildColumnInDataGrid(string header, string bindPath)
        {
            Routines.SetToQueueCreateColumnInDataGrid(this.dgDevices, this.cmbDeviceType, this.FQueueManager, this.FColumnsLocker, this.AssemblyProtocolMode, header, bindPath);

            //запоминаем исходные данные для построения столбца в dgDevices
            DataGridColumnSourceData dataGridColumnSourceData = new DataGridColumnSourceData(header, bindPath);
            this.FDataGridColumnSourceData.Add(dataGridColumnSourceData);
        }

        public void KeyEventHandler(object sender, KeyEventArgs e)
        {
            if (sender is DatePicker)
            {
                switch (e.Key)
                {
                    case Key.Delete:
                    case Key.Back:
                    case Key.Escape:
                        DatePicker dt = (DatePicker)sender;
                        dt.SelectedDate = null;
                        break;

                    case Key.Enter:
                        //LoadDevices();
                        break;
                }
            }
        }

        private void KeyPreview()
        {
            foreach (Control fe in FindVisualChildren<Control>(grdParent))
            {
                fe.KeyDown += new KeyEventHandler(KeyEventHandler);
            }
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private TemperatureCondition TemperatureConditionByProfileName(string profileName)
        {
            TemperatureCondition result = TemperatureCondition.None;

            if (profileName.Trim() != string.Empty)
            {
                string profileNameUpper = profileName.ToUpper();

                result = profileNameUpper.Contains("RT") ? TemperatureCondition.RT : profileNameUpper.Contains("TM") ? TemperatureCondition.TM : TemperatureCondition.None;
            }

            return result;
        }

        /*
        private int IndexOfDevID(List<DataTableParameters> listOfDeviceParameters, int DevID)
        {
            //вычисляет индекс записи в списке listOfDeviceParameters, с идентификатором DevID. в списке listOfDeviceParameters может быть только одна такая запись
            var results = from DataTableParameters dtp in listOfDeviceParameters
                          where (
                                 (dtp.DevID == DevID)
                                )
                          select dtp;

            if (results.Count() != 1)
                throw new Exception(string.Format("MainWindow.IndexOfDevID. Для DevID={0} найдено записей: {1}. Ожидалась одна запись.", DevID, results.Count()));

            DataTableParameters result = results.FirstOrDefault();

            return listOfDeviceParameters.IndexOf(result);
        }
        */


        public delegate void delegateRefreshBottomRecordCount();
        private void RefreshBottomRecordCount()
        {
            //вычисляем сколько записей стоит ниже текущей выбранной
            if (dgDevices.CurrentCell.IsValid)
            {
                int selectedRowNum = dgDevices.Items.IndexOf(dgDevices.CurrentCell.Item) + 1;
                int bottomRecords = dgDevices.Items.Count - selectedRowNum;

                lbBottomRecordCount.Content = string.Format("({0})", bottomRecords.ToString());
            }
            else
            {
                lbBottomRecordCount.Content = string.Empty;
            }
        }

        private void dgDevices_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //смена выбранной ячейки
            //вычисляем сколько записей стоит ниже текущей выбранной
            this.RefreshBottomRecordCount();
        }

        private void dgDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //смена выбранной строки
            //вычисляем сколько записей стоит ниже текущей выбранной
            this.RefreshBottomRecordCount();
        }

        /*
        private DataTableParameters FindDataTableParametersByProfileID(List<DataTableParameters> listOfDeviceParameters, string profileID)
        {
            //ищет в принятом listOfDeviceParameters первую попавшуюся запись, имеющую профиль profileID
            var linqResults = listOfDeviceParameters.Where(fn => fn.ProfileID == profileID);

            return linqResults.FirstOrDefault();
        }
        */

        private int? CalcClass(int? value1, int? value2)
        {
            if ((value1 == null) || (value2 == null))
            {
                //если хотя-бы одно из принятых значенией равно null, то и результат null 
                return null;
            }
            else
            {
                //оба принятых значения не null
                return Math.Min((int)value1, (int)value2);
            }
        }

        private void BuildReportInExcel(ReportData reportData, double systemScale)
        {
            //построение отчёта в Excel
            //получаем содержимое кеша
            if (reportData != null)
            {
                //для того, чтобы в отчёте была обеспечена уникальность шапки (любая шапка в формируемом отчёте должна быть уникальной) выполняем сортировку исходных данных для построения отчёта по ColumnsSignature, а внутри каждого уникального набора по коду ГП
                SCME.CustomControls.CustomComparer<object> customComparer = new SCME.CustomControls.CustomComparer<object>(ListSortDirection.Ascending);
                List<ReportRecord> sortedReportByDevices = reportData.OrderBy(x => x.ColumnsSignature).ThenBy(x => x.Code, customComparer).ToList<ReportRecord>();

                ReportData rep = new ReportData(sortedReportByDevices);

                //формируем отчёт            
                rep.ReportToExcel(systemScale);
            }
        }

        private string SelectedValueFromComboBox(ComboBox cmb)
        {
            //возвращает значение выбранное в cmb, соответствующее принятому cmb (cmbDUDt, cmbTrr, cmbTq, cmbTgt, cmbQrr)
            string result;

            if (cmb != null)
            {
                switch (cmb.Name)
                {
                    case "cmbDUDt":
                        result = (cmbDUDt.SelectedItem is GroupOfValues groupOfValuesdUdt) ? string.Concat("V", groupOfValuesdUdt.TrueValue) : null;

                        return result;

                    case "cmbTrr":
                        result = (cmbTrr.SelectedItem is GroupOfValues groupOfValuesTrr) ? groupOfValuesTrr.TrueValue : null;

                        return result;

                    case "cmbTq":
                        result = (cmbTq.SelectedItem is GroupOfValues groupOfValuesTq) ? groupOfValuesTq.TrueValue : null;

                        return result;

                    case "cmbTgt":
                        result = (cmbTgt.SelectedItem is GroupOfValues groupOfValuesTgt) ? groupOfValuesTgt.TrueValue : null;

                        return result;

                    case "cmbQrr":
                        result = (cmbQrr.SelectedItem is QrrGroupOfValues groupOfValuesQrr) ? groupOfValuesQrr.Value : null;

                        return result;

                    default:
                        return null;
                }
            }

            return null;
        }

        private DynamicObj UserPropsOfAssemblyProtocol(int assemblyReportRecordCount)
        {
            //assemblyReportRecordCount - сколько записей содержит данный отчёт
            //запоминаем в возвращаемом результате значения реквизитов, которые пользователь установил для протокола сборки (по которому формируется отчёт)
            //возвращает:
            // DynamicObj - данная реализация успешно запомнила реквизиты протокола сборки в возращаемом DynamicObj;
            // Null - данная реализация не смогла запомнить реквизиты протокола сборки - обнаружила ошибки в сохраняемых данных

            DynamicObj result = new DynamicObj();

            string assemblyJob = null;
            string packageType = null;

            if (!string.IsNullOrEmpty(this.tbAssemblyJob.Text))
            {
                assemblyJob = this.tbAssemblyJob.Text.Trim();

                if (!string.IsNullOrEmpty(assemblyJob))
                {
                    //проверяем соответствие маске ввода
                    if (!SCME.Common.Routines.CheckAssemblyJobByMask(assemblyJob, out string mask))
                    {
                        MessageBox.Show(string.Format(Properties.Resources.WrongValueByMask, Properties.Resources.AssemblyJob, mask), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }

                    //вычисляем и запоминаем тип корпуса
                    string item = DbRoutines.ItemByAssemblyJob(assemblyJob);

                    if (!string.IsNullOrEmpty(item))
                        packageType = SCME.Common.Routines.PackageTypeByItem(item);

                    //запоминаем количество запущенных изделий по ПЗ
                    result.SetMember(Constants.QtyReleasedByGroupName, DbRoutines.QtyReleasedByGroupName(assemblyJob));
                }
            }

            //запоминаем сборочное ПЗ
            result.SetMember(Constants.AssemblyJob, assemblyJob);

            //запоминаем тип корпуса
            result.SetMember(Constants.PackageType, packageType);

            //запоминаем обозначение изделия
            result.SetMember(Constants.Device, lbDevice.Content);

            //запоминаем тип изделия
            //считываем RU обозначение типа изделия
            string deviceTypeRu = (cmbDeviceType.SelectedItem == null) ? null : ((string[])cmbDeviceType.SelectedItem)[1];
            result.SetMember(Constants.DeviceTypeRu, deviceTypeRu);

            //запоминаем значение КОФ
            result.SetMember(Constants.Omnity, tbOmnity.Text);

            //запоминаем значение Tq
            string tq = this.SelectedValueFromComboBox(this.cmbTq);
            result.SetMember(Constants.Tq, tq);

            //запоминаем значение Trr
            string trr = this.SelectedValueFromComboBox(this.cmbTrr);
            result.SetMember(Constants.Trr, trr);

            //запоминаем значение Qrr
            string qrr = this.SelectedValueFromComboBox(this.cmbQrr);
            result.SetMember(Constants.Qrr, qrr);

            //запоминаем значение dUdt
            string dUdt = this.SelectedValueFromComboBox(this.cmbDUDt);
            result.SetMember(Constants.dUdt, dUdt);

            //запоминаем значение Tgt
            string tgt = this.SelectedValueFromComboBox(this.cmbTgt);
            result.SetMember(Constants.Tgt, tgt);

            //запоминаем количество записей в отчёте
            result.SetMember(Constants.AssemblyReportRecordCount, assemblyReportRecordCount);

            return result;
        }

        /*
        private void FillDeviceReferences()
        {
            //перекачка данных таблицы КАТАЛОГ в DEVICEREFERENCES
            SqlConnection accessDBConnection = new SqlConnection("server=192.168.0.134, 1433; uid=sa; pwd=Hpl1520; database=mplace; MultipleActiveResultSets=True");

            accessDBConnection.Open();

            string sql = @"SELECT СреднийТок, Номенклатура, Конструктив, Idrm,
                                  Utm, Qrr, Igt, Ugt, Tjmax, Prsm, Корпус, Поправка
                           FROM Каталог
                           WHERE not(Код=540) AND
                                 not(Код=553) AND
                                 not(Код=531) AND
                                 not(Код=446) AND
                                 not(Код=590) AND
                                 not(Код=665)";

            SqlCommand command = new SqlCommand(sql, accessDBConnection);
            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(values);

                    int index = reader.GetOrdinal("СреднийТок");

                    int itav;                    
                    if (int.TryParse(values[index].ToString(), out int vItav))
                    {
                        itav = vItav;
                    }
                    else
                        itav = -1;

                    index = reader.GetOrdinal("Номенклатура");
                    string deviceType = (values[index] == DBNull.Value) ? null : values[index].ToString();

                    index = reader.GetOrdinal("Конструктив");
                    string constructive = (values[index] == DBNull.Value) ? null : values[index].ToString();

                    index = reader.GetOrdinal("Idrm");
                    int? idrm;
                    if (int.TryParse(values[index].ToString(), out int vIdrm))
                    {
                        idrm = vIdrm;
                    }
                    else
                        idrm = null;

                    index = reader.GetOrdinal("Utm");
                    decimal? utm;
                    if (decimal.TryParse(values[index].ToString(), out decimal vUtm))
                    {
                        utm = vUtm;
                    }
                    else
                        utm = null;

                    index = reader.GetOrdinal("Qrr");
                    int? qrr;
                    if (int.TryParse(values[index].ToString(), out int vQrr))
                    {
                        qrr = vQrr;
                    }
                    else
                        qrr = null;

                    index = reader.GetOrdinal("Igt");
                    int? igt;
                    if (int.TryParse(values[index].ToString(), out int vIgt))
                    {
                        igt = vIgt;
                    }
                    else
                        igt = null;

                    index = reader.GetOrdinal("Ugt");
                    decimal? ugt;
                    if (decimal.TryParse(values[index].ToString(), out decimal vUgt))
                    {
                        ugt = vUgt;
                    }
                    else
                        ugt = null;

                    index = reader.GetOrdinal("Tjmax");
                    int? tjMax;
                    if (int.TryParse(values[index].ToString(), out int vTjMax))
                    {
                        tjMax = vTjMax;
                    }
                    else
                        tjMax = null;

                    index = reader.GetOrdinal("Prsm");
                    int? prsm;
                    if (int.TryParse(values[index].ToString(), out int vPrsm))
                    {
                        prsm = vPrsm;
                    }
                    else
                        prsm = null;

                    index = reader.GetOrdinal("Корпус");
                    string caseType = (values[index] == DBNull.Value) ? null : values[index].ToString();

                    index = reader.GetOrdinal("Поправка");
                    decimal? correction;
                    if (decimal.TryParse(values[index].ToString(), out decimal vCorrection))
                    {
                        correction = vCorrection;
                    }
                    else
                        correction = null;

                    DbRoutines.InsertToDeviceReferences(itav, deviceType, constructive, idrm, utm, qrr, igt, ugt, tjMax, prsm, caseType, correction);
                }
            }

            finally
            {
                reader.Close();
            }
        }
        */

        private ReportData GetReportData(int cacheSize)
        {
            List<DynamicObj> cacheData = new List<DynamicObj>();
            Routines.GetCacheData(cacheData, cacheSize);

            ReportData reportData = new ReportData(cacheData);

            return reportData;
        }

        private void btReportPrint_Click(object sender, RoutedEventArgs e)
        {
            //dgDevices.SelectedItem = sItem;
            //dgDevices.UpdateLayout();
            //dgDevices.ScrollIntoView(dgDevices.SelectedItem);



            /*
            //string s = string.Format("{0:X2}{1:X2}{2:X2}", 150, 174, 226);
            //string s = ProfileRoutines.ProfileBodyByProfileName("PSERT MT 260 44 A2");

            //формируем отчёт в Excel и не показывая его пользователю, а сразу отправляем на печать           
            ReportData rep = BuildReportInExcel(false);
            rep?.Print();
            */
        }

        private void dgDevices_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void CmbPreviewKeyUp(object sender, KeyEventArgs e)
        {
            bool PreviewKeyUp(ComboBox cmb, Key key)
            {
                bool result = false;

                if (cmb != null)
                {
                    switch (key)
                    {
                        case Key.Delete:
                        case Key.Back:
                        case Key.Escape:
                            cmb.SelectedItem = null;
                            result = true;
                            break;

                        default:
                            result = false;
                            break;
                    }
                }

                return result;
            }

            if (PreviewKeyUp(sender as ComboBox, e.Key))
                e.Handled = true;
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                this.RefreshData();
                e.Handled = true;
            }
        }

        private void RefreshMenu()
        {
            if (Common.Routines.IsUserAdmin(this.PermissionsLo))
            {
                mnuManagePermissionsOfUser.Visibility = Visibility.Visible;
                mnuManageBySelfPermissions.Visibility = Visibility.Visible;
                mnuDeletePermissionsOfUser.Visibility = Visibility.Visible;
            }
            else
            {
                mnuManagePermissionsOfUser.Visibility = Visibility.Collapsed;
                mnuManageBySelfPermissions.Visibility = Visibility.Collapsed;
                mnuDeletePermissionsOfUser.Visibility = Visibility.Collapsed;
            }

            //если в меню работы с пользовательскими параметрами права позволяют работать с ними - показываем меню верхнего уровня, иначе прячем
            //создание измерений отключил 07.02.2024 чтобы пользователи работали только через форму ManualInputDevices, т.к. отключенное средсво всегда создаёт измерения с кодом MME_CODE='Manually' что плохо (они не имеют вычисленного класса и поиск по таким ручным параметрам не работает)
            //mnuParameters.Visibility = this.IsMnuParametersVisible() ? Visibility.Visible : Visibility.Collapsed; 
            mnuDevices.Visibility = this.IsMnuDevicesVisible() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MnuBeginSessionClick(object sender, RoutedEventArgs e)
        {
            //начать сеанс работы
            AuthenticationWindow auth = new AuthenticationWindow();

            auth.ShowModal(out string tabNum, out this.FUserID, out ulong permissionsLo);
            this.TabNum = tabNum;
            this.PermissionsLo = permissionsLo;

            this.RefreshMenu();
            this.AssemblyProtocolMode = false;
            //this.SetVisibleFiltersOfSourceData();
        }

        private void MnuCloseSessionClick(object sender, RoutedEventArgs e)
        {
            //завершить сеанс работы - забываем ранее авторизованного пользователя и его права
            this.TabNum = null;
            this.FUserID = -1;
            this.PermissionsLo = 0;

            this.RefreshMenu();
            this.AssemblyProtocolMode = false;

            //перестраиваем столбцы которые отображают conditions/parameters в dgDevices
            this.ReBuildCPColumnsForAssemblyProtocolMode();
        }

        private bool IsMnuParametersVisible()
        {
            return Common.Routines.IsUserCanCreateValueOfManuallyEnteredParameter(this.PermissionsLo) || Common.Routines.IsUserCanEditValueOfManuallyEnteredParameter(this.PermissionsLo) || Common.Routines.IsUserCanDeleteValueOfManuallyEnteredParameter(this.PermissionsLo);
        }

        private bool IsMnuDevicesVisible()
        {
            return Common.Routines.IsUserCanCreateValueOfManuallyEnteredParameter(this.PermissionsLo);
        }

        private void mnuParameters_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            mnuCreateValueOfManuallyEnteredParameter.Visibility = Common.Routines.IsUserCanCreateValueOfManuallyEnteredParameter(this.PermissionsLo) ? Visibility.Visible : Visibility.Collapsed;
            mnuEditValueOfManuallyEnteredParameter.Visibility = Common.Routines.IsUserCanEditValueOfManuallyEnteredParameter(this.PermissionsLo) ? Visibility.Visible : Visibility.Collapsed;
            mnuDeleteValueOfManuallyEnteredParameter.Visibility = Common.Routines.IsUserCanDeleteValueOfManuallyEnteredParameter(this.PermissionsLo) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MnuDevices_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            mnuCreateDevices.Visibility = Common.Routines.IsUserCanCreateDevices(this.PermissionsLo) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MnuManagePermissionsOfUserClick(object sender, RoutedEventArgs e)
        {
            //выбор пользователя из списка пользователей DC
            if (Common.Routines.IsUserAdmin(this.PermissionsLo))
            {
                DCUsersList dcUsersList = new DCUsersList();

                if (dcUsersList.ShowModal(out string managedTabNum, out long managedUserID, out ulong managedPermissionsLo) ?? false)
                {
                    BitCalculator bitCalc = new BitCalculator(managedUserID, managedPermissionsLo, string.Format("{0} '{1}'", Properties.Resources.SetUserPermissions, managedTabNum));

                    if (bitCalc.ShowModal(out managedPermissionsLo) ?? false)
                    {
                        this.RefreshMenu();
                    }
                }
            }
        }

        private void MnuManageBySelfPermissionsClick(object sender, RoutedEventArgs e)
        {
            //управление соими правами доступно только пользователю, который является администратором этой системы
            if (Common.Routines.IsUserAdmin(this.PermissionsLo))
            {
                //имеем случай, когда пользователь, являющийся администратором управляет своей собственной битовой маской разрешений
                BitCalculator bitCalc = new BitCalculator(this.FUserID, this.PermissionsLo, string.Format("{0} '{1}'", Properties.Resources.SetUserPermissions, this.TabNum));

                bool needRefreshMenu = (bitCalc.ShowModal(out ulong permissionsLo) ?? false);
                this.PermissionsLo = permissionsLo;

                if (needRefreshMenu)
                {
                    this.RefreshMenu();
                }
            }
        }

        private void MnuDeletePermissionsOfUserClick(object sender, RoutedEventArgs e)
        {
            //удаление записи о пользователе из таблицы USERS (данной системы) доступно только пользователю, который является администратором этой системы
            if (Common.Routines.IsUserAdmin(this.PermissionsLo))
            {
                DCUsersList dcUsersList = new DCUsersList();

                if (dcUsersList.ShowModal(out string managedTabNum, out long managedUserID, out ulong managedPermissionsLo) ?? false)
                {
                    MessageBoxResult needDelete = MessageBox.Show(string.Format("{0}: '{1}'. {2}?", Properties.Resources.UserSelected, managedTabNum, Properties.Resources.DeletePermissionsOfUser), Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    switch (needDelete)
                    {
                        case MessageBoxResult.Yes:
                            DbRoutines.DeleteFromUsers(managedUserID);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void LoadProfileParameters(List<string> profileParameters)
        {
            //загрузка списка параметров профиля
            if (int.TryParse(this.dgDevices.ValueFromSelectedRow("PROF_ID").ToString(), out int profileID))
            {
                string profileGUID = Types.DbRoutines.ProfileGUIDByProfileID(profileID);

                if (profileGUID != null)
                    Types.DbRoutines.LoadProfileParameters(profileGUID, profileParameters);
            }
        }

        private void mnuCreateValueOfManuallyEnteredParameterClick(object sender, RoutedEventArgs e)
        {
            //создание параметра пользователя
            if (Common.Routines.IsUserCanCreateValueOfManuallyEnteredParameter(this.PermissionsLo))
            {
                if (dgDevices.CurrentItem is DynamicObj currentItem)
                {
                    object objTDevID = this.dgDevices.ValueFromSelectedRow(Common.Constants.TDevID);

                    if (objTDevID != null)
                    {
                        IEnumerable<string> tDevIDList = objTDevID.ToString().Split(new string[] { Common.Constants.cString_AggDelimeter.ToString() }, StringSplitOptions.None);

                        //строим список температурных режимов при которых проводились измерения objTDevID
                        List<TemperatureCondition> listTemperatureCondition = new List<TemperatureCondition>();

                        foreach (string tDevID in tDevIDList)
                        {
                            //первые два символа в tDevID это температурный режим, далее идентификатор изделия
                            string temperatureConditionByDevice = tDevID.Substring(0, 2);

                            if ((temperatureConditionByDevice != null) && Enum.TryParse(temperatureConditionByDevice, out TemperatureCondition tc))
                                listTemperatureCondition.Add(tc);
                        }

                        ManualInputParams manualInputParams = new ManualInputParams(null, listTemperatureCondition);

                        if (manualInputParams.LoadProfileParametersHandler == null)
                            manualInputParams.LoadProfileParametersHandler = this.LoadProfileParameters;

                        if (manualInputParams.GetManualParameterID(out string temperatureCondition, out int manualInputParamID, out string manualInputParamName) ?? false)
                        {
                            //пользователь выбрал параметр
                            double value = 0;

                            //спрашиваем значение выбранного параметра
                            ManualInputParamValueEditor manualInputParamValueEditor = new ManualInputParamValueEditor();

                            if (manualInputParamValueEditor.GetValue(ref value) ?? false)
                            {
                                //идём по построенному списку идентификаторов tDevIDList и выполняем сохранение введённого значения пользовательского параметра только для изделий с тепловым режимом как у выбранного им параметра
                                bool needRefresh = false;

                                foreach (string tDevID in tDevIDList)
                                {
                                    //первые два символа в tDevID это температурный режим, далее идентификатор изделия
                                    string temperatureConditionByDevice = tDevID.Substring(0, 2);
                                    string sDevID = tDevID.Remove(0, 2);

                                    if ((temperatureConditionByDevice == temperatureCondition) && int.TryParse(sDevID, out int devID))
                                    {
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
                                            DbRoutines.SaveToManualInputDevParam(connection, transaction, devID, manualInputParamID, value);
                                        }
                                        catch (Exception exc)
                                        {
                                            transaction.Rollback();
                                            MessageBox.Show(string.Concat(Properties.Resources.SaveFailed, Constants.StringDelimeter, exc.Message), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                            return;
                                        }

                                        transaction.Commit();

                                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                                        //если  оединение к БД было открыто в этой реализации - закрываем его
                                        if (connectionOpened)
                                            connection.Close();

                                        //операция сохранения данных не возбудила исключительную ситуацию - сохранение прошло успешно, перечитываем запись
                                        needRefresh = true;
                                    }
                                }

                                switch (needRefresh)
                                {
                                    case true:
                                        this.RefreshShowingData();
                                        break;

                                    default:
                                        MessageBox.Show(Properties.Resources.WrongTermalConditions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void mnuEditValueOfManuallyEnteredParameterClick(object sender, RoutedEventArgs e)
        {
            //редактирование параметра пользователя
            if (Common.Routines.IsUserCanEditValueOfManuallyEnteredParameter(this.PermissionsLo))
            {
                DataGridCellInfo currentCell = dgDevices.CurrentCell;

                if (currentCell != null)
                {
                    if ((dgDevices.CurrentCell.Column is DataGridBoundColumn column) && (dgDevices.CurrentItem is DynamicObj currentItem))
                    {
                        //получаем имя пользовательского параметра. имя начинается с обозначения температурного режима, далее само имя
                        string paramName = Common.Routines.SourceFieldNameByColumn(column);

                        //смотрим на индекс столбца в таблице
                        //столбцы параметров пользователя создаются динамически, они все стоят за индексом Constants.ParametersInDataSourceFirstIndex
                        //вычисляем значение начального индекса Parameters
                        int parametersInDataSourceFirstIndex = -1;
                        if (currentItem.GetMember(Constants.ParametersInDataSourceFirstIndex, out object objParametersInDataSourceFirstIndex))
                            int.TryParse(objParametersInDataSourceFirstIndex.ToString(), out parametersInDataSourceFirstIndex);

                        List<string> memberNames = currentItem.GetDynamicMemberNames().ToList();

                        if (memberNames.IndexOf(paramName.ToLower()) >= parametersInDataSourceFirstIndex)
                        {
                            if (currentItem.GetMember(paramName, out object objValue) && (double.TryParse(Common.Routines.SimpleFloatingValueToFloatingValue(objValue.ToString()), out double value)))
                            {
                                string trueName = Routines.ParseColumnName(paramName, out string temperatureCondition);

                                if (trueName != null)
                                {
                                    paramName = string.Concat(temperatureCondition, trueName);

                                    //проверяем, что выбранный параметр создан для ручного ввода                        
                                    if (DbRoutines.CheckManualInputParamExist(paramName, out int manualInputParamID))
                                    {
                                        //спрашиваем у пользователя значение редактируемого параметра
                                        ManualInputParamValueEditor manualInputParamValueEditor = new ManualInputParamValueEditor();

                                        if (manualInputParamValueEditor.GetValue(ref value) ?? false)
                                        {
                                            //считываем список идентификаторов изделий начинающихся с температурного режима из текущей записи (каждая запись всегда есть группа изделий)
                                            object objTDevID = this.dgDevices.ValueFromSelectedRow(Common.Constants.TDevID);

                                            if (objTDevID != null)
                                            {
                                                IEnumerable<string> tDevIDList = objTDevID.ToString().Split(new string[] { Common.Constants.cString_AggDelimeter.ToString() }, StringSplitOptions.None); //.Select(i => int.Parse(i)

                                                bool needRefresh = false;

                                                //идём по построенному списку идентификаторов и выполняем сохранение введённого значения пользовательского параметра только для изделий с тепловым режимом как у выбранного параметра
                                                foreach (string tDevID in tDevIDList)
                                                {
                                                    //первые два символа в tDevID это температурный режим, далее идентификатор изделия
                                                    string temperatureConditionByDevice = tDevID.Substring(0, 2);
                                                    string sDevID = tDevID.Remove(0, 2);

                                                    if ((temperatureConditionByDevice == temperatureCondition) && int.TryParse(sDevID, out int devID))
                                                    {
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
                                                            DbRoutines.SaveToManualInputDevParam(connection, transaction, devID, manualInputParamID, value);
                                                        }
                                                        catch (Exception exc)
                                                        {
                                                            transaction.Rollback();
                                                            MessageBox.Show(string.Concat(Properties.Resources.SaveFailed, Constants.StringDelimeter, exc.Message), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                                            return;
                                                        }

                                                        transaction.Commit();

                                                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                                                        //если  оединение к БД было открыто в этой реализации - закрываем его
                                                        if (connectionOpened)
                                                            connection.Close();

                                                        //операция сохранения данных не возбудила исключительную ситуацию - сохранение прошло успешно, перечитываем запись
                                                        needRefresh = true;
                                                    }
                                                }

                                                if (needRefresh)
                                                    this.RefreshShowingData();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //этот параметр не найден в списке параметров для ручного ввода - ругаемся 
                                        MessageBox.Show(string.Format(Properties.Resources.SelectedParameterIsNotEditable, paramName), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //этот параметр не предназначен для изменения - ругаемся
                            MessageBox.Show(Properties.Resources.SelectedDataIsNotForChange, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
            }
        }

        private void mnuDeleteValueOfManuallyEnteredParameterClick(object sender, RoutedEventArgs e)
        {
            //удаление значения параметра пользователя
            if (Common.Routines.IsUserCanDeleteValueOfManuallyEnteredParameter(this.PermissionsLo))
            {
                DataGridCellInfo currentCell = dgDevices.CurrentCell;

                if (currentCell != null)
                {
                    if ((dgDevices.CurrentCell.Column is DataGridBoundColumn column) && (dgDevices.CurrentItem is DynamicObj currentItem))
                    {
                        //получаем имя пользовательского параметра. имя начинается с обозначения температурного режима, далее само имя
                        string paramName = Common.Routines.SourceFieldNameByColumn(column);

                        //смотрим на индекс столбца в таблице
                        //столбцы параметров пользователя создаются динамически, они все стоят за индексом Constants.ParametersInDataSourceFirstIndex
                        //вычисляем значение начального индекса Parameters
                        int parametersInDataSourceFirstIndex = -1;
                        if (currentItem.GetMember(Constants.ParametersInDataSourceFirstIndex, out object objParametersInDataSourceFirstIndex))
                            int.TryParse(objParametersInDataSourceFirstIndex.ToString(), out parametersInDataSourceFirstIndex);

                        List<string> memberNames = currentItem.GetDynamicMemberNames().ToList();

                        if (memberNames.IndexOf(paramName.ToLower()) >= parametersInDataSourceFirstIndex)
                        {
                            if (currentItem.GetMember(paramName, out object objValue))
                            {
                                string trueName = Routines.ParseColumnName(paramName, out string temperatureCondition);

                                if (trueName != null)
                                {
                                    paramName = string.Concat(temperatureCondition, trueName);

                                    //проверяем, что выбранный параметр создан для ручного ввода                        
                                    if (DbRoutines.CheckManualInputParamExist(paramName, out int manualInputParamID))
                                    {
                                        //считываем список идентификаторов изделий начинающихся с температурного режима из текущей записи (каждая запись всегда есть группа изделий)
                                        object objTDevID = this.dgDevices.ValueFromSelectedRow(Common.Constants.TDevID);

                                        if (objTDevID != null)
                                        {
                                            IEnumerable<string> tDevIDList = objTDevID.ToString().Split(new string[] { Common.Constants.cString_AggDelimeter.ToString() }, StringSplitOptions.None);

                                            bool needRefresh = false;

                                            //идём по построенному списку идентификаторов и выполняем удаление места хранения значения пользовательского параметра только для изделий с тепловым режимом как у выбранного параметра
                                            foreach (string tDevID in tDevIDList)
                                            {
                                                //первые два символа в tDevID это температурный режим, далее идентификатор изделия
                                                string temperatureConditionByDevice = tDevID.Substring(0, 2);
                                                string sDevID = tDevID.Remove(0, 2);

                                                if ((temperatureConditionByDevice == temperatureCondition) && int.TryParse(sDevID, out int devID))
                                                {
                                                    DbRoutines.DeleteFromManualInputDevParam(devID, manualInputParamID);

                                                    //операция удаления данных не возбудила исключительную ситуацию - удаление прошло успешно
                                                    needRefresh = true;
                                                }
                                            }

                                            if (needRefresh)
                                                this.RefreshShowingData();
                                        }
                                    }
                                    else
                                    {
                                        //этот параметр не найден в списке параметров для ручного ввода - ругаемся 
                                        MessageBox.Show(string.Format(Properties.Resources.SelectedParameterIsNotDeletable, paramName), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //этот параметр не предназначен для изменения - ругаемся
                            MessageBox.Show(Properties.Resources.SelectedDataIsNotForChange, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
            }
        }

        private void MnuCreateDevicesClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanCreateValueOfManuallyEnteredParameter(this.PermissionsLo))
            {
                object obj = this.dgDevices.ValueFromSelectedRow(Common.Constants.GroupName);
                string groupName = obj?.ToString();

                obj = this.dgDevices.ValueFromSelectedRow("PROF_ID");
                string stringsProfileID = obj?.ToString();
                string profID = Routines.FirstInList(stringsProfileID, Constants.DelimeterForStringConcatenate.ToString());

                int.TryParse(profID, out int profileID);
                string profileGUID = DbRoutines.ProfileGUIDByProfileID(profileID);

                int apID = this.AssemblyProtocolIDFromFilter();
                int? assemblyProtocolID = (apID == -1) ? null : (int?)apID;

                ManualInputDevices manualAddDevices = new ManualInputDevices(this.ProcessWaitVisualizerHWnd)
                {
                    Owner = this
                };

                manualAddDevices.ShowModal(groupName, profileGUID, assemblyProtocolID);
            }
        }

        private void MnuShowDeviceReferencesClick(object sender, RoutedEventArgs e)
        {
            DeviceReferences deviceReferences = new DeviceReferences();
            deviceReferences.ShowModal();
        }

        private void DataGridCell_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //запрещаем возможность редактирования содержимого DataGridCell, нажатие пробела (очищает содержимое ячейки), нажатие TAB (открывает возможность изменять содержимое столбца с checkBox)
            //разрешаем выполнение: Ctrl+C, клавиши курсора
            if (sender is DataGridCell cell)
            {
                switch (e.Key)
                {
                    //запрещаем
                    case Key.Space:
                    case Key.Tab:
                    case Key.Enter:
                        e.Handled = true;
                        break;

                    //разрешаем
                    case Key.Up:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                    case Key.PageUp:
                    case Key.PageDown:
                    case Key.Home:
                    case Key.End:
                        break;

                    default:
                        //разрешаем выполнение: Ctrl+C
                        if (!cell.IsEditing && !(((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && (e.Key == Key.C)))
                        {
                            cell.IsEditing = true;
                            e.Handled = true;
                        }

                        break;
                }
            }
        }

        private int[] ObjectToArrayOfInt(object obj)
        {
            if (obj != null)
            {
                string[] stringArray = obj.ToString().Split(new[] { Common.Constants.cString_AggDelimeter }, StringSplitOptions.None);
                int[] arrayOfInt = Array.ConvertAll(stringArray, s => int.Parse(s));

                return arrayOfInt;
            }

            return null;
        }

        private int[] DevIDArrayFromSelectedRow()
        {
            //чтение массива идентификаторов Dev_ID из выбранной пользователем записи
            object obj = this.dgDevices.ValueFromSelectedRow("Dev_ID");

            return ObjectToArrayOfInt(obj);
        }

        private void WorkWithCommentsShowModalForSelectedRow(int[] devIDArray)
        {
            if (devIDArray != null)
            {
                WorkWithComments workWithComments = new WorkWithComments(devIDArray);

                if (workWithComments.ShowModal())
                    this.RefreshShowingData();
            }
        }

        /*
        private void DgDevices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            IInputElement inputElement = e.MouseDevice.DirectlyOver;

            if (inputElement != null && (inputElement is FrameworkElement element) && (element != null))
            {
                if ((element.Parent is DataGridCell cell) && (cell != null))
                {
                    if (Common.Routines.SourceFieldNameByColumn(cell.Column) == Common.Constants.DeviceComments)
                        WorkWithCommentsShowModalForSelectedRow();
                }
            }

            e.Handled = true;
        }
        */

        private string BindingPathByColumnIndex(int index)
        {
            //для столбца dgDevices с индексом index возвращает имя столбца в источнике данных
            /*
            if (dgDevices.Columns[index] is DataGridTextColumn column)
            {
                if (column.Binding is Binding bind)
                    return bind.Path.Path;
            }

            return null;
            */

            return this.dgDevices.Columns[index].SortMemberPath;
        }

        /*
                private int CalcCPNumberOfUses(DataGridBoundColumn columnForSort)
                {
                    //вычисляет число использований имени Condition/Parameter по значениям которого пользователь хочет сортировать данные
                    //данное число использований учитывает сколько раз Condition/Parameter с его температурным режимом встречается в тесте с одним и тем же наименованием теста
                    //вычисляемое число использований не имеет ничего общего с числом в конце имени столбца, отображаемом в dgDevices
                    //вычисляемое число использований Condition/Parameter используется в реализации сортировки данных
                    //возвращает:
                    //           0 - не удалось вычислить число использований Condition/Parameter по значениям которого пользователь желает выполнить сортировку данных;
                    //           значение больше или равное 1 - число использований успешно вычислено

                    int result = 0;

                    if (columnForSort != null)
                    {
                        int columnIndex = dgDevices.Columns.IndexOf(columnForSort);

                        if (columnIndex != -1)
                        {
                            //выбрасывваем индекс в конце имени
                            string columnForFound = SCME.Common.Routines.RemoveEndingNumber(this.BindingPathByColumnIndex(columnIndex));
                            int nameCount;

                            //просматриваем содержимое последней загруженной порции данных в this.DataSource для того чтобы определить число использований
                            for (int i = this.FDataProviderAsync.LastOffSet; i < this.FDataProviderAsync.LastOffSet + this.FDataProviderAsync.PortionSize; i++)
                            {
                                DynamicObj item = this.DataSource.ElementAt(i);

                                //вычисляем значение начального индекса Conditions
                                int conditionsInDataSourceStartIndex = -1;
                                if (item.GetMember(Constants.ConditionsInDataSourceFirstIndex, out object conditionsInDataSourceFirstIndex))
                                    if (!int.TryParse(conditionsInDataSourceFirstIndex.ToString(), out conditionsInDataSourceStartIndex))
                                        conditionsInDataSourceStartIndex = -1;

                                //получаем список имён столбцов текушего item
                                List<string> dynamicMemberNames = item.GetDynamicMemberNames().ToList();

                                //вычисляем сколько раз в текущем item встречается condition/parameter с именем bindingPathForFound
                                nameCount = 0;
                                for (int k = conditionsInDataSourceStartIndex; k < dynamicMemberNames.Count(); k++) //string columnName in dynamicMemberNames
                                {
                                    string columnName = dynamicMemberNames[k];

                                    if ((!Routines.Contains(columnName, Constants.HiddenMarker)) && Routines.Contains(columnName, columnForFound))
                                        nameCount++;
                                }

                                if (nameCount > result)
                                    result = nameCount;
                            }
                        }
                    }

                    return result;
                }
        */

        private void DgDevices_Sorting(object sender, DataGridSortingEventArgs e)
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

        private void CacheSetOnDuty(SCME.Common.Routines.XMLValues subject, int index, string testName, string columnName, bool temperatureMode, bool direction)
        {
            //процедура актуализации поля сортировки кеша
            //входной параметр temperatureMode - принадлежность к температурному режиму:
            //   false - холодный;
            //   true - горячий;
            //входной параметр index имеет начальное значение начиная с [1, ...]
            switch (subject)
            {
                case Common.Routines.XMLValues.UnAssigned:
                    DbRoutines.CacheDevicesSetSortingValue(columnName, direction, Common.Constants.cString_AggDelimeter);
                    break;

                case Common.Routines.XMLValues.Conditions:
                    if (index >= 1)
                        DbRoutines.CacheConditionsSetSortingValue(index, testName, columnName, temperatureMode, direction, Common.Constants.cString_AggDelimeter);
                    break;

                case Common.Routines.XMLValues.Parameters:
                    if (index >= 1)
                        DbRoutines.CacheParametersSetSortingValue(index, testName, columnName, temperatureMode, direction, Common.Constants.cString_AggDelimeter);
                    break;

                case Common.Routines.XMLValues.ManuallyParameters:
                    if (index >= 1)
                        DbRoutines.CacheManuallyParametersSetSortingValue(index, columnName, temperatureMode, direction, Common.Constants.cString_AggDelimeter);
                    break;
            }
        }

        private double? StringToNullableDouble(string value)
        {
            double? result = Common.Routines.TryStringToDouble(value, out double d) ? (double?)d : null;

            return result;
        }

        private int CacheSetOnDuty(SCME.Common.Routines.XMLValues subject, byte comparison, int index, string testName, string columnName, bool temperatureMode, IEnumerable<string> values)
        {
            //процедура актуализации данных в кеше с целью выполнения фильтрации
            //входной параметр temperatureMode - принадлежность к температурному режиму:
            //   false - холодный;
            //   true - горячий;
            //входной параметр index имеет начальное значение начиная с [1, ...]
            //возвращает количество отфильтрованных записей кеша (удалённых из кеша записей)

            int result = 0;

            switch (subject)
            {
                case Common.Routines.XMLValues.UnAssigned:
                    result = DbRoutines.CacheDevicesApplyFilter(columnName, comparison, values, Common.Constants.cString_AggDelimeter);
                    break;

                case Common.Routines.XMLValues.Conditions:
                    if (index >= 1)
                        result = DbRoutines.CacheConditionsApplyFilter(index, testName, columnName, temperatureMode, comparison, values, Common.Constants.cString_AggDelimeter);

                    break;

                case Common.Routines.XMLValues.Parameters:
                    if (index >= 1)
                    {
                        //преобразуем принятый список значений параметров IEnumerable<string> в IEnumerable<double?>
                        IEnumerable<double?> parametersValues = values.Select(x => this.StringToNullableDouble(x));

                        /*
                        List<double?> parametersValues = new List<double?>();

                        foreach (string sParamValue in values)
                        {
                            double? dValue = Common.Routines.TryStringToDouble(sParamValue, out double d) ? (double?)d : null;
                            parametersValues.Add(dValue);
                        }
                        */

                        result = DbRoutines.CacheParametersApplyFilter(index, testName, columnName, temperatureMode, comparison, parametersValues, Common.Constants.cString_AggDelimeter);
                    }

                    break;

                case Common.Routines.XMLValues.ManuallyParameters:
                    if (index >= 1)
                    {
                        //преобразуем принятый список значений параметров IEnumerable<string> в IEnumerable<double?>
                        IEnumerable<double?> manuallyParametersValues = values.Select(x => this.StringToNullableDouble(x));

                        /*
                        List<double?> manuallyParametersValues = new List<double?>();

                        foreach (string sManuallyParamValue in values)
                        {
                            double? dValue = Common.Routines.TryStringToDouble(sManuallyParamValue, out double d) ? (double?)d : null;
                            manuallyParametersValues.Add(dValue);
                        }
                        */

                        result = DbRoutines.CacheManuallyParametersApplyFilter(index, columnName, temperatureMode, comparison, manuallyParametersValues, Common.Constants.cString_AggDelimeter);
                    }

                    break;
            }

            return result;
        }

        private void RefreshShowingData()
        {
            //обновление отображаемых данных из-за сортировки или фильтрации
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                //если у this.dgDevices на момент выполнения данной реализации выделена целая строка - данная реализация начинает бесконечно перегружать текущую страницу данных
                //почему так происходит не понял, избавится от этого можно сбросив выделение строки
                this.dgDevices.SelectedItem = null;

                //уничтожаем this.FDataSource для принудительного перечитывания данных в this.FDataSource
                this.FDataSource = null;
                this.dgDevices.ItemsSource = this.DataSource;
            }));
        }

        private void DataGridCellCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
                {
                    //данное событие сработает только при ручной установке CheckBox
                    //убираем фокус т.к. при повторном клике мышкой по имеющем фокус DataGridCell размер CheckBox отображаемый в данной ячейке уменьшается до размеров которые CheckBox имеет по умолчанию - станет слишком мелким
                    cb.Focusable = false;

                    //считываем список идентификаторов изделий записанных через разделитель из записи в которой пользователь тычет в cb
                    object value = this.dgDevices.ValueFromSelectedRow(Common.Constants.DevID);

                    if (value != null)
                    {
                        IEnumerable<string> devIDEnumerable = Routines.DelimetedStringsToEnumerable(value.ToString(), SCME.Common.Constants.cString_AggDelimeter.ToString());
                        switch (cb.IsChecked)
                        {
                            //пользователь выбрал группу
                            case true:
                                DbRoutines.SetDevicesChoice(devIDEnumerable, true);
                                break;

                            //пользователь снял отметку выбора группы - сбрасываем флаг выбора с изделий данной группы
                            default:
                                DbRoutines.SetDevicesChoice(devIDEnumerable, false);
                                break;
                        }
                    }

                    e.Handled = true;
                }
                else
                {
                    e.Handled = true;
                    MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ContextMenuSelectRecords_Click(object sender, RoutedEventArgs e)
        {
            //установка отметок выбора для всех показанных групп изделий
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                //данное действие предполагает фильтрацию данных
                SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

                DbRoutines.SetChoiceForAllDevicesInCache();
                //HideProcessWaitVisualizerSortingFiltering будет вызван при полном выгребании отложенной очереди исполняемой в потоке System.Windows.Threading.DispatcherTimer

                this.RefreshShowingData();
            }
            else
                MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private int? ReadIntValueByNameFromContextMenu(object sender, string name, out string stringValue)
        {
            //чтение целочисленного значения поля из контекстного меню (число введённое пользователем)
            stringValue = null;

            if (sender is MenuItem mnu)
            {
                //ищем в mnu поле ввода количества
                //оно в нём в единственном числе
                TextBox tbIntValue = null;

                foreach (TextBox tb in Common.Routines.FindVisualChildren<TextBox>(mnu))
                {
                    if (tb.Name == name)
                    {
                        tbIntValue = tb;
                        stringValue = tbIntValue.Text;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(stringValue))
                {
                    if (int.TryParse(tbIntValue.Text, out int intValue))
                        return intValue;
                }
            }

            return null;
        }

        private void ContextMenuSelectByNumberOfRecords_Click(object sender, RoutedEventArgs e)
        {
            //установка отметок выбора для заданного пользователем количества записей
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                int? intValue = this.ReadIntValueByNameFromContextMenu(sender, "tbQuantity", out string stringValue);

                if (intValue == null)
                {
                    MessageBox.Show(string.Format(Properties.Resources.ValueIsNotInteger, stringValue), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    int rowCount = (int)intValue;

                    //данное действие предполагает фильтрацию данных
                    SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);
                    DbRoutines.SetChoiceForRowCountDevicesInCache(rowCount);

                    //HideProcessWaitVisualizerSortingFiltering будет вызван при полном выгребании отложенной очереди исполняемой в потоке System.Windows.Threading.DispatcherTimer

                    this.RefreshShowingData();
                }
            }
            else
                MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ContextMenuUnSelectRecords_Click(object sender, RoutedEventArgs e)
        {
            //сброс отметок выбора со всех показанных групп изделий
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                //данное действие предполагает фильтрацию данных
                SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);
                DbRoutines.DropChoiceForAllDevicesInCache();
                //HideProcessWaitVisualizerSortingFiltering будет вызван при полном выгребании отложенной очереди исполняемой в потоке System.Windows.Threading.DispatcherTimer

                this.RefreshShowingData();
            }
            else
                MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void AssemblyProtocolModeOn(int descr)
        {
            //узнаём обозначение статуса сборки с AssemblyStatusID=1
            string assemblyStatusDescr = DbRoutines.AssemblyDescrByAssemblyStatusID(1);

            //удаляем все имеющиеся на данный момент фильтры
            this.FActiveFilters.Clear();

            //формируем фильтр по принятому обозначению протокола сборки
            CustomControls.FilterDescription filterByAssemblyProtocolDescr = new CustomControls.FilterDescription(this.FActiveFilters, Common.Constants.AssemblyProtocolDescr) { Type = typeof(System.Int32).FullName, TittlefieldName = Properties.Resources.AssemblyProtocol, Comparison = "=", Value = descr, AcceptedToUse = true };

            //формируем фильтр по значению статуса сборки
            CustomControls.FilterDescription filterByAssemblyStatusDescr = new CustomControls.FilterDescription(this.FActiveFilters, Common.Constants.AssemblyStatusDescr) { Type = typeof(System.String).FullName, TittlefieldName = Properties.Resources.AssemblyStatusDescr, Comparison = "=", Value = assemblyStatusDescr, AcceptedToUse = true };

            //добавляем сформированные фильтры
            this.FActiveFilters.Add(filterByAssemblyProtocolDescr);
            this.FActiveFilters.Add(filterByAssemblyStatusDescr);

            //устанавливать this.AssemblyProtocolMode=true перед применением фильтров нельзя - сроллинг и форма будут работать с жуткими тормозами

            //применяем фильтры
            this.ApplyFilters();

            //ставим в конец очереди
            this.FQueueManager.Enqueue(
                                        delegate
                                        {
                                            //включение режима просмотра протокола сборки
                                            this.AssemblyProtocolMode = true;

                                            //строим шапку протокола сборки
                                            if (int.TryParse(this.lbRecordCount.Content.ToString(), out int cacheSize))
                                                this.BuildHeadOfAssemblyProtocol(cacheSize);

                                            //перестраиваем столбцы которые отображают conditions/parameters в dgDevices
                                            this.ReBuildCPColumnsForAssemblyProtocolMode();
                                        }
                                      );
        }

        private void ContextMenuCreateAssemblyProtocol_Click(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                //данное действие предполагает фильтрацию данных
                SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

                //создание протокола сборки, установка состояния 'Сборка' для выбранных групп из показанных в данный момент пользователю
                int assemblyProtocolID = DbRoutines.CreateAssemblyProtocol(this.TabNum, this.AssemblyProtocolMode);

                //HideProcessWaitVisualizerSortingFiltering будет вызван при полном выгребании отложенной очереди исполняемой в потоке System.Windows.Threading.DispatcherTimer

                if (assemblyProtocolID <= 0)
                {
                    MessageBox.Show(string.Concat(Properties.Resources.OperationFailed, ". ", Properties.Resources.FailedToCreate, " ", Properties.Resources.AssemblyProtocol.ToLowerInvariant(), "."), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                //по полученному идентификатору вычисляем обозначение созданного протокола сборки
                int descr = DbRoutines.DescrByAssemblyProtocolID(assemblyProtocolID);

                this.AssemblyProtocolModeOn(descr);
            }
            else
                MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ContextMenuSendSelectedToAssemblyProtocol_Click(object sender, RoutedEventArgs e)
        {
            //отправка выбранных групп измерений в уже существующий протокол сборки
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                int? intValue = this.ReadIntValueByNameFromContextMenu(sender, "tbAssemblyProtocolDescr", out string stringValue);

                if (intValue == null)
                {
                    MessageBox.Show(string.Format(Properties.Resources.ValueIsNotInteger, stringValue), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    int descr = (int)intValue;
                    int assemblyProtocolID = DbRoutines.AssemblyProtocolIDByDescr(descr);

                    if (assemblyProtocolID == -1)
                    {
                        MessageBox.Show(string.Concat(Properties.Resources.AssemblyProtocol, " № ", descr.ToString(), " ", Properties.Resources.DoesNotExist, "."), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        //идентификатор протокола сборки успешно вычислен по его обозначению
                        //отправляем выбранные пользователем группы измерений в протокол сборки
                        if (DbRoutines.SendToAssemblyProtocol(this.AssemblyProtocolMode, assemblyProtocolID))
                        {
                            //удаляем все протоколы сборки, которые не содержат в себе ни одной строки
                            DbRoutines.DeleteEmptyAssemblyProtocols();

                            if (this.AssemblyProtocolMode)
                                this.RefreshShowingData();

                            MessageBox.Show(string.Format(Properties.Resources.SelectedSuccessfullySendedToAssemblyProtocol, descr.ToString()), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            else
                MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void AfterPortionDataLoadedRoutines()
        {
            //вызывается по факту загрузки порции данных из кеша
            this.VisualiseDynamicColumnsInDataGrid();
        }

        public void VisualiseDynamicColumnsInDataGrid()
        {
            //на момент вызова данной реализации в очереди сообщений уже стоят сообщения на создание всех столбцов DataGrid полученных из XML (conditions/parameters)
            //добавляем в очередь сообщение на создания столбца сортировки с целью всегда показать пользователю столбец по которому выполнена сортировка данных
            this.BuildColumnInDataGrid(this.dgDevices.LastHeaderTextClicked, this.dgDevices.LastSourceFieldNameClicked);
        }

        private void SetColumnsVisibilityByDeviceTypeRu(string deviceTypeRu)
        {
            //прячем столбцы в dgDevices если они не содержат в Header полученных parameters
            //получаем список столбцов которые должны быть для отображаемого в протоколе сборки типа изделий
            if (this.AssemblyProtocolMode && !string.IsNullOrEmpty(deviceTypeRu))
            {
                string[] parameters = Routines.AssemblyProtocolParametersByDeviceTypeRu(deviceTypeRu);

                for (int i = Constants.StartConditionsParamersInDataGridIndex; i < dgDevices.Columns.Count; i++)
                {
                    DataGridColumn column = dgDevices.Columns[i];

                    //нам нужны только такие столбцы, которые отображают данные conditions/parameters
                    if (Routines.IsCPColumn(column))
                    {
                        string nameFromHeader = SCME.Common.Routines.RemoveEndingNumber(column.Header.ToString()).ToUpper();

                        bool needToShow = true;

                        if (parameters != null)
                            needToShow = parameters.Any(p => nameFromHeader.EndsWith(p)); //|| nameFromHeader.Contains(DbRoutines.cManually

                        column.Visibility = needToShow ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        private int CacheEdit()
        {
            //установка значений поля сортировки кеша и удаление из кеша записей не удовлетворяющих критериям фильтрации
            //данная реализация будет вызвана сразу после формирования данных кеша перед вызовом this.AfterBuildingDataInCacheRoutines
            //смотрим какие сортированные данные нам требуется получать: простые реквизиты, условия или параметры
            //возвращает сколько записей кеша удалил вызов данной реализации
            string testTypeName = null;
            string subjectNameInDB = null;
            bool temperatureMode = false;
            SCME.Common.Routines.XMLValues subject;
            int index = 1;

            int deletedSummCount = 0;

            //требуется фильтрация данных                
            if (this.FActiveFilters.Count > 0)
            {
                byte comparison;

                this.FActiveFilters.Correct();

                foreach (CustomControls.FilterDescription filter in this.FActiveFilters)
                {
                    subject = SCME.Common.Routines.SubjectByPath(filter.FieldName);
                    comparison = Routines.ComparisonToByte(filter.ComparisonCorrected);

                    /*
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
                                value = ((bool)filter.Value) ? "true" : null;

                            break;

                        default:
                            value = filter.Value?.ToString();
                            break;
                    }
                    */

                    if ((subject == Common.Routines.XMLValues.Conditions) || (subject == Common.Routines.XMLValues.Parameters) || (subject == Common.Routines.XMLValues.ManuallyParameters))
                    {
                        //считываем тип теста, он нужен только для Conditions/Parameters
                        if ((subject == Common.Routines.XMLValues.Conditions) || (subject == Common.Routines.XMLValues.Parameters))
                            testTypeName = SCME.Common.Routines.ExtractXMLTestFromPath(filter.FieldName);

                        subjectNameInDB = SCME.Common.Routines.ExtractXMLNameFromPath(filter.FieldName);
                        index = Routines.EndingNumberFromValue(filter.FieldName);
                        temperatureMode = filter.TemperatureMode() != "RT";
                    }
                    else
                        subjectNameInDB = filter.FieldName;

                    //если имеем дело с маской - имеем только одно значение в filter.Values
                    //меняем понятный пользователю символ замены не известного * на понятный базе данных %
                    FilterValues values = null;

                    if ((comparison == 5) && (filter.Type.ToString() == "System.String") && (filter.Values.Count == 1))
                    {
                        values = new FilterValues(null);
                        values.NewFilterValue().Value = filter.Values[0].Value.ToString().Replace("*", "%");
                    }
                    else
                        values = filter.Values;

                    //удаляем из кеша записи которые не удовлетворяют условиям фильтрации
                    int deletedCount = this.CacheSetOnDuty(subject, comparison, index, testTypeName, subjectNameInDB, temperatureMode, values.Values());

                    //вычисляем сколько было удалено записей (сумму)
                    deletedSummCount += deletedCount;

                    //фильтрация данных выполнена
                }
            }

            //требуется сортировка данных
            if (!string.IsNullOrEmpty(this.FSortSourceFieldName))
            {
                //определяем с чем мы имеем дело: с conditions или parameters
                subject = SCME.Common.Routines.SubjectByPath(this.FSortSourceFieldName);

                if ((subject == Common.Routines.XMLValues.Conditions) || (subject == Common.Routines.XMLValues.Parameters) || (subject == Common.Routines.XMLValues.ManuallyParameters))
                {
                    //имеем дело с Conditions/Parameters/ManuallyParameters
                    index = Routines.EndingNumberFromValue(this.FSortSourceFieldName);

                    //считываем тип теста, он нужен только для Conditions/Parameters
                    if ((subject == Common.Routines.XMLValues.Conditions) || (subject == Common.Routines.XMLValues.Parameters))
                        testTypeName = SCME.Common.Routines.ExtractXMLTestFromPath(this.FSortSourceFieldName);

                    //считываем наименование условия/параметра как оно записано в базе данных по имени столбца
                    subjectNameInDB = SCME.Common.Routines.ExtractXMLNameFromPath(this.FSortSourceFieldName);

                    //узнаём температурный режим выбранного для сортировки столбца
                    temperatureMode = this.FSortSourceFieldName.Substring(0, 2).ToUpper() != "RT";
                }
                else
                    subjectNameInDB = this.FSortSourceFieldName;

                //вычисляем направление сортировки
                bool direction = (this.dgDevices.LastSortedDirection == ListSortDirection.Ascending) ? false : true;

                //устанавливаем значение поля сортировки в кеше
                this.CacheSetOnDuty(subject, index, testTypeName, subjectNameInDB, temperatureMode, direction);

                //сортировка данных выполнена
            }

            return deletedSummCount;
        }

        private void AfterBuildingDataInCacheRoutines(int cacheSize)
        {
            //показываем количество отображаемых записей
            this.ShowRecordCount(cacheSize);

            //прокручиваем данные до первой записи не NULL значением в поле сортировки
            this.ScrollToFirstNotNullSortingValue();

            //формируем шапку протокола испытаний
            this.BuildHeadOfAssemblyProtocol(cacheSize);
        }

        private void ScrollToFirstNotNullSortingValue()
        {
            //прокручиваем данные в this.dgDevices до первой записи с не NULL значением в поле по которому выполнена сортировка
            //делаем это только один раз если это ещё не сделано
            if (this.dgDevices.ScrolledAfterSortingToNotNullValueRecord == false)
            {
                int rowNum = SCME.Types.DbRoutines.FirstRowNumByNotNullSortingValue();

                if (rowNum != -1)
                {
                    this.FQueueManager.Enqueue(
                                               delegate
                                               {
                                                   object item = this.dgDevices.Items[rowNum];

                                                   if (item != null)
                                                   {
                                                       this.dgDevices.UpdateLayout();
                                                       this.dgDevices.ScrollIntoView(item);

                                                       //устанавливаем флаг о прошедшем скроллинге
                                                       this.dgDevices.ScrolledAfterSortingToNotNullValueRecord = true;
                                                   }
                                               });
                }
            }
        }

        private string ReadSelectedComboBoxValue(ComboBox cmb)
        {
            //считываем символьное обозначение группы (которую визуализирует принятый cmb) или её цифровое обозначение в зависимости от состояния cbDeviceModeView
            string result = null;

            if ((cmb != null) && (cmb.SelectedItem != null))
            {
                if (cmb.SelectedItem is GroupOfValues groupOfValues)
                {
                    switch (cbDeviceModeView.IsChecked)
                    {
                        case true:
                            result = groupOfValues.Num;
                            break;

                        default:
                            result = groupOfValues.Group;
                            break;
                    }
                }
            }

            return result;
        }

        private void BuildDevice()
        {
            //строим обозначение изделия-результата          
            string selectedDeviceTypeRU = string.Empty;
            string selectedDeviceTypeEN = string.Empty;
            if (this.cmbDeviceType.SelectedItem is string[] deviceTypeSelectedItem)
            {
                if (deviceTypeSelectedItem != null)
                {
                    selectedDeviceTypeRU = deviceTypeSelectedItem[1];
                    selectedDeviceTypeEN = deviceTypeSelectedItem[2];
                }
            }

            string modification = string.Empty;
            if (this.cmbModification.SelectedItem is string modificationSelectedItem)
                modification = modificationSelectedItem;

            string climatic = string.Empty;
            if (this.cmbClimatic.SelectedItem is ComboBoxItem climaticSelectedItem)
                if (climaticSelectedItem != null)
                    climatic = climaticSelectedItem.Content.ToString();

            this.lbDevice.Content = Routines.CalcDeviceDescr(selectedDeviceTypeRU, selectedDeviceTypeEN, (this.cbExport.IsChecked == true), this.tbConstructive.Text, this.ReadSelectedComboBoxValue(this.cmbDUDt), this.ReadSelectedComboBoxValue(this.cmbTq), this.ReadSelectedComboBoxValue(this.cmbTrr), this.ReadSelectedComboBoxValue(this.cmbTgt), modification, climatic, this.tbAverageCurrent.Text, this.tbDeviceClass.Text);
        }

        private void TbTextChanged(object sender, TextChangedEventArgs e)
        {
            this.BuildDevice();
        }

        private void ApplyFilterByComboBoxSelection(object sender)
        {
            //применение фильтров в соответствии с выбранным пользователем значением в comboBox: cmbDUDt, cmbTrr, cmbTq, cmbTgt, cmbQrr

            //не устанавливаем и не применяем фильтры по изменению выбранных значений в указанных comboBox в режиме протокола сборки
            if (!this.AssemblyProtocolMode && (sender is ComboBox cmb))
            {
                //узнаём имя условия/параметра для формирования фильтра, тип фильтра, наименование фильтра для демонстрации пользователю
                string fieldName = this.FilterFieldNameByComboBox(cmb, out string tittlefieldName, out string filterType, out string comparison);

                if (!string.IsNullOrEmpty(fieldName))
                {
                    string value = this.SelectedValueFromComboBox(cmb);

                    //если после открытия выпадающего списка ComboBox пользователь нажал клавишу Escape - выбранное значение будет null - проверим это
                    if (!string.IsNullOrEmpty(value))
                    {
                        CustomControls.FilterDescription filter = null;

                        bool keyCtrlPressed = Common.Routines.IsKeyCtrlPressed();

                        if (!keyCtrlPressed)
                        {
                            //удаляем уже имеющийся фильтр по полю fieldName (их может быть либо ни одного, либо один единственный)
                            filter = this.FActiveFilters.Where(f => f.FieldName == fieldName).FirstOrDefault();

                            if (filter != null)
                                this.FActiveFilters.Remove(filter);
                        }

                        //создаём и применяем фильтр по данным, которые выбрал пользователь в cmb
                        filter = new CustomControls.FilterDescription(this.FActiveFilters, fieldName) { Type = filterType, TittlefieldName = tittlefieldName, Comparison = comparison, Value = value };
                        this.FActiveFilters.Add(filter);

                        //важно устанавливать флаг AcceptedToUse только после того, как filter будет добавлен в список this.FActiveFilters
                        filter.AcceptedToUse = true;

                        if (!keyCtrlPressed)
                            this.ApplyFilters();
                    }
                }
            }
        }

        private void CmbSelectionChangedEventHandler(object sender, SelectionChangedEventArgs e)
        {
            //общий обработчик изменения выбранного значения в combobox которые формируют значение итогого изделия lbResult
            if (sender == this.cmbDeviceType)
            {
                string deviceTypeRU = (this.cmbDeviceType.SelectedItem == null) ? null : ((string[])this.cmbDeviceType.SelectedItem)[1];
                this.SetColumnsVisibilityByDeviceTypeRu(deviceTypeRU);

                this.BuildDevice();

                return;
            }

            if ((sender == this.cmbModification) || (sender == this.cmbDUDt) || (sender == this.cmbTrr) || (sender == this.cmbTq) || (sender == this.cmbTgt) || (sender == this.cmbClimatic))
                this.BuildDevice();
        }

        private void CmbDropDownClosedEventHandler(object sender, EventArgs e)
        {
            //один и тот же обработчик для перечисленных ниже comboBox
            if ((sender == this.cmbDUDt) || (sender == this.cmbTrr) || (sender == this.cmbTq) || (sender == this.cmbTgt) || (sender == this.cmbQrr))
                this.ApplyFilterByComboBoxSelection(sender);
        }

        public void SetComboBoxSelected(ComboBox cmb, object cmbItem)
        {
            if (cmb != null)
            {
                //чтобы при установке cmb.SelectedItem не сработало его событие SelectionChanged
                cmb.SelectionChanged -= this.CmbSelectionChangedEventHandler;

                try
                {
                    switch (cmbItem == null)
                    {
                        case true:
                            //значение cmbItem отсутствует в cmb
                            cmb.SelectedIndex = -1;
                            break;

                        default:
                            cmb.SelectedItem = cmbItem;
                            break;
                    }
                }
                finally
                {
                    cmb.SelectionChanged += this.CmbSelectionChangedEventHandler;
                }
            }
        }

        private void ClearSelectedItemInComboBoxFilters()
        {
            if (this.cmbDUDt.SelectedItem != null)
                this.SetComboBoxSelected(this.cmbDUDt, null);

            if (this.cmbTrr.SelectedItem != null)
                this.SetComboBoxSelected(this.cmbTrr, null);

            if (this.cmbTq.SelectedItem != null)
                this.SetComboBoxSelected(this.cmbTq, null);

            if (this.cmbTgt.SelectedItem != null)
                this.SetComboBoxSelected(this.cmbTgt, null);

            if (this.cmbQrr.SelectedItem != null)
                this.SetComboBoxSelected(this.cmbQrr, null);
        }

        private void PrepareHeadOfAssemblyProtocol(bool needSaveAssemblyProtocol, bool? deviceModeView, string assemblyJob, bool? export, int? deviceTypeID, string constructive, int? itav, string modification, int? deviceClass, int? dUdt, double? trr, string tq, double? tgt, int? qrr, string climatic, int? omnity)
        {
            //построение шапки протокола сборки
            //формируем значения в шапке протокола сборки
            //числовое обозначение групп
            this.cbDeviceModeView.IsChecked = deviceModeView ?? false;

            //ПЗ сборки
            this.tbAssemblyJob.Text = assemblyJob;

            //Export
            this.cbExport.IsChecked = export ?? false;

            //тип изделия comboBoxStringArray
            //каждый элемент списка есть массив, в нулевом индексе которого хранится идентификатор типа изделия, в первом индексе строковое значение типа на русском языке
            string[] deviceTypeItem = this.cmbDeviceType.Items.OfType<string[]>().FirstOrDefault(x => int.Parse(x[0]) == deviceTypeID);
            this.SetComboBoxSelected(this.cmbDeviceType, deviceTypeItem);

            //фильтр по протоколу испытаний на момент вызова данной реализации уже отработал
            //и из-за того, что на момент получения данных из кеша значения типа было не определённым - требуемая (по типу изделия) видимость столбцов в dgDevices не была задана
            //поэтому устанавливаем требуемую видимость столбцов здесь
            string deviceTypeRu = deviceTypeItem?[1];
            this.SetColumnsVisibilityByDeviceTypeRu(deviceTypeRu);

            //конструктив
            this.tbConstructive.Text = constructive ?? string.Empty;

            //средний ток
            this.tbAverageCurrent.Text = (itav == null) ? string.Empty : itav.ToString();

            //модификация
            string modificationItem = this.cmbModification.Items.OfType<string>().FirstOrDefault(x => x == modification);
            this.SetComboBoxSelected(this.cmbModification, modificationItem);

            //класс изделия
            this.tbDeviceClass.Text = (deviceClass == null) ? string.Empty : deviceClass.ToString();

            //dUdt
            string sdUdt = (dUdt == null) ? string.Empty : dUdt.ToString();
            GroupOfValues cmbItem = this.cmbDUDt.Items.OfType<GroupOfValues>().FirstOrDefault(x => x.Value == sdUdt);
            this.SetComboBoxSelected(this.cmbDUDt, cmbItem);

            //trr
            string sTrr = (trr == null) ? string.Empty : trr.ToString();
            cmbItem = this.cmbTrr.Items.OfType<GroupOfValues>().FirstOrDefault(x => x.Value == sTrr);
            this.SetComboBoxSelected(this.cmbTrr, cmbItem);

            //tq список в cmbTq состоит из списка значений для быстродействующих тиристоров (TI, TS) и низкочастотных тиристоров (TR, TG)
            //каждое значение списка начинается с указания типа тиристора
            //есть два случая установки значения в combobox:
            // - установка по сохранённому пользователем значению tq - имеем дело с полным значением tq;
            // - установка по наиболее часто используемому значению, вычисленному по списку значений параметра tq из кеша - имеем дело с числовым значением tq без указания типа тиристора - поэтому ищем вхождение строки tq
            cmbItem = null;

            if (!string.IsNullOrEmpty(tq))
            {
                cmbItem = this.cmbTq.Items.OfType<GroupOfValues>().FirstOrDefault(x => x.Value == tq);

                //если полное соответствие tq не найдено - пытаемся найти первое попавшееся вхождение в строку - тип тиристора при этом учитываться не будет
                if (cmbItem == null)
                    cmbItem = this.cmbTq.Items.OfType<GroupOfValues>().FirstOrDefault(x => x.Value.Contains(tq));
            }
            this.SetComboBoxSelected(this.cmbTq, cmbItem);

            //tgt
            string sTgt = (tgt == null) ? string.Empty : tgt.ToString();
            cmbItem = this.cmbTgt.Items.OfType<GroupOfValues>().FirstOrDefault(x => x.Value == sTgt);
            this.SetComboBoxSelected(cmbTgt, cmbItem);

            //qrr
            string sQrr = (qrr == null) ? string.Empty : qrr.ToString();
            QrrGroupOfValues cmbQrrItem = this.cmbQrr.Items.OfType<QrrGroupOfValues>().FirstOrDefault(x => x.Value == sQrr);
            this.SetComboBoxSelected(this.cmbQrr, cmbQrrItem);

            //climatic
            ComboBoxItem comboBoxItem = this.cmbClimatic.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString() == climatic);
            this.SetComboBoxSelected(this.cmbClimatic, comboBoxItem);

            //siOmnity
            tbOmnity.Text = (omnity == null) ? string.Empty : omnity.ToString();

            //строим обозначение прибора
            this.BuildDevice();

            if (needSaveAssemblyProtocol)
                this.SaveAssemblyProtocol();
        }

        private void ShowRecordCount(int recordCount)
        {
            //выводим количество отобранных для отображения записей
            this.FQueueManager.Enqueue(
                                       delegate
                                       {
                                           this.lbRecordCount.Content = recordCount.ToString();
                                       });
        }

        private int? AssemblyProtocolID(DynamicObj cacheItem)
        {
            //извлечение из принятого cacheItem идентификатора протокола сборки
            if (this.AssemblyProtocolMode)
            {
                if (cacheItem != null)
                {
                    if (cacheItem.GetMember(Common.Constants.AssemblyProtocolID, out object assemblyProtocolID))
                    {
                        string sAssemblyProtocolID = Routines.RemoveEmptyValuesAndDuplicates(assemblyProtocolID.ToString());

                        if (int.TryParse(sAssemblyProtocolID, out int result))
                            return result;
                    }
                }
            }

            return null;
        }

        private void BuildHeadOfAssemblyProtocol(int cacheSize)
        {
            //строим шапку протокола испытаний            
            if (this.AssemblyProtocolMode)
            {
                bool? deviceModeView = null;
                string assemblyJob = null;
                bool? export = null;
                int? deviceTypeID = null;
                string constructive = null;
                int? itav = null;
                string modification = null;
                int? deviceClass = null;
                int? dUdt = null;
                double? trr = null;
                string tq = null;
                double? tgt = null;
                int? qrr = null;
                string climatic = null;
                int? omnity = null;

                bool allRequisitesAreEmpty = true;

                //смотрим сохранял ли пользователь реквизиты протокола сборки в базу данных
                //если пользователь их сохранял ранее - отображаем эти сохранённые значения;
                //если пользователь не сохранил ни одного реквизита протокола сборки - вычисляем их значения по содержимому кеша;
                int assemblyProtocolID = this.AssemblyProtocolIDFromFilter();

                if (assemblyProtocolID != -1)
                {
                    //смотрим сохранил ли пользователь хотя бы один реквизит протокола испытаний
                    DbRoutines.LoadAssemblyProtocol((int)assemblyProtocolID, out deviceModeView, out assemblyJob, out export, out deviceTypeID, out itav, out modification, out constructive, out deviceClass, out dUdt, out trr, out tq, out tgt, out qrr, out climatic, out omnity);
                    allRequisitesAreEmpty = string.IsNullOrEmpty(assemblyJob) && (deviceTypeID == null) && (itav == null) && (modification == null) && (constructive == null) && (deviceClass == null) && (dUdt == null) && (trr == null) && string.IsNullOrEmpty(tq) && (tgt == null) && (qrr == null) && string.IsNullOrEmpty(climatic) && (omnity == null);

                    if (allRequisitesAreEmpty)
                    {
                        if (cacheSize > 0)
                        {
                            //все отображаемые записи имеют один и тот же протокол сборки
                            //читаем из первой записи кеша идентификатор протокола сборки
                            //получаем содержимое кеша
                            List<DynamicObj> cacheData = new List<DynamicObj>();
                            Routines.GetCacheData(cacheData, cacheSize);

                            //пользователь не сохранил ни одного реквизита протокола испытаний - предлагаем ему самые часто используемые значения вычисленные по отображаемым данным
                            //вычисляем самый часто используемый тип изделий, хранящихся в кеше
                            string sDeviceTypeID = Routines.MostPopularValue(cacheData, "DEVICETYPEID");
                            string deviceTypeRu = null;

                            //по вычисленному самому часто используемому типу изделия вычисляем идентификатор типа
                            if (!string.IsNullOrEmpty(sDeviceTypeID))
                            {
                                deviceTypeID = int.TryParse(sDeviceTypeID, out int iDeviceTypeID) ? (int?)iDeviceTypeID : null;

                                if (deviceTypeID != null)
                                    DbRoutines.DeviceTypeByDeviceTypeID((int)deviceTypeID, out deviceTypeRu, out string deviceTypeEn);
                            }

                            //вычисляем самый часто используемый конструктив изделий, хранящихся в кеше
                            constructive = Routines.MostPopularValue(cacheData, Common.Constants.Constructive);

                            //вычисляем самый часто используемый средний ток изделий, хранящихся в кеше
                            if (int.TryParse(Routines.MostPopularValue(cacheData, Common.Constants.AverageCurrent), out int iItav))
                                itav = iItav;

                            //вычисляем самую часто используемую модификацию, хранящихся в кеше (в кеше нет этого поля)
                            //modification = Routines.MostPopularValue(cacheData, "MODIFICATION");

                            //вычисляем минимальное значение класса для списка изделий, хранящихся в кеше
                            if (int.TryParse(Routines.MinValue(cacheData, Common.Constants.DeviceClass), out int iDeviceClass))
                                deviceClass = iDeviceClass;

                            //вычисляем минимальное значение DVDT_VoltageRate для списка изделий, хранящихся в кеше
                            if (int.TryParse(Routines.MinCPValue(cacheData, "DVDT_VoltageRate"), out int idUdt))
                                dUdt = idUdt;

                            //вычисляем максимальное значение TRR для списка изделий, хранящихся в кеше
                            if (double.TryParse(Routines.MaxCPValue(cacheData, Common.Constants.Trr), out double dTrr))
                                trr = dTrr;

                            //вычисляем максимальное значение TQ для списка изделий, хранящихся в кеше
                            tq = Routines.MaxCPValue(cacheData, Common.Constants.Tq);

                            //вычисляем максимальное значение TOU_TGT для списка изделий, хранящихся в кеше
                            if (double.TryParse(Routines.MaxCPValue(cacheData, "TOU_TGT"), out double dTgt))
                                tgt = dTgt;

                            //qrr вычисляется по вычисленным deviceTypeRu, constructive, itav
                            qrr = Routines.CalcQrr(deviceTypeRu, constructive, itav);

                            //вычисляем самый часто используемый климат, хранящихся в кеше (в кеше нет этого поля)
                            //climatic = Routines.MostPopularValue(cacheData, Common.Constants.Climatic);

                            //вычисляем самое часто используемое значение Omnity (в кеше нет этого поля)
                            if (int.TryParse(Routines.MostPopularValue(cacheData, "SIOMNITY"), out int iSiOmnity))
                                omnity = iSiOmnity;
                        }
                    }
                }

                this.FQueueManager.Enqueue(
                                            delegate
                                            {
                                                //если все реквизиты протокола сборки пусты и система вычислила самые популярные значения - вызываемая реализация должна выполнить сохранение протокола сборки
                                                this.PrepareHeadOfAssemblyProtocol(allRequisitesAreEmpty, deviceModeView, assemblyJob, export, deviceTypeID, constructive, itav, modification, deviceClass, dUdt, trr, tq, tgt, qrr, climatic, omnity);
                                            }
                                          );
            }
        }

        private int AssemblyProtocolIDFromFilter()
        {
            //считывает значение установленного фильтра по полю обозначения протокола сборки и если
            //обозначение протокола сборки было успешно считано - вычисляет по нему идентификатор протокола сборки и возвращает его в качестве результата

            int assemblyProtocolID = -1;

            //режим просмотра протокола сборки включён - значит на данный момент в системе активен один фильтр по протоколу сборки
            //считываем из этого фильтра обозначение протокола сборки и вычисляем по нему его идентификатор
            IEnumerable<FilterDescription> filters = this.FActiveFilters.Where(f => f.FieldName == Common.Constants.AssemblyProtocolDescr);

            if (filters.Count() != 0)
            {
                FilterDescription filter = filters.Single();
                IEnumerable<string> values = filter.Values.Values();

                if ((values.Count() == 1) && int.TryParse(values.Single().ToString(), out int descr))
                    assemblyProtocolID = DbRoutines.AssemblyProtocolIDByDescr(descr);
            }

            return assemblyProtocolID;
        }

        private void ContextMenuSelectedToInitState_Click(object sender, RoutedEventArgs e)
        {
            //переводим выбранные строки имеющие один и тот же протокол сборки (должен быть включен режим просмотра протокола сборки) в состояние 'НЗП'
            if (this.AssemblyProtocolMode)
            {
                if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
                {
                    int assemblyProtocolID = this.AssemblyProtocolIDFromFilter();

                    if (assemblyProtocolID != -1)
                    {
                        SCME.Common.Routines.ShowProcessWaitVisualizer(this, this.ProcessWaitVisualizerHWnd);
                        int processedCount = 0;

                        try
                        {
                            processedCount = DbRoutines.SetInitStateForSelectedDevices(assemblyProtocolID);

                            //проверяем наличие ссылок на протокол сборки assemblyProtocolID в DEVICES.ASSEMBLYPROTOCOLID
                            //если ссылки на протокол сборки assemblyProtocolID отсутствуют - удаляем сам протокол сборки
                            if (!DbRoutines.IsDeviсesUseAssemblyProtocolID(assemblyProtocolID))
                                DbRoutines.DeleteFromAssemblyProtocols(assemblyProtocolID);
                        }
                        finally
                        {
                            SCME.Common.Routines.HideProcessWaitVisualizer(this.ProcessWaitVisualizerHWnd);
                        }

                        if (processedCount == 0)
                        {
                            MessageBox.Show(string.Format(string.Concat(Properties.Resources.OperationFailed, ". ", Properties.Resources.ProcessedRecordCount, "."), processedCount), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        //применяем имеющиеся фильтры чтобы увидеть результат
                        this.ApplyFilters();
                    }
                }
                else
                    MessageBox.Show(Properties.Resources.NoPermissions, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #region Filters
        private string DataTypeByColumn(DataGridColumn column, out string sourceFieldName)
        {
            //определение типа данных, отображаемых в столбце column
            //судить о типе всех данных по порции данных нельзя - в порции данных может не быть ни одного значения, тип данных не вычислим - поэтому никак не используем данные для вычисления их типа
            //но нам всегда известно что за данные могут быть в данном столбце, т.к. они всегда грузятся только из базы данных, а типы данных в ней чётко заданы
            string result = null;

            Common.Routines.XMLValues subject = Common.Routines.XMLValues.UnAssigned;
            sourceFieldName = null;

            if (column != null)
            {
                //определяем с чем мы имеем дело: с conditions или parameters
                sourceFieldName = column.SortMemberPath;
                subject = Common.Routines.SubjectByPath(sourceFieldName);

                switch (subject)
                {
                    case Common.Routines.XMLValues.UnAssigned:
                        switch (sourceFieldName)
                        {
                            case Common.Constants.Ts:
                                result = "System.DateOnly";
                                break;

                            case Common.Constants.Choice:
                                result = typeof(bool).FullName;
                                break;

                            case Common.Constants.AssemblyStatusID:
                            case Common.Constants.AssemblyProtocolDescr:
                            case Common.Constants.AverageCurrent:
                            case Common.Constants.DeviceClass:
                                result = typeof(int).FullName;
                                break;

                            default:
                                result = typeof(string).FullName;
                                break;
                        }
                        break;

                    case SCME.Common.Routines.XMLValues.Conditions:
                        result = typeof(string).FullName;
                        break;

                    case SCME.Common.Routines.XMLValues.Parameters:
                        result = typeof(double).FullName;
                        break;

                    default:
                        result = typeof(string).FullName;
                        break;
                }
            }

            return result;
        }

        /*
        private bool IsAssemblyProtocolModeOn()
        {
            //отвечает на вопрос о включенном режиме просмотра содержимого протокола испытаний
            //true - включен режим просмотра содержимого протокола испытаний
            //false - режим просмотра содержимого протокола испытаний выключен
            return (this.FActiveFilters.FirstOrDefault(f => f.FieldName == Common.Constants.AssemblyProtocolDescr) != null);
        }
        */

        private void ApplyFilters()
        {
            //вызвать данную реализацию может только dbViewer
            //применение установленных пользователем фильтров к отображаемым данным

            //показываем пользователю: надо ждать результат фильтрации
            SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

            //фильтрация предполагает поиск нужных пользователю данных
            //чтобы фильтрованные данные всегда были актуальными уничтожаем кеш, в этом случае кеш будет построен на последних имеющихся данных - данные в кеше будут актуализированы
            DbRoutines.CacheFree();

            //применяем фильтры
            this.RefreshShowingData();
        }

        private void SetFilter(Point position, System.Windows.Controls.Primitives.DataGridColumnHeader columnHeader)
        {
            if (this.dgDevices.ItemsSource != null)
            {
                string filterType = this.DataTypeByColumn(columnHeader.Column, out string sourceFieldName);

                CustomControls.FilterDescription filter = new CustomControls.FilterDescription(this.FActiveFilters, sourceFieldName) { Type = filterType, TittlefieldName = columnHeader.Content.ToString(), Comparison = "=", Value = this.dgDevices.ValueFromSelectedRow(sourceFieldName) };
                //this.LoadListOfValues(filter.ListOfValues, bindPath);

                this.FActiveFilters.Add(filter);

                FiltersInput fmFiltersInput = new FiltersInput(this.FActiveFilters, this);

                if (fmFiltersInput.Demonstrate(position) == true)
                    this.ApplyFilters();
            }
        }

        private string FilterFieldNameByComboBox(ComboBox cmb, out string tittlefieldName, out string filterType, out string comparison)
        {
            //возвращает как результат имя условия/параметра для выполнения фильтрации, соответствующее принятому cmb (cmbDUDt, cmbTrr, cmbTq, cmbTgt, cmbQrr)
            //пример: TMDvdtConditions©DVDT_VoltageRate, где
            //        TM - горячий;
            //        Dvdt - наименование теста;
            //        Conditions - имеем дело с условием теста;
            //        DVDT_VoltageRate - наименование условия
            //в out tittlefieldName возвращает имя фильтра для демонстрации пользователю
            //в out filterType возвращает тип фильтра для выполнения фильтрации

            if (cmb != null)
            {
                switch (cmb.Name)
                {
                    case "cmbDUDt":
                        //всегда горячий
                        //условие
                        tittlefieldName = string.Concat("TM/Dvdt", Constants.StringDelimeter, Common.Constants.DUdt);
                        filterType = typeof(string).FullName;
                        comparison = "=";

                        return "TMDvdtConditions©DVDT_VoltageRate";

                    case "cmbTrr":
                        //всегда горячий
                        //параметр
                        tittlefieldName = string.Concat("TM/QrrTq", Constants.StringDelimeter, "Qrr");
                        filterType = typeof(double).FullName;
                        comparison = "<=";

                        return "TMQrrTqParameters©TRR";

                    case "cmbTq":
                        //всегда горячий
                        //параметр
                        tittlefieldName = string.Concat("TM/QrrTq", Constants.StringDelimeter, "Tq");
                        filterType = typeof(double).FullName;
                        comparison = "<=";

                        return "TMQrrTqParameters©TQ";

                    case "cmbTgt":
                        //всегда холодный
                        //параметр
                        //на момент разработки не измеряется средствами КИПП СПП
                        tittlefieldName = string.Concat("RT/TOU", Constants.StringDelimeter, "Tgt");
                        filterType = typeof(double).FullName;
                        comparison = "<=";

                        return "RTTOUParameters©TOU_TGT";

                    case "cmbQrr":
                        //всегда горячий
                        //параметр
                        tittlefieldName = string.Concat("TM/QrrTq", Constants.StringDelimeter, "Qrr");
                        filterType = typeof(double).FullName;
                        comparison = "<=";

                        return "TMQrrTqParameters©QRR";

                    default:
                        tittlefieldName = null;
                        filterType = null;
                        comparison = null;

                        return null;
                }
            }

            tittlefieldName = null;
            filterType = null;
            comparison = null;

            return null;
        }

        private void OnChangedListOfFiltersHandler(IEnumerable<CustomControls.FilterDescription> actualFilters)
        {
            //не существует единственного фильтра по полю 'ASSEMBLYPROTOCOLDESCR' с типом сравнения '='
            if (this.AssemblyProtocolMode)
            {
                //данная реализация будет вызвана сразу после изменения списка действующих фильтров
                IEnumerable<CustomControls.FilterDescription> filtersByAssemblyProtocolDescr = actualFilters.Where(f => f.FieldName == Common.Constants.AssemblyProtocolDescr);
                int countOfFiltersByAssemblyProtocolDescr = filtersByAssemblyProtocolDescr.Count();

                IEnumerable<CustomControls.FilterDescription> filtersByAssemblyStatusDescr = actualFilters.Where(f => f.FieldName == Common.Constants.AssemblyStatusDescr);
                int countOfFiltersByAssemblyStatusDescr = filtersByAssemblyStatusDescr.Count();

                string assemblyDescr = DbRoutines.AssemblyDescrByAssemblyStatusID(1);

                //проверяем два возможных варианта описания фильтра:
                //один или несколько фильтров по протоколу сборки;
                //один фильтр с несколькими значениями протокола сборки
                bool needResetAssemblyProtocolMode = (
                                                      (actualFilters.Count() != 2) ||
                                                      (countOfFiltersByAssemblyProtocolDescr != 1) || (countOfFiltersByAssemblyStatusDescr != 1) ||
                                                      (filtersByAssemblyProtocolDescr.Single().Comparison != "=") || (filtersByAssemblyProtocolDescr.Single().Values.Count() != 1) || (filtersByAssemblyStatusDescr.Single().Comparison != "=") || (filtersByAssemblyStatusDescr.Single().Values.Count() != 1) || (filtersByAssemblyStatusDescr.Single().Values[0].Value.ToString() != assemblyDescr)
                                                     );


                if (needResetAssemblyProtocolMode)
                {
                    //выключаем режим просмотра протокола сборки
                    this.AssemblyProtocolMode = false;

                    foreach (DataGridColumn column in this.dgDevices.Columns)
                    {
                        if (column.Visibility == Visibility.Collapsed)
                            column.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private MenuItem ContextMenuItemByName(string name)
        {
            return this.dgDevices.ContextMenu.Items.OfType<MenuItem>().FirstOrDefault(i => i.Name == name);
        }

        private void DgDevices_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //вернуть выбранные строки в состояние 'НЗП' можно только в режиме просмотра протокола сборки
            MenuItem item = this.ContextMenuItemByName("ctxmSelectedToInitState");

            if (item != null)
                item.Visibility = this.AssemblyProtocolMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ReBuildCPColumnsForAssemblyProtocolMode()
        {
            //перестройка столбцов conditions/parameters в dgDevices для случая включенного режима AssemblyProtocolMode и выбранного типа в cmbDeviceType
            //перед удалением списка столбцов в dgDevices скопируем содержимое this.FDataGridColumnSourceData т.к. оно при удалении столбцов бедет очищено
            List<DataGridColumnSourceData> copyDataGridColumnSourceData = new List<DataGridColumnSourceData>();

            foreach (DataGridColumnSourceData columnSourceData in this.FDataGridColumnSourceData)
            {
                DataGridColumnSourceData copy = new DataGridColumnSourceData(columnSourceData.Header, columnSourceData.BindPath);
                copyDataGridColumnSourceData.Add(copy);
            }

            //удаляем столбцы dgDevices, которые отображают значения conditions/parameters, при этом будет очищено содержимое this.FDataGridColumnSourceData
            this.RemoveXMLDataGridColumns();

            foreach (DataGridColumnSourceData columnSourceData in copyDataGridColumnSourceData)
            {
                this.BuildColumnInDataGrid(columnSourceData.Header, columnSourceData.BindPath);
            }
        }

        private void MnuListOfAssemblyProtocolsClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                AssemblyProtocols assemblyProtocols = new AssemblyProtocols(this.ProcessWaitVisualizerHWnd)
                {
                    Owner = this
                };

                //даём пользователю выбрать протокол сборки из списка
                if (assemblyProtocols.ShowModal(out int assemblyProtocolDescr))
                    this.AssemblyProtocolModeOn(assemblyProtocolDescr);
            }
        }

        private void MnuSetAssemblyProtocolModeOnClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                //для перехода в режим просмотра протокола сборки должны быть включены два фильтра: по полю "ASSEMBLYPROTOCOLDESCR" и по полю "ASSEMBLYSTATUSDESCR"
                if (this.FActiveFilters.Count() != 2)
                {
                    MessageBox.Show(Properties.Resources.FiltersCountIsNotOne, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                //если активны фильтры по протоколу сборки (в единственном числе) и по обозначению статуса сборки (в единственном числе) - разрешаем пользователю включить режим просмотра протокола сборки
                IEnumerable<CustomControls.FilterDescription> filters = this.FActiveFilters.Where(f => f.FieldName == Common.Constants.AssemblyProtocolDescr);

                if (filters == null)
                {
                    MessageBox.Show(string.Format(Properties.Resources.FilterIsNotOn, Properties.Resources.ProtocolDescr), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                else
                {
                    if (filters.Count() != 1)
                    {
                        MessageBox.Show(string.Format(Properties.Resources.FilterIsNotSingle, Properties.Resources.ProtocolDescr), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    //раз мы здесь - значит фильтр по протоколу сборки в единственном числе
                    CustomControls.FilterDescription filter = filters.Single();

                    //проверим, что в данном фильтре тип сравнения есть '='
                    if (filter.Comparison != "=")
                    {
                        MessageBox.Show(string.Concat(Properties.Resources.ProtocolDescr, ". ", Properties.Resources.FilterComparisonTypeIsNotEqual), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    //проверим, что в данном фильтре установлено не более одного значения поля
                    if (filter.Values.Count != 1)
                    {
                        MessageBox.Show(string.Format(Properties.Resources.FilterValuesIsNotSingle, Properties.Resources.ProtocolDescr), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    //проверяем наличие фильтра по обозначению статуса протокола сборки
                    filters = this.FActiveFilters.Where(f => f.FieldName == Common.Constants.AssemblyStatusDescr);

                    if (filters == null)
                    {
                        MessageBox.Show(string.Format(Properties.Resources.FilterIsNotOn, Properties.Resources.AssemblyStatusDescr), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    else
                    {
                        if (filters.Count() != 1)
                        {
                            MessageBox.Show(string.Format(Properties.Resources.FilterIsNotSingle, Properties.Resources.AssemblyStatusDescr), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                        //раз мы здесь - значит фильтр по обозначению статуса сборки в единственном числе
                        filter = filters.Single();

                        //проверим, что в данном фильтре тип сравнения есть '='
                        if (filter.Comparison != "=")
                        {
                            MessageBox.Show(string.Concat(Properties.Resources.AssemblyStatusDescr, ". ", Properties.Resources.FilterComparisonTypeIsNotEqual), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                        //проверим, что в данном фильтре установлено не более одного значения поля
                        if (filter.Values.Count != 1)
                        {
                            MessageBox.Show(string.Format(Properties.Resources.FilterValuesIsNotSingle, Properties.Resources.AssemblyStatusDescr), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                        //проверим, что в данном фильтре установлено значение 'Сборка'
                        string assemblyDescr = DbRoutines.AssemblyDescrByAssemblyStatusID(1);

                        if (filter.Values[0].Value.ToString() != assemblyDescr)
                        {
                            MessageBox.Show(string.Format(Properties.Resources.FilterValueIsBad, Properties.Resources.AssemblyStatusDescr, assemblyDescr), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                        this.AssemblyProtocolMode = true;

                        //строим шапку протокола сборки
                        if (int.TryParse(this.lbRecordCount.Content.ToString(), out int cacheSize))
                            this.BuildHeadOfAssemblyProtocol(cacheSize);

                        //перестраиваем столбцы которые отображают conditions/parameters в dgDevices
                        this.ReBuildCPColumnsForAssemblyProtocolMode();
                    }
                }
            }
        }

        private string SaveAssemblyProtocol()
        {
            //возвращает:
            // null - сохранение выполнено успешно;
            // сообщение об ошибке - сохранение не выполнено
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                //всегда имеем дело с уже созданным протоколом сборки
                if (this.AssemblyProtocolMode)
                {
                    //все отображаемые записи имеют один и тот же протокол сборки
                    if (this.dgDevices.ItemsSource is Collection<DynamicObj> items)
                    {
                        //все строки в протоколе сборки имеют один и тот же протокол сборки
                        int? assemblyProtocolID = this.AssemblyProtocolID(items.ElementAt(0));

                        //читаем из первой отображаемой записи обозначение протокола сборки
                        if ((assemblyProtocolID != null) && (assemblyProtocolID != -1))
                        {
                            string assemblyJob = null;
                            if (!string.IsNullOrEmpty(tbAssemblyJob.Text))
                            {
                                //проверяем корректность ввода ПЗ сборки
                                assemblyJob = tbAssemblyJob.Text.Trim();

                                //проверяем соответствие маске ввода
                                if (!SCME.Common.Routines.CheckAssemblyJobByMask(assemblyJob, out string mask))
                                    return string.Format(Properties.Resources.WrongValueByMask, Properties.Resources.AssemblyJob, mask);

                                //проверяем наличие ПЗ сборки в SL
                                //assemblyJob может быть написано пользователем с суффиксом и без суффикса
                                //если assemblyJob содержит два символа '-' - значит суффикс в assemblyJob есть
                                string job = assemblyJob;
                                short suffix = 0;

                                if (assemblyJob.Count(f => f == '-') == 2)
                                {
                                    //суффикс в наличии
                                    string sSuffix = Routines.EndingNumber(assemblyJob);
                                    short.TryParse(sSuffix, out suffix);

                                    //вырезаем обозначение суффикса из assemblyJob - суффикс всегда начинается с разделителя и далее 4 символа итого 5 символов
                                    job = assemblyJob.Substring(0, assemblyJob.Length - 5);
                                }

                                if (!DbRoutines.JobExist(job, suffix))
                                    return string.Format(Properties.Resources.NotFoundInSL, Properties.Resources.AssemblyJob);
                            }

                            bool? export = cbExport.IsChecked;

                            //если пользователь не выбрал тип изделия - не даём ему сохранить такие данные ибо при значении AssemblyProtocols.DeviceTypeID=Null в последствии не получится выполнить построение шапки протокола сборки
                            int? deviceTypeID = null;
                            if (cmbDeviceType.SelectedItem is string[] selectedItem)
                            {
                                if (int.TryParse(selectedItem[0], out int devTypeID))
                                    deviceTypeID = devTypeID;
                            }
                            else
                                return string.Concat(Properties.Resources.DeviceType, ". ", Properties.Resources.ValueIsNotGood);

                            int? itav = null;
                            if (!string.IsNullOrEmpty(tbAverageCurrent.Text))
                            {
                                if (int.TryParse(tbAverageCurrent.Text, out int iItav))
                                    itav = iItav;
                            }

                            string modification = null;
                            if (cmbModification.SelectedItem is string selModification)
                                modification = selModification;

                            int? deviceClass = null;
                            if (!string.IsNullOrEmpty(tbDeviceClass.Text))
                            {
                                if (int.TryParse(tbDeviceClass.Text, out int iDeviceClass))
                                    deviceClass = iDeviceClass;
                            }

                            int? dUdt = null;
                            if (cmbDUDt.SelectedItem is GroupOfValues seldUdt)
                            {
                                if (int.TryParse(seldUdt.Value, out int idUdt))
                                    dUdt = idUdt;
                            }

                            double? trr = null;
                            if (cmbTrr.SelectedItem is GroupOfValues selTrr)
                            {
                                if (double.TryParse(selTrr.Value, out double dTrr))
                                    trr = dTrr;
                            }

                            string tq = null;
                            if (cmbTq.SelectedItem is GroupOfValues selTq)
                                tq = selTq.Value.ToString();

                            double? tgt = null;
                            if (cmbTgt.SelectedItem is GroupOfValues selTgt)
                            {
                                if (double.TryParse(selTgt.Value, out double dTgt))
                                    tgt = dTgt;
                            }

                            int? qrr = null;
                            if (cmbQrr.SelectedItem is QrrGroupOfValues selQrr)
                            {
                                if (int.TryParse(selQrr.Value, out int iQrr))
                                    qrr = iQrr;
                            }

                            string climatic = null;
                            if (cmbClimatic.SelectedItem is ComboBoxItem selClimatic)
                                climatic = selClimatic.Content.ToString();

                            int? omnity = null;
                            if (int.TryParse(tbOmnity.Text, out int iOmnity))
                                omnity = iOmnity;

                            DbRoutines.UpdateAssemblyProtocols((int)assemblyProtocolID, cbDeviceModeView.IsChecked ?? false, assemblyJob, export, deviceTypeID, itav, modification, tbConstructive.Text, deviceClass, dUdt, trr, tq, tgt, qrr, climatic, omnity);

                            //раз мы сюда добрались - сохранение выполнено успешно                           
                            return null;
                        }
                    }
                }
            }
            else
                return Properties.Resources.NoPermissions;

            //если мы здесь - сохранение не выполнено, говорим об этом пользователю
            return Properties.Resources.SaveFailed;
        }

        private void MnuSaveAssemblyProtocolClick(object sender, RoutedEventArgs e)
        {
            string error = this.SaveAssemblyProtocol();

            if (error == null)
            {
                MessageBox.Show(Properties.Resources.SaveWasSuccessful, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
                MessageBox.Show(error, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CbDeviceModeView_Click(object sender, RoutedEventArgs e)
        {
            this.BuildDevice();
        }

        private void RefreshData()
        {
            //данное действие возможно будет использовать фильтрацию данных
            SCME.Common.Routines.ShowProcessWaitVisualizerSortingFiltering(this, this.ProcessWaitVisualizerHWnd);

            //уничтожаем кеш для его принудительной перестройки на основе последних данных
            DbRoutines.CacheFree();

            this.RefreshShowingData();
        }

        private void MnuRefreshClick(object sender, RoutedEventArgs e)
        {
            this.RefreshData();
        }

        #endregion

        private void mnuWorkWithManualParamsOfAssemblyProtocolClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanWorkWithAssemblyProtocol(this.PermissionsLo))
            {
                object objProfID = this.dgDevices.ValueFromSelectedRow("PROF_ID");

                if (int.TryParse(objProfID.ToString(), out int profID))
                {
                    ManualInputAssemblyProtocolParam manualInputAssemblyProtocolParam = new ManualInputAssemblyProtocolParam(profID)
                    {
                        Owner = this
                    };

                    object obj = this.dgDevices.ValueFromSelectedRow(Common.Constants.AssemblyProtocolDescr);

                    if (obj != null)
                    {
                        if (int.TryParse(obj.ToString(), out int assemblyProtocolDescr))
                        {
                            manualInputAssemblyProtocolParam.ShowModal(assemblyProtocolDescr);
                        }
                    }
                }
            }
        }

        private void CbExport_Click(object sender, RoutedEventArgs e)
        {
            //перестраиваем обозначение итогового изделия-результата
            this.BuildDevice();
        }

        private void TbOnlyNumeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Common.Routines.TextBoxOnlyNumeric_PreviewTextInput(sender, e);
        }

        private void TbOnlyNumericPaste(object sender, DataObjectPastingEventArgs e)
        {
            Common.Routines.TextBoxOnlyNumericPaste(sender, e);
        }

        private void TbDisableSpace_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Common.Routines.TextBoxDisableSpace_PreviewKeyDown(sender, e);
        }

        private bool TextAsConstructiveValid(string text)
        {
            //символ "X" в обозначении конструктива можно использовать только один раз
            //если символ "X" уже использован, то ничего другого в обозначении конструктива быть не может
            //если значение конструктива записано цифрами - символ "X" уже не может быть использован

            string resultConstructive = this.tbConstructive.Text.Remove(this.tbConstructive.SelectionStart, this.tbConstructive.SelectionLength);
            resultConstructive = string.Concat(resultConstructive, text);
            bool resultConstructiveIsNumeric = int.TryParse(resultConstructive, out int temp);

            return !((resultConstructive == "X") || resultConstructiveIsNumeric || string.IsNullOrEmpty(resultConstructive));
        }

        private void TbConstructive_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = this.TextAsConstructiveValid(e.Text);
        }

        private void TbConstructivePaste(object sender, DataObjectPastingEventArgs e)
        {
            //запрещаем вставку из буфера обмена не цифр
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));

                if (this.TextAsConstructiveValid(text))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private void MnuReportBuildClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(this.lbRecordCount.Content.ToString(), out int cacheSize))
            {
                if (cacheSize > 0)
                {
                    SCME.Common.Routines.ShowProcessWaitVisualizer(this, this.ProcessWaitVisualizerHWnd);

                    try
                    {
                        //не все пользователи хорошо видят и некоторые устанавливают в операционной системе увеличенный масштаб
                        //узнаём его для передачи в реализацию построения отчёта
                        double systemScale = Routines.SystemScale(this);

                        switch (this.AssemblyProtocolMode)
                        {
                            //требуется сгенерировать в Excel отчёт по протоколу сборки
                            case true:
                                //считываем выбранные пользователем: средний ток, тип, конструктив
                                //если хотя-бы одно из значений этих параметров пользователь не выбрал - ругаемся и отказываемся формировать отчёт ибо без них нельзя считать из базы данных описание норм на параметры
                                if (!int.TryParse(tbAverageCurrent.Text, out int itav))
                                {
                                    MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.AverageCurrent, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    return;
                                }

                                if (this.cmbDeviceType.SelectedItem == null)
                                {
                                    MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.DeviceType, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    return;
                                }

                                string[] selectedItem = cmbDeviceType.SelectedItem as string[];
                                if (!int.TryParse(selectedItem[0], out int deviceTypeID))
                                    throw new Exception(string.Format("The '{0}' is not an integer value", selectedItem[0]));

                                string constructive = tbConstructive.Text;
                                if (string.IsNullOrEmpty(constructive))
                                {
                                    MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.Constructive, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    return;
                                }

                                //значение модификации пользователь может ввести только если поле ввода видно
                                string modification = null;

                                if (cmbModification.Visibility == Visibility.Visible)
                                {
                                    modification = cmbModification.Text;
                                    if (string.IsNullOrEmpty(modification))
                                    {
                                        MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.Modification, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                        return;
                                    }
                                }

                                string assemblyJob = null;
                                if (!string.IsNullOrEmpty(this.tbAssemblyJob.Text))
                                    assemblyJob = this.tbAssemblyJob.Text.Trim();

                                //считываем RU обозначение типа изделия
                                string deviceTypeRU = (this.cmbDeviceType.SelectedItem == null) ? null : ((string[])this.cmbDeviceType.SelectedItem)[1];

                                //считываем значение Tq
                                string tq = null;
                                if (this.cmbTq.SelectedItem is GroupOfValues groupOfValuesTq)
                                    tq = groupOfValuesTq.TrueValue;

                                //считываем значение Trr
                                string trr = null;
                                if (this.cmbTrr.SelectedItem is GroupOfValues groupOfValuesTrr)
                                    trr = groupOfValuesTrr.TrueValue;

                                //считываем значение Qrr
                                string qrr = null;
                                if (this.cmbQrr.SelectedItem is QrrGroupOfValues groupOfValuesQrr)
                                    qrr = groupOfValuesQrr.Value;

                                //считываем значение dUdt
                                string dUdt = null;
                                if (this.cmbDUDt.SelectedItem is GroupOfValues groupOfValuesdUdt)
                                    dUdt = groupOfValuesdUdt.TrueValue;

                                //считываем значение Tgt
                                string tgt = null;
                                if (this.cmbTgt.SelectedItem is GroupOfValues groupOfValuesTgt)
                                    tgt = groupOfValuesTgt.TrueValue;

                                //идентификатор отображаемого протокола сборки нужен для фильтрации исходных данных
                                //в этом случае формирования отчёта он нам не нужен - в кеше и так могут быть только такие записи, которые уже отфильтрованы по номеру протокола сборки
                                //параметр assemblyProtocolID в реализации AssemblyProtocolReport.Build нужен только для случая вызова из списка отображаемых протоколов сборки - там нет фильтрации по протоколу сборки
                                AssemblyProtocolReport.Build(-1, this.SaveAssemblyProtocol, systemScale, cacheSize, assemblyJob, this.lbDevice.Content.ToString(), deviceTypeRU, this.tbOmnity.Text, tq, trr, qrr, dUdt, tgt, itav, deviceTypeID, constructive, modification, this.tbDeviceClass.Text);
                                break;

                            default:
                                //требуется сгенерировать в Excel отчёт по отображаемым данным
                                this.BuildReportInExcel(this.GetReportData(cacheSize), systemScale);
                                break;
                        }
                    }
                    finally
                    {
                        SCME.Common.Routines.HideProcessWaitVisualizer(this.ProcessWaitVisualizerHWnd);
                    }
                }
                else
                    MessageBox.Show(Properties.Resources.ReportCannotBeGenerated, Properties.Resources.NoData, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MainForm_Closing(object sender, CancelEventArgs e)
        {
            SCME.Common.Routines.KillProcessWaitVisualizer(this.ProcessWaitVisualizerHWnd);
        }

        private void TextBoxDeviceCommentsTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tbDeviceComments)
                this.FTextBoxDeviceCommentsTextChanged = true;
        }

        private int[] DevIDsByTextBoxDeviceComments(TextBox tbDeviceComments)
        {
            int[] devIDArray = null;

            if ((tbDeviceComments != null) && (tbDeviceComments.DataContext is SCME.CustomControls.DynamicObj dynamicObj))
            {
                dynamicObj.GetMember(Common.Constants.DevID, out object devIDs);
                devIDArray = ObjectToArrayOfInt(devIDs);
            }

            return devIDArray;
        }

        private void SaveComments(int[] devIDArray, string comments)
        {
            try
            {
                //сохраняем в базу данных введённый пользователем комментарий для всех элементов группы
                if (devIDArray != null)
                {
                    foreach (int devID in devIDArray)
                    {
                        DbRoutines.SaveToDeviceComment(devID, this.FUserID, comments);
                    }
                }
            }
            finally
            {
                this.FTextBoxDeviceCommentsTextChanged = false;
            }
        }

        private void TextBoxDeviceCommentsLostFocus(object sender, RoutedEventArgs e)
        {
            //выполняем сохранение введённого пользователем комментария
            if (sender is TextBox tbDeviceComments)
            {
                if (this.FTextBoxDeviceCommentsTextChanged && !tbDeviceComments.IsReadOnly)
                {
                    int[] devIDArray = this.DevIDsByTextBoxDeviceComments(tbDeviceComments);
                    this.SaveComments(devIDArray, tbDeviceComments.Text);
                }
            }
        }

        private void ContextMenuDeviceCommentsEdit_Click(object sender, RoutedEventArgs e)
        {
            //вызываем редактор комментариев

            IInputElement parent = (IInputElement)LogicalTreeHelper.GetParent((DependencyObject)sender);

            if ((parent is ContextMenu cm) && (cm.PlacementTarget is TextBox tbDeviceComments))
            {
                //если пользователь отредактировал комментарий средствами формы this и не выходя из поля комментария откроет его контекстное меню - событие потери фокуса полем комментария не отработает, поэтому выполним сохранение комментария, чтобы форма WorkWithComments прочитала его из базы данных в актуальном состоянии
                int[] devIDArray = this.DevIDsByTextBoxDeviceComments(tbDeviceComments);

                if (this.FTextBoxDeviceCommentsTextChanged)
                    this.SaveComments(devIDArray, tbDeviceComments.Text);

                this.WorkWithCommentsShowModalForSelectedRow(devIDArray);
            }
        }

        private void DataGridCellEditByTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((this.dgDevices.Items.Count != 0) && (sender is DataGridCell cell) && (e.OriginalSource is UIElement uiElement))
            {
                DataGridRow currentRow = DataGridRow.GetRowContainingElement(cell);

                if (currentRow != null)
                {
                    int currentRowIndex = currentRow.GetIndex();

                    switch (e.Key)
                    {
                        //технологи хотят двигаться вниз по списку по нажатию на клавишу Enter
                        case Key.Down:
                        case Key.Enter:
                            int nextRowIndex = currentRowIndex + 1;
                            DataGridCellInfo nextCell = new DataGridCellInfo(this.dgDevices.Items[nextRowIndex], this.dgDevices.Columns[cell.Column.DisplayIndex]);

                            this.dgDevices.SelectedIndex = nextRowIndex;
                            this.dgDevices.CurrentCell = nextCell;
                            this.dgDevices.SelectedCells.Clear();
                            this.dgDevices.SelectedCells.Add(nextCell);
                            uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));

                            e.Handled = true;
                            break;

                        case Key.Up:
                            if (currentRowIndex > 0)
                            {
                                int previousRowIndex = currentRowIndex - 1;
                                DataGridCellInfo previousCell = new DataGridCellInfo(this.dgDevices.Items[previousRowIndex], this.dgDevices.Columns[cell.Column.DisplayIndex]);

                                this.dgDevices.SelectedIndex = previousRowIndex;
                                this.dgDevices.CurrentCell = previousCell;
                                this.dgDevices.SelectedCells.Clear();
                                this.dgDevices.SelectedCells.Add(previousCell);
                                uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                            }

                            e.Handled = true;
                            break;
                    }
                }
            }
        }

        private void MnuAboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Просмотр результатов с КИП СПП\nВерсия проекта: 1.12");
        }

        /*
                private bool IsMouseOverCheckBox(CheckBox cb)
                {
                    //стандартная реализация cb.IsMouseOver не работает, поэтому делаем свою
                    //проверяем, что курсор мыши над принятым cb
                    Point mousePosition = Mouse.GetPosition(cb);

                    return (mousePosition.X >= 0) && (mousePosition.X <= cb.ActualWidth) && (mousePosition.Y >= 0) && (mousePosition.Y <= cb.ActualHeight);
                }

                public void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
                {            
                    <EventSetter Event="PreviewMouseLeftButtonDown" Handler="DataGridCell_PreviewMouseLeftButtonDown"/>
                    if (sender is DataGridCell cell)
                    {
                        if (cell.Content is CheckBox cb)
                        {
                            if (this.IsMouseOverCheckBox(cb))
                            {
                                //чтобы вычислялось количество ниже стоящих записей
                                cell.Focus();

                                //чтобы отрабатывал xaml триггер на IsSelected
                                //cell.IsSelected = true;
                            }

                            //e.Handled = true;
                        }
                    }  
                }
        */

    }

    public class DataGridColumnSourceData
    {
        //исходные данные для построения столбца в DataGrid
        public string Header { get; }
        public string BindPath { get; }

        public DataGridColumnSourceData(string header, string bindPath)
        {
            this.Header = header;
            this.BindPath = bindPath;
        }
    }

    public class ListOfDeviceType
    {
        public List<string[]> GetDeviceTypeList()
        {
            return new Routines.DeviceTypes();
        }
    }

    public class ListOfModificationGroup
    {
        public List<string> GetModificationList()
        {
            return new Routines.ModificationGroups();
        }
    }

    public class GroupOfValues
    {
        //вариант конструктора для Tq
        public GroupOfValues(string value, string trueValue, string group, string num)
        {
            this.Value = value;
            this.TrueValue = trueValue;
            this.Group = group;
            this.Num = num;
        }

        public GroupOfValues(string value, string group, string num)
        {
            this.Value = value;
            this.TrueValue = value;
            this.Group = group;
            this.Num = num;
        }

        public string Value { get; }
        public string TrueValue { get; }
        public string Group { get; }
        public string Num { get; }
    }

    public class ListOfDUDtGroup
    {
        public List<GroupOfValues> GetDUDtList()
        {
            return Routines.DUDtGroups.Select(x => new GroupOfValues(x.Key.ToString(), x.Value.Descr, x.Value.Num)).ToList();
        }
    }

    public class ListOfTrrGroup
    {
        public List<GroupOfValues> GetTrrList()
        {
            return Routines.TrrGroups.Select(x => new GroupOfValues(x.Key.ToString(), x.Value.Descr, x.Value.Num)).ToList();
        }
    }

    public class ListOfTqGroup
    {
        public List<GroupOfValues> GetTqList()
        {
            return Routines.TqGroups.Select(x => new GroupOfValues(x.Key, x.Value.TrueValue, x.Value.Descr, x.Value.Num)).ToList();
        }
    }

    public class ListOfTgtGroup
    {
        public List<GroupOfValues> GetTgtList()
        {
            return Routines.TgtOnGroups.Select(x => new GroupOfValues(x.Key.ToString(), x.Value.Descr, x.Value.Num)).ToList();
        }
    }

    public class QrrGroupOfValues
    {
        public QrrGroupOfValues(string value, string deviceTypeRu, string constructive, string itav)
        {
            this.Value = value;
            this.DeviceTypeRu = deviceTypeRu;
            this.Constructive = constructive;
            this.Itav = itav;
        }

        public string Value { get; }
        public string DeviceTypeRu { get; }
        public string Constructive { get; }
        public string Itav { get; }

        public override string ToString()
        {
            //переопределяем, чтобы после выбора из выпадаюшего списка система понимала что ей показывать в IsEditable=true comboBox поле
            return this.Value;
        }
    }

    public class ListOfQrrGroup
    {
        public List<QrrGroupOfValues> GetQrrList()
        {
            return Routines.QrrGroups.Select(x => new QrrGroupOfValues(x.Value.ToString(), x.DeviceTypeRu, x.Constructive.ToString(), x.Itav.ToString())).ToList();
        }
    }



    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum NrmStatus
    {
        [EnumMember]
        UnCheckable = 0,

        [EnumMember]
        Good = 1,

        [EnumMember]
        Defective = 2,

        [EnumMember]
        NotSetted = 3,

        [EnumMember]
        LegallyAbsent = 4
    }
}
