using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using FastReport;
using FastReport.Preview;
using FastReport.Utils;
using Microsoft.VisualBasic;
using SCME.SQLDatabaseClient.Annotations;
using SCME.SQLDatabaseClient.EntityData;
using SCME.SQLDatabaseClient.Properties;
using EntityState = System.Data.Entity.EntityState;


namespace SCME.SQLDatabaseClient
{
    /// <summary>
    /// Interaction logic for ViewDataPage.xaml
    /// </summary>
    public partial class ViewDataPage : Page, INotifyPropertyChanged
    {
        #region Fields

        private EntityData.Entities _db;

        private string _searchingLabelStatus = "Результаты поиска";
        private string _deviceLabelStatus = "Код прибора";

        private List<TableData> tableData = new List<TableData>();
        private string templateFileName = "По умолчанию.frx";
        private string fakeNodeForDelete = "FakeNodeForDelete";
        private string reportTableWitdth = "1030.05";
        private string heightTableData = "18.9";
        private string horzAlignTableData = "Center";
        private string vertAlignTableData = "Center";
        private string fontTableData = "Arial, 8.25pt";
        private string topTableData = "4";
        private string formatForValueTableData = "Number";
        private string hideZerosForValueTableData = "true";
        private string formatUseLocaleForValueTableData = "false";
        private string FormatDecimalDigitForValues = "1";
        private string formatDecimalSeparatorForValueTableData = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        private string formatGroupSeparatorForValueTableData = " ";
        private string formatNegativePatternForValueTableData = "1";
        private string vertAlignForValueTableData = "Center";
        private string HorzAlignForValueTableData = "Right";
        private string fontForValueTableData = "Arial, 9.75pt";

        private IList<ParameterAndVisibility> _visibleParams;
        private IList<VisibleParameter> _visibleParameterList = new List<VisibleParameter>();

        private GROUP _selectedGroup;
        private DeviceAndParametersWithProfile _selectedDevice;

        private int _selectionTypeIndex = -1;
        private string _groupSelectionString;
        private bool _isEditEnabled, _isReportEnabled;
        private IList<ReportTemplateInfo> _reportTemplateList;
        private int _selectedReportTemplateIndex;
        private static object ms_locker = new object();

        #endregion

        #region Bounded properties

        public string GroupSelectionString
        {
            get { return _groupSelectionString; }
            set
            {
                _groupSelectionString = value;
                OnPropertyChanged();
            }
        }

        public string GroupSelectionString2 { get; set; }

        public string DeviceSelectionString { get; set; }

        public string DeviceSelectionString2 { get; set; }

        public DateTime StartSelectionDate { get; set; } = DateTime.Now;

        public DateTime EndSelectionDate { get; set; } = DateTime.Now;

        public int SelectionTypeIndex
        {
            get { return _selectionTypeIndex; }
            set
            {
                _selectionTypeIndex = value;
                OnPropertyChanged();
            }
        }

        public string SearchingLabelStatus
        {
            get { return _searchingLabelStatus; }
            private set
            {
                _searchingLabelStatus = value;
                OnPropertyChanged();
            }
        }

        public string DeviceLabelStatus
        {
            get { return _deviceLabelStatus; }
            set
            {
                _deviceLabelStatus = value;
                OnPropertyChanged();
            }
        }

        public IList<ParameterAndVisibility> ParameterList
        {
            get { return _visibleParams; }
            set
            {
                _visibleParams = value;
                OnPropertyChanged();
            }
        }

        public IList<VisibleParameter> VisibleParameterList
        {
            get { return _visibleParameterList; }
            set
            {
                _visibleParameterList = value;
                OnPropertyChanged();
            }
        }

        public GROUP SelectedGroup
        {
            get { return _selectedGroup; }
            set
            {
                _selectedGroup = value;
                OnPropertyChanged();

                RebuildDeviceList();
            }
        }

        public DeviceAndParametersWithProfile SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;

                OnPropertyChanged();
            }
        }

        public bool IsEditEnabled
        {
            get { return _isEditEnabled; }
            set
            {
                _isEditEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsReportEnabled
        {
            get { return _isReportEnabled; }
            set
            {
                _isReportEnabled = value;
                OnPropertyChanged();
            }
        }

        public IList<ReportTemplateInfo> ReportTemplateList
        {
            get { return _reportTemplateList; }
            set
            {
                _reportTemplateList = value;
                OnPropertyChanged();
            }
        }

        public ReportTemplateInfo SelectedReportTemplate { get; set; }

        public int SelectedReportTemplateIndex
        {
            get { return _selectedReportTemplateIndex; }
            set
            {
                _selectedReportTemplateIndex = value;
                OnPropertyChanged();
            }
        }

        public string ReportDeviceType { get; set; }

        public string ReportNumber { get; set; }

        public string ReportCustomer { get; set; }

        #endregion

        #region Data retrieve methods

        private async void RebuildDeviceList()
        {
            var detailViewSource = (CollectionViewSource)Resources["DevicesView"];

            if (SelectedGroup != null)
            {
                DeviceLabelStatus = "Загрузка...";

                var parBlock = await Task.Factory.StartNew(SelectDevicesWithParams);

                detailViewSource.Source = new ObservableCollection<DeviceAndParametersWithProfile>(parBlock);

                DeviceLabelStatus = "Код прибора";
            }
            else
            {
                detailViewSource.Source = null;
            }
        }

        private async Task<IEnumerable<DeviceAndParametersWithProfile>> RebuildDeviceListAsync()
        {
            var detailViewSource = (CollectionViewSource)Resources["DevicesView"];

            if (SelectedGroup != null)
            {
                DeviceLabelStatus = "Загрузка...";

                var parBlock = await Task.Factory.StartNew(SelectDevicesWithParams);

                detailViewSource.Source = new ObservableCollection<DeviceAndParametersWithProfile>(parBlock);

                DeviceLabelStatus = "Код прибора";

                return parBlock;
            }

            return null;
        }

        private IList<DeviceAndParametersWithProfile> SelectDevicesWithParams()
        {
            lock (ms_locker)
            {
                var res = (from cgi in _selectedGroup.DEVICES
                           select new DeviceAndParametersWithProfile
                           {
                               Device = cgi,
                               Profile = (from prof in _db.PROFILES
                                          where prof.PROF_GUID == cgi.PROFILE_ID
                                          select prof).FirstOrDefault(),
                               Parameters =
                                   (from p in cgi.DEV_PARAM
                                    where
                                 (from q in VisibleParameterList
                                  select q.ParamName
                                     ).Contains(p.PARAM.PARAM_NAME)
                                    group p by p.PARAM.PARAM_NAME
                                       into ch
                                    select new ParameterValueChunk
                                    {
                                        ParameterEntity = (from prm in _db.PARAMS
                                                           where prm.PARAM_NAME == ch.Key
                                                           select prm).FirstOrDefault(),
                                        ChunkValues = new ObservableCollection<ParameterValue>(
                                     from pr in ch
                                     select new ParameterValue
                                     {
                                         ParameterValueEntity = pr,
                                         ParameterValueData = pr.VALUE.ToString("F2", CultureInfo.InvariantCulture)
                                     })
                                    }).Concat(
                                           from ch in VisibleParameterList
                                           where cgi.DEV_PARAM.All(q => q.PARAM.PARAM_NAME != ch.ParamName)
                                           select new ParameterValueChunk
                                           {
                                               ParameterEntity = (from prm in _db.PARAMS
                                                                  where prm.PARAM_NAME == ch.ParamName
                                                                  select prm).FirstOrDefault(),
                                               ChunkValues =
                                                   new ObservableCollection<ParameterValue>(new[] { new ParameterValue() }),
                                               IsFake = true
                                           }).OrderBy(p => p.ParameterEntity.PARAM_NAME).ToList()
                           }.FitInternal()).ToList();

                return res;
            }
        }

        private RestrictionsAndConditions SelectProfileConditionsAndRestrictions(Guid profileGuid)
        {
            lock (ms_locker)
            {
                var list = (from prf in _db.PROFILES
                            where prf.PROF_GUID == profileGuid
                            select new RestrictionsAndConditions
                            {
                                Restrictions = (from t in prf.PROF_PARAM
                                                where !t.PARAM.PARAM_IS_HIDE
                                                group t by t.PARAM.PARAM_NAME
                                    into gt
                                                select new RestrictionValues
                                                {
                                                    Name = gt.Key.Trim(),
                                                    Bounds = (from pgt in gt
                                                              orderby pgt.PROF_TEST_TYPE.ORD
                                                              select new Restriction
                                                              {
                                                                  MinVal = pgt.MIN_VAL,
                                                                  MaxVal = pgt.MAX_VAL
                                                              }).ToList()
                                                }).ToList(),
                                Conditions = (from t in prf.PROF_COND
                                              where !t.CONDITION.COND_IS_TECH
                                              group t by t.CONDITION.COND_NAME
                                    into gt
                                              select new ConditionValues
                                              {
                                                  Name = gt.Key.Trim(),
                                                  Values = (from pgt in gt
                                                            orderby pgt.PROF_TEST_TYPE.ORD
                                                            select pgt.VALUE).ToList()
                                              }).ToList()
                            }).FirstOrDefault();

                return list;
            }
        }

        private static string BuildEntityConnectionString()
        {
            const string providerName = "System.Data.SqlClient";
            const string metadataEF =
                @"res://*/EntityData.ModelGroups.csdl|res://*/EntityData.ModelGroups.ssdl|res://*/EntityData.ModelGroups.msl";

            var sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Settings.Default.DbPath,
                InitialCatalog = Settings.Default.DBName,
                IntegratedSecurity = Settings.Default.DBIntegratedSecurity
            };

            if (!Settings.Default.DBIntegratedSecurity)
            {
                sqlBuilder.UserID = Settings.Default.DBUser;
                sqlBuilder.Password = Settings.Default.DBPassword;
            }

            var entityBuilder = new EntityConnectionStringBuilder
            {
                Provider = providerName,
                ProviderConnectionString = sqlBuilder.ToString(),
                Metadata = metadataEF
            };

            return entityBuilder.ToString();
        }

        public async Task<bool> InitDataConnection()
        {
            _db = new EntityData.Entities(BuildEntityConnectionString());
            _db.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

            ParameterList = await
                Task.Factory.StartNew<IList<ParameterAndVisibility>>(() => (from prm in _db.PARAMS
                                                                            where !prm.PARAM_IS_HIDE
                                                                            orderby prm.PARAM_NAME ascending
                                                                            select
                                                                                new ParameterAndVisibility
                                                                                {
                                                                                    ParamName = prm.PARAM_NAME,
                                                                                    VisibleParamName = prm.PARAM_NAME_LOCAL.Trim(),
                                                                                    IsParamCheckedForVisible = false
                                                                                }).ToList());

            RebuildVisibleParameterList();

            RebuildReportTemplateList();

            return true;
        }

        private void RebuildReportTemplateList()
        {
            try
            {
                var repFolder = Settings.Default.ReportTemplateFolder;
                var files = Directory.EnumerateFiles(repFolder, "*.frx", SearchOption.AllDirectories);

                var list = (from temp in files
                            select new ReportTemplateInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(temp),
                                FullPath =
                                    Path.Combine(Path.IsPathRooted(repFolder) ? repFolder : Directory.GetCurrentDirectory(),
                                        temp)
                            }).ToList();

                list.Insert(0, new ReportTemplateInfo { Name = "Создать...", FullPath = "" });
                ReportTemplateList = list;
                SelectedReportTemplateIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка инициализации отчетов", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        public ViewDataPage()
        {
            InitializeComponent();
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void ViewDataPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            Cache.Main.WindowState = WindowState.Maximized;
            tiDataView.IsSelected = true;
        }

        private async void ButtonSearch_OnClick(object sender, RoutedEventArgs e)
        {
            if (_db.ChangeTracker.HasChanges())
            {
                ShowChangesPresentExclamation();
                return;
            }

            SearchingLabelStatus = "Выполняется поиск...";

            var masterViewSource = (CollectionViewSource)Resources["GroupsView"];


            {
                switch (SelectionTypeIndex)
                {
                    case 0:
                        var data = await Task.Factory.StartNew<IEnumerable<GROUP>>(
                            () =>
                            {
                                lock (ms_locker)
                                    return (from grp in _db.GROUPS
                                            where grp.GROUP_NAME.ToUpper().StartsWith(GroupSelectionString.ToUpper())
                                            orderby grp.GROUP_NAME
                                            select grp).ToList();
                            });

                        masterViewSource.Source = data;
                        break;
                    case 1:
                        data = await Task.Factory.StartNew<IEnumerable<GROUP>>(() =>
                        {
                            lock (ms_locker)
                                return (from grp in _db.GROUPS
                                        where grp.GROUP_NAME.ToUpper().Contains(GroupSelectionString2.ToUpper())
                                        orderby grp.GROUP_NAME
                                        select grp).ToList();
                        });

                        masterViewSource.Source = data;
                        break;
                    case 2:
                        {
                            data = await Task.Factory.StartNew<IEnumerable<GROUP>>(() =>
                            {
                                lock (ms_locker)
                                    return (from grp in _db.GROUPS
                                            where grp.DEVICES.Any(d => d.TS <= EndSelectionDate && d.TS >= StartSelectionDate)
                                            orderby grp.GROUP_NAME
                                            select grp).ToList();
                            });

                            masterViewSource.Source = data;
                        }
                        break;
                    case 3:
                        {
                            data = await Task.Factory.StartNew<IEnumerable<GROUP>>(() =>
                            {
                                lock (ms_locker)
                                    return (from grp in _db.GROUPS
                                            where
                                            grp.DEVICES.Any(d => d.CODE.ToUpper().Contains(DeviceSelectionString.ToUpper()))
                                            orderby grp.GROUP_NAME
                                            select grp).ToList();
                            });

                            masterViewSource.Source = data;
                        }
                        break;
                    case 4:
                        {
                            data = await Task.Factory.StartNew<IEnumerable<GROUP>>(() =>
                            {
                                lock (ms_locker)
                                    return (from grp in _db.GROUPS
                                            where grp.DEVICES.Any(d => d.CODE.ToUpper().Contains(DeviceSelectionString2.ToUpper()))
                                            orderby grp.GROUP_NAME
                                            select grp).ToList();
                            });

                            masterViewSource.Source = data;
                        }
                        break;
                }

            }

            SearchingLabelStatus = "Результаты поиска";
        }

        #region Params handlers

        private async void ButtonApplyParams_OnClick(object sender, RoutedEventArgs e)
        {
            if (_db.ChangeTracker.HasChanges())
            {
                ShowChangesPresentExclamation();
                return;
            }

            if (ParameterList != null)
            {
                RebuildVisibleParameterList();

                var savedIndex = lvDevices.SelectedIndex;
                await RebuildDeviceListAsync();
                lvDevices.SelectedIndex = (savedIndex >= lvDevices.Items.Count) ? lvDevices.Items.Count - 1 : savedIndex;
            }
        }

        private void RebuildVisibleParameterList()
        {
            VisibleParameterList = (from p in ParameterList
                                    where p.IsParamCheckedForVisible
                                    select new VisibleParameter(p)).ToList();

            LoadTableData(tableData);

            foreach (VisibleParameter param in VisibleParameterList)
            {
                AddToTableData(tableData, param.ParamName.Trim(), param.VisibleParamName.Trim());
            }

            BuildTemplate();
        }

        private void lblShowAll_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (var par in ParameterList)
                par.IsParamCheckedForVisible = true;
        }

        private void lblHideAll_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (var par in ParameterList)
                par.IsParamCheckedForVisible = false;
        }

        #endregion

        private static void ShowChangesPresentExclamation()
        {
            MessageBox.Show(
                "Операция невозможна, так как присутствуют несохраненные изменения.\nСохраните или отмените изменения перед выполнением данной операции",
                "Предупреждение валидатора", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private static string UnrollException(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Невозможно сохранить изменения: {ex.Message}");

            var cm = ex.InnerException;
            while (cm != null)
            {
                sb.AppendLine($"Причина ошибки: {cm.Message}");
                cm = cm.InnerException;
            }
            return sb.ToString();
        }

        #region Group handlers

        private void lbbDeleteGroup_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_db.ChangeTracker.HasChanges())
            {
                ShowChangesPresentExclamation();
                return;
            }

            try
            {
                if (MessageBox.Show($"Удалить группу: {SelectedGroup.GROUP_NAME.TrimEnd()}?", "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _db.GROUPS.Remove(SelectedGroup);
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OOPS: {ex.Message}", "BUGCHECK",
                    MessageBoxButton.OK, MessageBoxImage.Asterisk);

                return;
            }

            ButtonSearch_OnClick(sender, e);
        }

        private void lbbEditGroup_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_db.ChangeTracker.HasChanges())
            {
                ShowChangesPresentExclamation();
                return;
            }

            var str = Interaction.InputBox("Имя группы", "Редактирование", SelectedGroup.GROUP_NAME.TrimEnd());

            if (!String.IsNullOrWhiteSpace(str) && str != SelectedGroup.GROUP_NAME.TrimEnd())
            {
                try
                {
                    SelectedGroup.GROUP_NAME = str.Trim();
                    _db.SaveChanges();
                }
                catch (Exception)
                {
                    MessageBox.Show("Входные данные некорректны, либо уже существуют", "Ошибка операции",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    _db.Entry(SelectedGroup).Reload();

                    return;
                }

                GroupSelectionString = str.Trim();
                SelectionTypeIndex = 0;

                ButtonSearch_OnClick(sender, e);
            }
        }

        private void lbbAddGroup_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_db.ChangeTracker.HasChanges())
            {
                ShowChangesPresentExclamation();
                return;
            }

            var str = Interaction.InputBox("Имя группы", "Ввод данных");

            if (!String.IsNullOrWhiteSpace(str))
            {
                try
                {
                    var newGroup = _db.GROUPS.Create();
                    newGroup.GROUP_NAME = str;

                    _db.GROUPS.Attach(newGroup);
                    _db.Entry(newGroup).State = EntityState.Added;
                    _db.SaveChanges();
                }
                catch (Exception)
                {
                    MessageBox.Show("Входные данные некорректны, либо уже существуют", "Ошибка операции",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                GroupSelectionString = str;
                SelectionTypeIndex = 0;

                ButtonSearch_OnClick(sender, e);
            }
        }

        #endregion

        #region Device handlers

        private void lbbRefreshDevice_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_db.ChangeTracker.HasChanges())
            {
                ShowChangesPresentExclamation();
                return;
            }

            var savedIndex = lvDevices.SelectedIndex;

            _db.Entry(_selectedGroup).Reload();
            ((IObjectContextAdapter)_db).ObjectContext.Refresh(RefreshMode.StoreWins, _selectedGroup.DEVICES);
            _db.Entry(_selectedGroup).Collection(p => p.DEVICES).Load();
            RebuildDeviceList();

            lvDevices.SelectedIndex = (savedIndex >= lvDevices.Items.Count) ? lvDevices.Items.Count - 1 : savedIndex;
        }

        private async void lbbDeleteDevice_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (MessageBox.Show($"Удалить запись: {SelectedDevice.Device.CODE.TrimEnd()}?", "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _db.DEVICES.Remove(SelectedDevice.Device);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OOPS: {ex.Message}", "BUGCHECK",
                    MessageBoxButton.OK, MessageBoxImage.Asterisk);

                return;
            }

            var savedIndex = lvDevices.SelectedIndex;
            await RebuildDeviceListAsync();
            lvDevices.SelectedIndex = (savedIndex >= lvDevices.Items.Count) ? lvDevices.Items.Count - 1 : savedIndex;
        }

        private void lbbAddDevice_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var detailViewSource = (CollectionViewSource)Resources["DevicesView"];
            var devList = detailViewSource.Source as IList<DeviceAndParametersWithProfile>;

            if (devList == null)
                return;

            var newDevice = _db.DEVICES.Create();
            newDevice.CODE = "<BARCODE>";
            newDevice.SIL_N_1 = "<0>";
            newDevice.SIL_N_2 = "<0000>";
            newDevice.PROFILE_ID = Guid.Empty;
            newDevice.MME_CODE = "MANUAL";
            newDevice.USR = Cache.Welcome.SelectedAccount.ACC_NAME;
            newDevice.TS = DateTime.Now;
            newDevice.GROUP = SelectedGroup;
            _db.DEVICES.Add(newDevice);

            var newDevAndParams = new DeviceAndParametersWithProfile
            {
                Device = newDevice,
                Parameters = (from ch in VisibleParameterList
                              select new ParameterValueChunk
                              {
                                  ParameterEntity = (from prm in _db.PARAMS
                                                     where prm.PARAM_NAME == ch.ParamName
                                                     select prm).FirstOrDefault(),
                                  ChunkValues = new ObservableCollection<ParameterValue>(new[] { new ParameterValue() }),
                                  IsFake = true
                              }).OrderBy(p => p.ParameterEntity.PARAM_NAME).ToList()
            };

            devList.Add(newDevAndParams);
            detailViewSource.View.MoveCurrentTo(newDevAndParams);
        }

        #endregion

        #region CommitDiscard handlers

        private string GetChangesLog()
        {
            var sb = new StringBuilder();

            _db.ChangeTracker.DetectChanges();

            if (!_db.ChangeTracker.HasChanges())
                return sb.ToString();

            try
            {
                var changesAdd = (from e in _db.ChangeTracker.Entries()
                                  where e.State == EntityState.Added
                                  select e.Entity).ToList();

                var grpDev = from d in changesAdd.OfType<DEVICE>()
                             group d by d.GROUP.GROUP_NAME
                    into gd
                             select new
                             {
                                 G = gd.Key,
                                 D = gd.ToList()
                             };

                sb.AppendLine("\nДОБАВЛЕНИЕ ПРИБОРОВ: \n");
                foreach (var g in grpDev)
                {
                    sb.AppendLine($"\tГРУППА (ЗНП): {g.G.TrimEnd()}\n");
                    foreach (var device in g.D)
                        sb.AppendLine(
                            $"\t\tС/Н {device.CODE.TrimEnd()} с {device.SIL_N_1.TrimEnd()} - {device.SIL_N_2.Trim()}, {device.TS}, {device.USR.TrimEnd()} на {device.MME_CODE.TrimEnd()}\n");
                }

                var devPrm = from dp in changesAdd.OfType<DEV_PARAM>()
                             group dp by dp.DEVICE.CODE
                    into ddp
                             select new
                             {
                                 D = ddp.Key,
                                 P = ddp.ToList()
                             };

                sb.AppendLine("\nДОБАВЛЕНИЕ ПАРАМЕТРОВ: \n");
                foreach (var g in devPrm)
                {
                    sb.AppendLine($"\tПРИБОР: {g.D.TrimEnd()}\n");
                    foreach (var prm in g.P)
                        sb.AppendLine($"\t\t{prm.PARAM.PARAM_NAME.TrimEnd()} = {prm.VALUE}\n");
                }

                var changesMod = (from e in _db.ChangeTracker.Entries()
                                  where e.State == EntityState.Modified
                                  select e.Entity).ToList();

                var grpDevM = from d in changesMod.OfType<DEVICE>()
                              group d by d.GROUP.GROUP_NAME
                    into gd
                              select new
                              {
                                  G = gd.Key,
                                  D = gd.ToList()
                              };

                sb.AppendLine("\nИЗМЕНЕНИЕ ПРИБОРОВ: \n");
                foreach (var g in grpDevM)
                {
                    sb.AppendLine($"\tГРУППА (ЗНП): {g.G.TrimEnd()}\n");
                    foreach (var device in g.D)
                    {
                        sb.AppendLine($"\t\tС/Н {device.CODE.TrimEnd()}:\n");

                        var originalValues = _db.Entry(device).OriginalValues;
                        var currentValues = _db.Entry(device).CurrentValues;

                        foreach (var propertyName in originalValues.PropertyNames)
                        {
                            var original = originalValues[propertyName];
                            var current = currentValues[propertyName];

                            if (!Equals(original, current))
                                sb.AppendLine($"\t\t\t {propertyName}: {original} на {current}");
                        }
                    }
                }

                var devPrmM = from dp in changesMod.OfType<DEV_PARAM>()
                              group dp by dp.DEVICE.CODE
                    into ddp
                              select new
                              {
                                  D = ddp.Key,
                                  P = ddp.ToList()
                              };

                sb.AppendLine("\nИЗМЕНЕНИЕ ПАРАМЕТРОВ: \n");
                foreach (var g in devPrmM)
                {
                    sb.AppendLine($"\tПРИБОР: {g.D.TrimEnd()}\n");
                    foreach (var prm in g.P)
                    {
                        sb.AppendLine($"\t\t{prm.PARAM.PARAM_NAME.TrimEnd()}\n");

                        var originalValues = _db.Entry(prm).OriginalValues;
                        var currentValues = _db.Entry(prm).CurrentValues;

                        foreach (var propertyName in originalValues.PropertyNames)
                        {
                            var original = originalValues[propertyName];
                            var current = currentValues[propertyName];

                            if (!Equals(original, current))
                                sb.AppendLine($"\t\t\t {propertyName}: {original} на {current}");
                        }
                    }
                }


                var changesDel = (from e in _db.ChangeTracker.Entries()
                                  where e.State == EntityState.Deleted
                                  select e.Entity).ToList();

                var grpDevD = changesDel.OfType<DEVICE>();

                sb.AppendLine("\nУДАЛЕНИЕ ПРИБОРОВ: \n");
                foreach (var d in grpDevD)
                {
                    sb.AppendLine($"\t\tС/Н {d.CODE.TrimEnd()}\n");
                }

                var devPrmD = changesDel.OfType<DEV_PARAM>();

                sb.AppendLine("\nУДАЛЕНИЕ ПАРАМЕТРОВ: \n");
                foreach (var prm in devPrmD)
                {
                    sb.AppendLine($"\t\t{prm.VALUE}\n");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("OШИБКА ФОРМИРОВАНИЯ СПИСКА");
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }

        private void RollBack()
        {
            var context = _db;
            var changedEntries = context.ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged).ToList();

            foreach (var entry in changedEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }

        private void lbbSaveChanges_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_db.ChangeTracker.HasChanges())
            {
                MessageBox.Show(
                    "Операция успешна, изменения не обнаружены",
                    "Информация валидатора", MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            try
            {
                var chv = new ChangesViewer(GetChangesLog());
                var result = chv.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    _db.SaveChanges();

                    MessageBox.Show(
                        "Операция успешна, изменения сохранены",
                        "Информация валидатора", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (DbEntityValidationException ex)
            {
                var s = ex.EntityValidationErrors.Aggregate("Ошибки:\n",
                    (current1, entry) =>
                        entry.ValidationErrors.Aggregate(current1 + $"\t{entry.Entry.Entity.GetType().Name}:\n",
                            (current, err) => current + $"\t\t{err.ErrorMessage};\n"));

                MessageBox.Show(s, "Ошибка валидатора", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(UnrollException(ex), "Ошибка валидатора", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lbbDiscardChanges_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_db.ChangeTracker.HasChanges())
            {
                MessageBox.Show(
                    "Операция успешна, изменения не обнаружены",
                    "Информация валидатора", MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            if (MessageBox.Show("Отменить изменения?", "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    RollBack();

                    MessageBox.Show(
                        !_db.ChangeTracker.HasChanges()
                            ? "Операция успешна, изменения отменены"
                            : "Сбой операции, не удалось отменить изменения!\nПерезапустите приложение",
                        "Предупреждение валидатора", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    var savedIndex = lvDevices.SelectedIndex;
                    RebuildDeviceList();
                    lvDevices.SelectedIndex = (savedIndex >= lvDevices.Items.Count)
                        ? lvDevices.Items.Count - 1
                        : savedIndex;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(UnrollException(ex), "Ошибка валидатора", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        private void ButtonAddParamValue_OnClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var pv = btn?.DataContext as ParameterValueChunk;

            if (pv != null)
            {
                var newValueEntity = _db.DEV_PARAM.Create();
                newValueEntity.PARAM = pv.ParameterEntity;
                newValueEntity.DEVICE = SelectedDevice.Device;

                var newValue = new ParameterValue
                {
                    ParameterValueEntity = newValueEntity,
                    ParameterValueData = ""
                };

                pv.ChunkValues.Add(newValue);
            }
        }

        private void ButtonAcceptParamValues_OnClick(object sender, RoutedEventArgs e)
        {
            _db.ChangeTracker.DetectChanges();

            foreach (var parameterValueChunk in SelectedDevice.Parameters)
            {
                var listToRemove = new List<ParameterValue>();

                foreach (var parameterValue in parameterValueChunk.ChunkValues)
                {
                    if (parameterValue.ParameterValueEntity == null)
                    {
                        var newValueEntity = _db.DEV_PARAM.Create();
                        newValueEntity.PARAM = parameterValueChunk.ParameterEntity;
                        newValueEntity.DEVICE = SelectedDevice.Device;
                        parameterValue.ParameterValueEntity = newValueEntity;
                    }

                    var entry = _db.Entry(parameterValue.ParameterValueEntity);
                    switch (entry.State)
                    {
                        case EntityState.Detached:
                            {
                                if (parameterValue.IsValid)
                                {
                                    parameterValue.Commit();
                                    _db.DEV_PARAM.Add(parameterValue.ParameterValueEntity);
                                }
                                else
                                    parameterValue.ParameterValueEntity = null;
                            }
                            break;
                        case EntityState.Added:
                        case EntityState.Unchanged:
                        case EntityState.Modified:
                            {
                                if (parameterValue.IsEmpty)
                                {
                                    _db.DEV_PARAM.Remove(parameterValue.ParameterValueEntity);
                                    listToRemove.Add(parameterValue);
                                }
                                else
                                {
                                    if (!parameterValue.IsValid)
                                    {
                                        parameterValue.ParameterValueData = "0";
                                        parameterValue.ParameterValueEntity.VALUE = 0;
                                    }

                                    if (parameterValue.HasChanges)
                                        parameterValue.Commit();
                                }
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                foreach (var item in listToRemove)
                    parameterValueChunk.ChunkValues.Remove(item);

                if (parameterValueChunk.ChunkValues.Count == 0)
                    parameterValueChunk.ChunkValues.Add(new ParameterValue());
            }
        }

        private void ButtonRunReportDesigner_OnClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists(SelectedReportTemplate.FullPath) ||
                String.IsNullOrWhiteSpace(SelectedReportTemplate.FullPath))
            {
                RunReport(true, null);

                RebuildReportTemplateList();
            }
            else
            {
                MessageBox.Show($"Файл '{SelectedReportTemplate.FullPath}' недоступен.", "Ошибка запуска отчета",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ButtonRunReport_OnClick(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(SelectedReportTemplate.FullPath))
            {
                if (File.Exists(SelectedReportTemplate.FullPath))
                {
                    RunReport(false, reportPreview);

                    RebuildReportTemplateList();
                }
                else
                {
                    MessageBox.Show($"Файл '{SelectedReportTemplate.FullPath}' недоступен.", "Ошибка запуска отчета",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Выберите шаблон отчета", "Ошибка запуска отчета",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RunReport(bool useDesigner, PreviewControl preview)
        {
            if (VisibleParameterList != null)
            {
                int selParametersCount = VisibleParameterList.Count;
                if (selParametersCount > 10)
                    MessageBox.Show("Количество выбранных параметров " + selParametersCount.ToString() + ". Система допускает выбор не более 10 параметров. Уменьшите количество выбранных параметров.", "Ошибка при формировании отчёта", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                {
                    var report = new Report();

                    if (useDesigner && !String.IsNullOrWhiteSpace(SelectedReportTemplate.FullPath) || !useDesigner)
                        report.Load(SelectedReportTemplate.FullPath);

                    var detailViewSource = (CollectionViewSource)Resources["DevicesView"];
                    var data = detailViewSource.Source as IList<DeviceAndParametersWithProfile>;

                    if (data != null && (data.Count > 0))
                    {
                        var convertedData = PrepareReportDataDevices(data);

                        report.RegisterData((IEnumerable)convertedData, "DEVICES");
                        report.SetParameterValue("EXT_PAR_DevType", ReportDeviceType);
                        report.SetParameterValue("EXT_PAR_RepNum", ReportNumber);
                        report.SetParameterValue("EXT_PAR_Customer", ReportCustomer);
                        report.SetParameterValue("EXT_PAR_Count", data.Count);
                        PublishReportDataConditions(data, report, "CONDITIONS");
                        report.DoublePass = true;

                        if (useDesigner)
                        {
                            report.Design(true);
                        }
                        else
                        {
                            report.Preview = preview;

                            var win32Window = new System.Windows.Forms.NativeWindow();
                            win32Window.AssignHandle(new WindowInteropHelper(Cache.Main).Handle);

                            try
                            {
                                if (report.Prepare())
                                    report.ShowPrepared(true, win32Window);
                            }
                            catch (CompilerException ex)
                            {
                                MessageBox.Show("Некорректные поля отчета: " + ex.Message, "Ошибка запуска отчета",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Отсутствуют данные для формирования отчета", "Ошибка запуска отчета",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private static object PrepareReportDataDevices(IList<DeviceAndParametersWithProfile> source)
        {
            var paramListLengths = (from d in source
                                    from p in d.Parameters
                                    group p by p.ParameterEntity.PARAM_NAME.Trim()
                into gp
                                    select new
                                    {
                                        Nam = gp.Key,
                                        Val = gp.Max(t => t.ChunkValues.Count)
                                    }).ToDictionary(t => t.Nam, s => s.Val);

            var propXxDef = new Dictionary<string, Type>();
            foreach (var p in paramListLengths)
            {
                for (var i = 0; i < p.Value; ++i)
                    propXxDef.Add(p.Key + (i == 0 ? "" : (i + 1).ToString()), typeof(decimal));
            }

            var parametersReportItemType =
                CustomTypeFactory.CreateCustomType("ParametersReportItem",
                    propXxDef, typeof(ParametersReportItemBase));

            var propParamDef = new Dictionary<string, Type>
            {
                {"Parameters", parametersReportItemType}
            };

            var deviceReportItemType =
                CustomTypeFactory.CreateCustomType("DeviceReportItem",
                    propParamDef, typeof(DeviceReportItemBase));

            var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(deviceReportItemType));

            foreach (var dap in source)
            {
                var dr = (DeviceReportItemBase)Activator.CreateInstance(deviceReportItemType);
                dr.Properties = dap.Device;

                var props = Activator.CreateInstance(parametersReportItemType);

                foreach (var par in dap.Parameters)
                {
                    var key = par.ParameterEntity.PARAM_NAME.Trim();

                    for (var i = 0; i < paramListLengths[key]; ++i)
                    {
                        var targetPropInfo =
                            parametersReportItemType.GetProperty(key + (i == 0 ? "" : (i + 1).ToString()),
                                BindingFlags.Public | BindingFlags.Instance);

                        try
                        {
                            var pv = par.ChunkValues[i];
                            targetPropInfo?.SetValue(props, pv.IsEmpty ? null : (object)pv.ParameterValueEntity.VALUE);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            targetPropInfo?.SetValue(props, null);
                        }
                    }
                }

                var propParamPropInfo = deviceReportItemType.GetProperty("Parameters",
                                    BindingFlags.Public | BindingFlags.Instance);
                propParamPropInfo.SetValue(dr, props);

                var addMethod = typeof(List<>).MakeGenericType(deviceReportItemType).GetMethod("Add");
                addMethod.Invoke(list, new object[] { dr });
            }

            return list;
        }

        private void PublishReportDataConditions(IList<DeviceAndParametersWithProfile> source, Report report, string parent)
        {
            var data = SelectProfileConditionsAndRestrictions(source[0].Device.PROFILE_ID);

            if (data != null)
            {
                foreach (var rest in data.Restrictions)
                {
                    var i = 0;

                    foreach (var val in rest.Bounds)
                    {
                        report.SetParameterValue($"{parent}.{rest.Name}" + (i == 0 ? "" : (i + 1).ToString()) + ".MIN",
                            val.MinVal);
                        report.SetParameterValue($"{parent}.{rest.Name}" + (i == 0 ? "" : (i + 1).ToString()) + ".MAX",
                            val.MaxVal);

                        ++i;
                    }
                }

                foreach (var rest in data.Conditions)
                {
                    var i = 0;

                    foreach (var val in rest.Values)
                    {
                        report.SetParameterValue($"{parent}.{rest.Name}" + (i == 0 ? "" : (i + 1).ToString()), val.Trim());

                        ++i;
                    }
                }
            }
        }

        private void lbbBack_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_db.ChangeTracker.HasChanges())
            {
                ShowChangesPresentExclamation();
                return;
            }

            Cache.Main.mainFrame.NavigationService.GoBack();
        }


        private struct TableData
        {
            public string ParamName;
            public string Caption;
            public string Left;
            public string Width;
            public string Height;
            public string HorzAlign;
            public string VertAlign;
            public string Font;
            public string Top;
            public string FontForValue;
            public string DataSourceForValue;
            public string HideZerosForValue;
            public string FormatForValue;
            public string FormatUseLocaleForValue;
            public string FormatDecimalDigitForValues;
            public string FormatDecimalSeparatorForValue;
            public string FormatGroupSeparatorForValue;
            public string FormatNegativePatternForValue;
            public string HorzAlignForValue;
            public string VertAlignForValue;

        };


        private float StrToFloat(string value)
        {
            string decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            return Convert.ToSingle(value.Replace(".", decimalSeparator));
        }


        private string FloatToStr(float value)
        {
            string decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            return value.ToString().Replace(decimalSeparator, ".");

        }


        private string CalcColumnLeft(List<TableData> TableData)
        {
            string leftCoordinate = "9.45";

            //вычисляет координату Left для добавляемого столбца в уже существующий список столбцов
            if (TableData.Count == 0)
            {
                return leftCoordinate;
            }
            else
            {
                float sum = StrToFloat(leftCoordinate);

                for (int i = 0; i <= TableData.Count - 1; i++)
                    sum = sum + StrToFloat(TableData[i].Width);

                return FloatToStr(sum);
            }
        }


        private string CalcDataSourceByParamName(string ParamName)
        {
            return "[DEVICES.Parameters." + ParamName.Trim() + "]";

        }


        private void AddToTableData(List<TableData> TableData, string paramName, string caption)
        {
            if (TableData != null)
            {
                string left = CalcColumnLeft(TableData);
                TableData.Add(new TableData() { ParamName = paramName.Trim(), Caption = caption, Left = left, Width = "80", Height = heightTableData, HorzAlign = horzAlignTableData, VertAlign = vertAlignTableData, Font = fontTableData, Top = topTableData, FontForValue = fontForValueTableData, DataSourceForValue = CalcDataSourceByParamName(paramName), HideZerosForValue = hideZerosForValueTableData, FormatForValue = formatForValueTableData, FormatUseLocaleForValue = formatUseLocaleForValueTableData, FormatDecimalDigitForValues = FormatDecimalDigitForValues, FormatDecimalSeparatorForValue = formatDecimalSeparatorForValueTableData, FormatGroupSeparatorForValue = formatGroupSeparatorForValueTableData, FormatNegativePatternForValue = formatNegativePatternForValueTableData, HorzAlignForValue = HorzAlignForValueTableData, VertAlignForValue = vertAlignForValueTableData });

            }
        }


        private void LoadTableData(List<TableData> TableData)
        {
            //принимает на вход созданный TableData, очищает его содержимое и загружает в него данные: имя столбца, отступ слева, ширину и т.д.
            if (TableData != null)
            {
                TableData.Clear();

                string left = CalcColumnLeft(TableData);
                TableData.Add(new TableData() { ParamName = "CODE", Caption = "Code", Left = left, Width = "90", Height = heightTableData, HorzAlign = horzAlignTableData, VertAlign = vertAlignTableData, Font = fontTableData, Top = topTableData, FontForValue = fontForValueTableData, DataSourceForValue = "[DEVICES.Properties.CODE]" });
                left = CalcColumnLeft(TableData);

                TableData.Add(new TableData() { ParamName = "SIL_N_1", Caption = "Sil_N_1", Left = left, Width = "70", Height = heightTableData, HorzAlign = horzAlignTableData, VertAlign = vertAlignTableData, Font = fontTableData, Top = topTableData, FontForValue = fontTableData, DataSourceForValue = "[DEVICES.Properties.SIL_N_1]" });
                left = CalcColumnLeft(TableData);

                TableData.Add(new TableData() { ParamName = "SIL_N_2", Caption = "Sil_N_2", Left = left, Width = "70", Height = heightTableData, HorzAlign = horzAlignTableData, VertAlign = vertAlignTableData, Font = fontTableData, Top = topTableData, FontForValue = fontTableData, DataSourceForValue = "[DEVICES.Properties.SIL_N_2]" });
            }

        }


        private void RemoveAllChields(System.Xml.XmlNode xNode)
        {
            if (xNode != null)
            {
                System.Xml.XmlNode node = xNode.FirstChild;

                while (node != null)
                {
                    xNode.RemoveChild(node);
                    node = xNode.FirstChild;

                }
            }
        }

        private void BuildColumnHeader(System.Xml.XmlDocument xDoc, System.Xml.XmlNode xTableHeaderNode, List<TableData> TableData)
        {
            //построение всех нод столбцов, описывающих шапку таблицы
            if ((xTableHeaderNode != null) && (TableData != null))
            {
                //удаляем все ноды, которые хранит в себе xTableHeaderNode
                RemoveAllChields(xTableHeaderNode);

                //строим заголовок таблицы
                for (int i = 0; i <= TableData.Count - 1; i++)
                {
                    System.Xml.XmlElement xTextNodeChild = xDoc.CreateElement("TextObject");
                    xTableHeaderNode.AppendChild(xTextNodeChild);

                    xTextNodeChild.SetAttribute("Name", "TableHeader" + TableData[i].ParamName);
                    xTextNodeChild.SetAttribute("Left", TableData[i].Left);
                    xTextNodeChild.SetAttribute("Width", TableData[i].Width);
                    xTextNodeChild.SetAttribute("Height", TableData[i].Height);
                    xTextNodeChild.SetAttribute("Text", TableData[i].Caption);
                    xTextNodeChild.SetAttribute("HorzAlign", TableData[i].HorzAlign);
                    xTextNodeChild.SetAttribute("VertAlign", TableData[i].VertAlign);
                    xTextNodeChild.SetAttribute("Font", TableData[i].Font);
                }

                //чертим границы столбцов
                for (int i = 0; i <= TableData.Count - 1; i++)
                {
                    System.Xml.XmlElement xLineNodeChild = xDoc.CreateElement("LineObject");
                    xTableHeaderNode.AppendChild(xLineNodeChild);

                    xLineNodeChild.SetAttribute("Name", "TableHeaderRightBorderOfColumn" + i.ToString());
                    xLineNodeChild.SetAttribute("Left", TableData[i].Left);
                    xLineNodeChild.SetAttribute("Height", TableData[i].Height);
                    xLineNodeChild.SetAttribute("Diagonal", "true");
                }

                if (TableData.Count > 0)
                {
                    //чертим правую границу последнего столбца
                    System.Xml.XmlElement xRightBorderOfLastColumnNodeChild = xDoc.CreateElement("LineObject");
                    xTableHeaderNode.AppendChild(xRightBorderOfLastColumnNodeChild);

                    xRightBorderOfLastColumnNodeChild.SetAttribute("Name", "TableHeaderRightBorderOfLastColumn");
                    xRightBorderOfLastColumnNodeChild.SetAttribute("Left", CalcColumnLeft(TableData));
                    xRightBorderOfLastColumnNodeChild.SetAttribute("Height", TableData[0].Height);
                    xRightBorderOfLastColumnNodeChild.SetAttribute("Diagonal", "true");

                    //чертим нижнюю линию шапки таблицы
                    System.Xml.XmlElement xBottomLineNodeChild = xDoc.CreateElement("LineObject");
                    xTableHeaderNode.AppendChild(xBottomLineNodeChild);

                    xBottomLineNodeChild.SetAttribute("Name", "TableHeaderBottomLine");
                    xBottomLineNodeChild.SetAttribute("Left", TableData[0].Left);
                    xBottomLineNodeChild.SetAttribute("Top", TableData[0].Height); //Height не ошибка
                    xBottomLineNodeChild.SetAttribute("Width", reportTableWitdth);
                    xBottomLineNodeChild.SetAttribute("Diagonal", "true");
                }
            }

        }


        private void BuildColumn(System.Xml.XmlDocument xDoc, System.Xml.XmlNode xTableColumnNode, List<TableData> TableData)
        {
            //построение всех нод столбцов самой таблицы (данные таблицы)
            if ((xTableColumnNode != null) && (TableData != null))
            {
                //удаляем все ноды, которые хранит в себе xTableColumnNode
                RemoveAllChields(xTableColumnNode);

                //строим саму таблицу
                for (int i = 0; i <= TableData.Count - 1; i++)
                {
                    System.Xml.XmlElement xTextNodeChild = xDoc.CreateElement("TextObject");
                    xTableColumnNode.AppendChild(xTextNodeChild);

                    xTextNodeChild.SetAttribute("Name", "TableColumn" + TableData[i].ParamName);
                    xTextNodeChild.SetAttribute("Left", TableData[i].Left);
                    xTextNodeChild.SetAttribute("Top", TableData[i].Top);
                    xTextNodeChild.SetAttribute("Width", TableData[i].Width);
                    xTextNodeChild.SetAttribute("Height", TableData[i].Height);
                    xTextNodeChild.SetAttribute("Text", TableData[i].DataSourceForValue);
                    xTextNodeChild.SetAttribute("Font", TableData[i].Font);

                    //для реквизитов, которые имеют FormatForValue == "Number" допишем дополнительные атрибуты 
                    if (TableData[i].FormatForValue == "Number")
                    {
                        xTextNodeChild.SetAttribute("Format", TableData[i].FormatForValue);
                        xTextNodeChild.SetAttribute("Format.UseLocale", TableData[i].FormatUseLocaleForValue);
                        xTextNodeChild.SetAttribute("Format.DecimalDigit", TableData[i].FormatDecimalDigitForValues);
                        xTextNodeChild.SetAttribute("Format.DecimalSeparator", TableData[i].FormatDecimalSeparatorForValue);
                        xTextNodeChild.SetAttribute("Format.GroupSeparator", TableData[i].FormatGroupSeparatorForValue);
                        xTextNodeChild.SetAttribute("Format.NegativePattern", TableData[i].FormatNegativePatternForValue);
                        xTextNodeChild.SetAttribute("HorzAlign", TableData[i].HorzAlignForValue);
                        xTextNodeChild.SetAttribute("VertAlign", TableData[i].VertAlignForValue);
                    }

                }

                string BottomLineTop = "29";

                //чертим границы столбцов в области данных таблицы
                for (int i = 0; i <= TableData.Count - 1; i++)
                {
                    System.Xml.XmlElement xLineNodeChild = xDoc.CreateElement("LineObject");
                    xTableColumnNode.AppendChild(xLineNodeChild);

                    xLineNodeChild.SetAttribute("Name", "TableColumnRightBorderOfColumn" + i.ToString());
                    xLineNodeChild.SetAttribute("Top", "-1");
                    xLineNodeChild.SetAttribute("Left", TableData[i].Left);
                    xLineNodeChild.SetAttribute("Height", BottomLineTop);
                    xLineNodeChild.SetAttribute("Diagonal", "true");
                }

                if (TableData.Count > 0)
                {
                    //рисуем правую границу в области данных таблицы для самого последнего столбца
                    System.Xml.XmlElement xRightBorderLineNodeChild = xDoc.CreateElement("LineObject");
                    xTableColumnNode.AppendChild(xRightBorderLineNodeChild);

                    xRightBorderLineNodeChild.SetAttribute("Name", "TableColumnRightBorderOfLastColumn");
                    xRightBorderLineNodeChild.SetAttribute("Top", "-1");
                    xRightBorderLineNodeChild.SetAttribute("Left", CalcColumnLeft(TableData));
                    xRightBorderLineNodeChild.SetAttribute("Height", BottomLineTop);
                    xRightBorderLineNodeChild.SetAttribute("Diagonal", "true");

                    //рисуем нижнюю линию таблицы
                    System.Xml.XmlElement xBottomLineNodeChild = xDoc.CreateElement("LineObject");
                    xTableColumnNode.AppendChild(xBottomLineNodeChild);

                    xBottomLineNodeChild.SetAttribute("Name", "TableColumnBottomLine");
                    xBottomLineNodeChild.SetAttribute("Left", TableData[0].Left);
                    xBottomLineNodeChild.SetAttribute("Top", BottomLineTop);
                    xBottomLineNodeChild.SetAttribute("Width", reportTableWitdth);
                    xBottomLineNodeChild.SetAttribute("Diagonal", "true");
                }

            }

        }


        private void BuildTemplateHeaderAndColumn(System.Xml.XmlDocument xDoc, System.Xml.XmlNode xTableHeaderNode, System.Xml.XmlNode xTableColumnNode)
        {
            //построение нового столбца в таблице шаблона
            if ((xTableHeaderNode != null) && (xTableColumnNode != null))
            {
                //построение шапки таблицы
                BuildColumnHeader(xDoc, xTableHeaderNode, tableData);

                //построение самой таблицы
                BuildColumn(xDoc, xTableColumnNode, tableData);
            }

        }

        private string FullPath()
        {
            //возвращает путь к исполняемому файлу данного приложения
            string location = System.Reflection.Assembly.GetExecutingAssembly().Location;

            //получаем из location путь до файла
            return Path.GetDirectoryName(location);
        }


        private string TemplateFileFullAddress()
        {
            //возвращает полный путь к файлу шаблона FastReport
            return FullPath() + @"\ReportTemplates\" + templateFileName;
        }


        private string RemoveSubStr(string source, string substr)
        {
            //удаляет из строки source все встречающиеся подстроки substr
            string result = source;
            bool found = true;
            int index = -1;

            while (found)
            {
                index = result.IndexOf(substr);

                found = (index != -1);

                if (found)
                    result = result.Remove(index, substr.Length);
            }

            return result;
        }


        private string PrepareTemplateForSave(string Template)
        {
            //рассматриваем XML как обычную строку, удаляем объявление ноды "FakeNodeForDelete"
            string res = RemoveSubStr(Template, "<" + fakeNodeForDelete + ">");
            res = RemoveSubStr(res, "</" + fakeNodeForDelete + ">");

            //удаляем объявления CDATA, их два. формат объявления CDATA:
            //  <![CDATA[
            //    ]]>
            res = RemoveSubStr(res, "<![CDATA[");
            res = RemoveSubStr(res, "]]>");

            return res;
        }


        private void BuildTemplate()
        {
            //процедура построения шаблона отчёта FastReport
            //данная процедура динамически строит табличное представление шаблона в соответствии с выбранными пользователем параметрами в данной фоме
            System.Xml.XmlDocument xDoc = new System.Xml.XmlDocument();

            //грузим из ресурсов XML текст шаблона
            //этот XML текст изначально модифицирован: все ноды кроме "PageHeaderBand" и "DataBand" завёрнуты в CDATA и добавлена фальшивая нода "FakeNodeForDelete" для того чтобы не нарушать принцип построения XML файла
            //необходимость такой модификации связана с тем, что при загрузке файла система автоматически заменяет последовательности #13 и #10 на #xD и #xA соответственно (возможно подменяются и др. последовательности, но указанные точно). т.е. это делается автоматически и неявно
            xDoc.InnerXml = SCME.SQLDatabaseClient.Properties.Resources.DefaultFastReportTemplate;

            System.Xml.XmlNode xTableHeaderNode = xDoc.SelectSingleNode("//" + fakeNodeForDelete + "/PageHeaderBand");
            System.Xml.XmlNode xTableColumnNode = xDoc.SelectSingleNode("//" + fakeNodeForDelete + "/DataBand");

            if ((xTableHeaderNode != null) && (xTableColumnNode != null))
            {
                //для построения таблицы параметров отчёта используем возможности XML
                BuildTemplateHeaderAndColumn(xDoc, xTableHeaderNode, xTableColumnNode);

                //сохранять XML файл будем как обычный текстовый файл
                //перед сохранением файла отчёта FastReport вернём ему его первоначальный вид
                string templateText = PrepareTemplateForSave(xDoc.InnerXml);
                string templateFileFullAddress = TemplateFileFullAddress();

                File.WriteAllText(templateFileFullAddress, templateText, Encoding.Unicode);

            }

        }

    }

}

