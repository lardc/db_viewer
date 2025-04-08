using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.Gate;
using SCME.Types.Profiles;
using SCME.UI.Annotations;
using SCME.UI.Properties;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using HWFaultReason = SCME.Types.Gate.HWFaultReason;
using HWWarningReason = SCME.Types.Gate.HWWarningReason;
using TestParameters = SCME.Types.Commutation.TestParameters;
using TestResults = SCME.Types.Gate.TestResults;

namespace SCME.UI.PagesUser
{
    /// <summary>
    /// Interaction logic for UserTestPage.xaml
    /// </summary>
    public partial class UserTestPage : INotifyPropertyChanged
    {
        private const int DATA_LENGTH = 600;

        private readonly SolidColorBrush m_XRed, m_XGreen, m_XGreen3, m_XOrange;
        private readonly Brush m_TbBrush;
        private readonly List<string> m_Errors1, m_Errors2;
        private List<Types.Gate.TestResults> m_ResultsGate1, m_ResultsGate2;
        private List<Types.SL.TestResults> m_ResultsVTM1, m_ResultsVTM2; //m_ResultsITM1, m_ResultsITM2
        private List<Types.BVT.TestResults> m_ResultsBVT1, m_ResultsBVT2;
        private List<Types.dVdt.TestResults> _dvdTestResults1, _dvdTestResults2;
        private List<Types.ATU.TestResults> m_ResultsATU1, m_ResultsATU2;
        private List<Types.QrrTq.TestResults> m_ResultsQrrTq1, m_ResultsQrrTq2;
        private List<Types.RAC.TestResults> m_ResultsRAC1, m_ResultsRAC2;
        private DeviceState m_StateGate, m_StateVtm, m_StateBvt, m_StatedVdt, m_StateATU, m_StateQrrTq, m_StateRAC;
        private Profile m_Profile;
        private bool m_SpecialMeasureMode;
        private bool m_TwoPosRequested, m_IsRunning;
        private int m_CurrentPos = 1;
        private MeasureDialog measureDialog;
        private bool wasCurrentMore;
        private int? m_ClassByProfileName = null;

        public UserTestPage()
        {
            this.DataContext = new UserTestPageViewModel();

            m_ResultsGate1 = new List<Types.Gate.TestResults>();
            m_ResultsVTM1 = new List<Types.SL.TestResults>();
            m_ResultsBVT1 = new List<Types.BVT.TestResults>();
            _dvdTestResults1 = new List<Types.dVdt.TestResults>();
            m_ResultsATU1 = new List<Types.ATU.TestResults>();
            m_ResultsQrrTq1 = new List<Types.QrrTq.TestResults>();
            m_ResultsRAC1 = new List<Types.RAC.TestResults>();

            m_ResultsGate2 = new List<Types.Gate.TestResults>();
            m_ResultsVTM2 = new List<Types.SL.TestResults>();
            m_ResultsBVT2 = new List<Types.BVT.TestResults>();
            _dvdTestResults2 = new List<Types.dVdt.TestResults>();
            m_ResultsATU2 = new List<Types.ATU.TestResults>();
            m_ResultsQrrTq2 = new List<Types.QrrTq.TestResults>();
            m_ResultsRAC2 = new List<Types.RAC.TestResults>();

            m_StateGate = DeviceState.None;
            m_StateVtm = DeviceState.None;
            m_StateBvt = DeviceState.None;
            m_StateATU = DeviceState.None;
            m_StateQrrTq = DeviceState.None;
            m_StateRAC = DeviceState.None;

            m_Errors1 = new List<string>();
            m_Errors2 = new List<string>();

            InitializeComponent();

            m_XRed = (SolidColorBrush)FindResource("xRed1");
            m_XGreen = (SolidColorBrush)FindResource("xGreen1");
            m_XGreen3 = (SolidColorBrush)FindResource("xGreen3");
            m_XOrange = (SolidColorBrush)FindResource("xOrange1");

            m_TbBrush = tbPsdJob.BorderBrush;

            ClearStatus(true, true);

            if (Settings.Default.SinglePositionModuleMode)
            {
                gridResult2.Visibility = Visibility.Collapsed;
                line2.Visibility = Visibility.Collapsed;
                Grid.SetColumn(gridResult1, 3);
                Grid.SetColumn(line1, 3);

                chartPlotter2.Visibility = Visibility.Collapsed;

                Grid.SetRow(chartPlotter1, 6);
                Grid.SetColumnSpan(chartPlotter1, 3);
                Grid.SetRowSpan(chartPlotter1, 2);
            }

            SetChartPlotterSettings(Settings.Default.SinglePositionModuleMode);
        }

        private void SetChartPlotterSettings(bool SinglePositionModuleMode)
        {
            //настройка внешнего вида chartPlotter
            //блок QrrTq никогда не может быть совместно установленным в стенд вместе например BVT, Gate и т.п. если QrrTq установлен, то он один единственный измерительный блок в стенде
            int PositionCount = SinglePositionModuleMode ? 1 : 2;

            for (int i = 1; i <= PositionCount; i++)
            {
                if ((Settings.Default.PlotUserQrrTq) && (Settings.Default.QrrTqIsVisible))
                {
                    //для стенда с блоком QrrTq не требуется установка ограничения по оси Y, ось Y имеет размерность Амперы, ось X - время в мкс
                    VerticalAxisTitle vAxisTitle = FindChild<VerticalAxisTitle>(this.MainGrid, "chartPlotter" + i.ToString() + "VerticalAxisTitle");
                    if (vAxisTitle != null)
                        vAxisTitle.Content = Properties.Resources.Graph_IA;

                    HorizontalAxisTitle hAxisTitle = FindChild<HorizontalAxisTitle>(this.MainGrid, "chartPlotter" + i.ToString() + "HorizontalAxisTitle");
                    if (hAxisTitle != null)
                        hAxisTitle.Content = Properties.Resources.Graph_TimeUs;
                }
                else
                {
                    //для стенда без блока QrrTq требуется установка ограничения по оси Y
                    ViewportAxesRangeRestriction restr = new ViewportAxesRangeRestriction { YRange = new DisplayRange(-7, 7) };
                    ChartPlotter cp = FindChild<ChartPlotter>(this.MainGrid, "chartPlotter" + i.ToString());
                    if (cp != null)
                        cp.Viewport.Restrictions.Add(restr);
                }
            }
        }

        public class DisplayRange
        {
            public double Start { get; set; }
            public double End { get; set; }

            public DisplayRange(double start, double end)
            {
                Start = start;
                End = end;
            }
        }

        public class ViewportAxesRangeRestriction : IViewportRestriction
        {
            public DisplayRange XRange = null;
            public DisplayRange YRange = null;

            public Rect Apply(Rect oldVisible, Rect newVisible, Viewport2D viewport)
            {
                if (XRange != null)
                {
                    newVisible.X = XRange.Start;
                    newVisible.Width = XRange.End - XRange.Start;
                }

                if (YRange != null)
                {
                    newVisible.Y = YRange.Start;
                    newVisible.Height = YRange.End - YRange.Start;
                }

                return newVisible;
            }

            public event EventHandler Changed;
        }

        public void InitSorting()
        {
            var collectionView1 = CollectionViewSource.GetDefaultView(ListViewResults1.ItemsSource);
            collectionView1.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));

            var collectionView2 = CollectionViewSource.GetDefaultView(ListViewResults2.ItemsSource);
            collectionView2.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
        }

        public void InitTemp()
        {
            if (Profile.Temperature >= 60)
            {
                lblTitleTop.Visibility =
                   lblTitleBot.Visibility = BotTempLabel.Visibility = TopTempLabel.Visibility = Visibility.Visible;
                Cache.Net.StartHeating(Profile.Temperature);
            }
            else
            {
                Cache.Net.StopHeating();
            }
        }

        #region Bounded properties
        public Types.Profiles.Profile Profile
        {
            get { return m_Profile; }
            set
            {
                m_Profile = value;
                ((UserTestPageViewModel)this.DataContext).Profile = value;
                OnPropertyChanged("Profile");
            }
        }

        public bool SpecialMeasureMode
        {
            get { return m_SpecialMeasureMode; }
            set
            {
                m_SpecialMeasureMode = value;
                ((UserTestPageViewModel)this.DataContext).SpecialMeasureMode = value;
                OnPropertyChanged("SpecialMeasureMode");
            }
        }

        public List<Types.Gate.TestResults> ResultsGate1
        {
            get { return m_ResultsGate1; }
            set
            {
                m_ResultsGate1 = value;
                OnPropertyChanged("ResultsGate1");
            }
        }

        public List<Types.Gate.TestResults> ResultsGate2
        {
            get { return m_ResultsGate2; }
            set
            {
                m_ResultsGate2 = value;
                OnPropertyChanged("ResultsGate2");
            }
        }

        public List<Types.SL.TestResults> ResultsVTM1
        {
            get { return m_ResultsVTM1; }
            set
            {
                m_ResultsVTM1 = value;
                OnPropertyChanged("ResultsVTM1");
            }
        }

        public List<Types.SL.TestResults> ResultsVTM2
        {
            get { return m_ResultsVTM2; }
            set
            {
                m_ResultsVTM2 = value;
                OnPropertyChanged("ResultsVTM2");
            }
        }

        public List<Types.BVT.TestResults> ResultsBVT1
        {
            get { return m_ResultsBVT1; }
            set
            {
                m_ResultsBVT1 = value;
                OnPropertyChanged("ResultsBVT1");
            }
        }

        public List<Types.BVT.TestResults> ResultsBVT2
        {
            get { return m_ResultsBVT2; }
            set
            {
                m_ResultsBVT2 = value;
                OnPropertyChanged("ResultsBVT2");
            }
        }

        public List<Types.ATU.TestResults> ResultsATU1
        {
            get { return m_ResultsATU1; }
            set
            {
                m_ResultsATU1 = value;
                OnPropertyChanged("ResultsATU1");
            }
        }

        public List<Types.ATU.TestResults> ResultsATU2
        {
            get { return m_ResultsATU2; }
            set
            {
                m_ResultsATU2 = value;
                OnPropertyChanged("ResultsATU2");
            }
        }

        public List<Types.QrrTq.TestResults> ResultsQrrTq1
        {
            get { return m_ResultsQrrTq1; }
            set
            {
                m_ResultsQrrTq1 = value;
                OnPropertyChanged("ResultsQrrTq1");
            }
        }

        public List<Types.QrrTq.TestResults> ResultsQrrTq2
        {
            get { return m_ResultsQrrTq2; }
            set
            {
                m_ResultsQrrTq2 = value;
                OnPropertyChanged("ResultsQrrTq2");
            }
        }

        public List<Types.RAC.TestResults> ResultsRAC1
        {
            get { return m_ResultsRAC1; }
            set
            {
                m_ResultsRAC1 = value;
                OnPropertyChanged("ResultsRAC1");
            }
        }

        public List<Types.RAC.TestResults> ResultsRAC2
        {
            get { return m_ResultsRAC2; }
            set
            {
                m_ResultsRAC2 = value;
                OnPropertyChanged("ResultsRAC2");
            }
        }

        #endregion

        #region Set result

        private List<DependencyObject> GetGateItemContainer()
        {
            var gateResults = new List<DependencyObject>(5);

            bool isNewlyRealized;

            if (m_CurrentPos == 1)
            {
                for (int i = 0; i < ListViewResults1.Items.Count; i++)
                {
                    if (ListViewResults1.Items[i] is Types.Gate.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults1.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            gateResults.Add(child);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ListViewResults2.Items.Count; i++)
                {
                    if (ListViewResults2.Items[i] is Types.Gate.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults2.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            gateResults.Add(child);
                        }
                    }
                }
            }

            return gateResults;
        }

        /*
        internal void SetResultAll(DeviceState State)
        {
            if (State == DeviceState.None || State == DeviceState.Heating)
                return;

            Cache.Main.AccountButtonVisibility = (State == DeviceState.InProcess ? Visibility.Hidden : Visibility.Visible);
            Cache.Main.GoTechButtonVisibility = (State == DeviceState.InProcess ? Visibility.Hidden : Visibility.Visible);

            if (State != DeviceState.InProcess)
            {
                Cache.Net.StartHeating(Profile.Temperature);
                var structOrd = tbStructureOrder.Text;
                var structName = tbStructureName.Text;

                if (m_TwoPosRequested)
                {
                    m_TwoPosRequested = false;
                    m_CurrentPos = 2;

                    StartN(2);
                }
                else
                    IsRunning = false;

                var paramsClamp = new Types.Clamping.TestParameters
                {
                    StandardForce = Types.Clamping.ClampingForceInternal.Custom,
                    CustomForce = Profile.ParametersClamp,
                    IsHeightMeasureEnabled = Profile.IsHeightMeasureEnabled
                };

                if (!paramsClamp.IsHeightMeasureEnabled)
                {
                    if (!String.IsNullOrWhiteSpace(tbPartyNumber.Text) && !String.IsNullOrWhiteSpace(tbDeviceName.Text))
                    {
                        var isSent = true;

                        try
                        {
                            Cache.Net.WriteResultServer(new ResultItem
                            {
                                Timestamp = DateTime.Now,
                                User = Cache.Main.AccountName,
                                MmeCode = Cache.Main.MmeCode,
                                ProfileKey = Profile.Key,
                                Party = tbPartyNumber.Text,
                                Code = tbDeviceName.Text,
                                StructureOrd = structOrd,
                                StructureName = structName,
                                Gate = (m_CurrentPos == 1) ? ResultsGate1.ToArray() : ResultsGate2.ToArray(),
                                VTM = (m_CurrentPos == 1) ? ResultsVTM1.ToArray() : ResultsVTM2.ToArray(),
                                BVT = (m_CurrentPos == 1) ? ResultsBVT1.ToArray() : ResultsBVT2.ToArray(),
                                ATU = (m_CurrentPos == 1) ? ResultsATU1.ToArray() : ResultsATU2.ToArray(),
                                GateTestParameters = Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray(),
                                VTMTestParameters = Profile.TestParametersAndNormatives.OfType<Types.SL.TestParameters>().ToArray(),
                                BVTTestParameters = Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray(),
                                ATUTestParameters = Profile.TestParametersAndNormatives.OfType<Types.ATU.TestParameters>().ToArray(),
                                Position = m_CurrentPos,
                                IsHeightMeasureEnabled = false
                            }, (m_CurrentPos == 1) ? m_Errors1 : m_Errors2);
                        }
                        catch (Exception)
                        {
                            isSent = false;
                        }

                        Cache.Net.WriteResultLocal(new ResultItem
                        {
                            Timestamp = DateTime.Now,
                            User = Cache.Main.AccountName,
                            MmeCode = Cache.Main.MmeCode,
                            ProfileKey = Profile.Key,
                            Party = tbPartyNumber.Text,
                            Code = tbDeviceName.Text,
                            StructureOrd = structOrd,
                            StructureName = structName,
                            Gate = (m_CurrentPos == 1) ? ResultsGate1.ToArray() : ResultsGate2.ToArray(),
                            VTM = (m_CurrentPos == 1) ? ResultsVTM1.ToArray() : ResultsVTM2.ToArray(),
                            BVT = (m_CurrentPos == 1) ? ResultsBVT1.ToArray() : ResultsBVT2.ToArray(),
                            ATU = (m_CurrentPos == 1) ? ResultsATU1.ToArray() : ResultsATU2.ToArray(),
                            GateTestParameters = Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray(),
                            VTMTestParameters = Profile.TestParametersAndNormatives.OfType<Types.SL.TestParameters>().ToArray(),
                            BVTTestParameters = Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray(),
                            ATUTestParameters = Profile.TestParametersAndNormatives.OfType<Types.ATU.TestParameters>().ToArray(),
                            Position = m_CurrentPos,
                            IsHeightMeasureEnabled = false,
                            IsSentToServer = isSent
                        }, (m_CurrentPos == 1) ? m_Errors1 : m_Errors2);
                    }

                    tbDeviceName.Text = "";
                    tbStructureOrder.Text = "";
                    tbStructureName.Text = "";
                    tbDeviceName.Focus();
                    return;
                }

                var result = false;
                if (State == DeviceState.Success)
                {
                    measureDialog = new MeasureDialog(paramsClamp)
                    {
                        Top = 0,
                        Left = 0
                    };
                    result = measureDialog.ShowDialog() ?? false;
                }

                if (!String.IsNullOrWhiteSpace(tbPartyNumber.Text) && !String.IsNullOrWhiteSpace(tbDeviceName.Text))
                {
                    var isSent = true;

                    try
                    {
                        Cache.Net.WriteResultServer(new ResultItem
                        {
                            Timestamp = DateTime.Now,
                            User = Cache.Main.AccountName,
                            MmeCode = Cache.Main.MmeCode,
                            ProfileKey = Profile.Key,
                            Party = tbPartyNumber.Text,
                            Code = tbDeviceName.Text,
                            StructureOrd = structOrd,
                            StructureName = structName,
                            Gate = (m_CurrentPos == 1) ? ResultsGate1.ToArray() : ResultsGate2.ToArray(),
                            VTM = (m_CurrentPos == 1) ? ResultsVTM1.ToArray() : ResultsVTM2.ToArray(),
                            BVT = (m_CurrentPos == 1) ? ResultsBVT1.ToArray() : ResultsBVT2.ToArray(),
                            ATU = (m_CurrentPos == 1) ? ResultsATU1.ToArray() : ResultsATU2.ToArray(),
                            GateTestParameters = Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray(),
                            VTMTestParameters = Profile.TestParametersAndNormatives.OfType<Types.SL.TestParameters>().ToArray(),
                            BVTTestParameters = Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray(),

                            Position = m_CurrentPos,
                            IsHeightMeasureEnabled = true,
                            IsHeightOk = result
                        }, (m_CurrentPos == 1) ? m_Errors1 : m_Errors2);
                    }
                    catch (Exception)
                    {
                        isSent = false;
                    }

                    Cache.Net.WriteResultLocal(new ResultItem
                    {
                        Timestamp = DateTime.Now,
                        User = Cache.Main.AccountName,
                        MmeCode = Cache.Main.MmeCode,
                        ProfileKey = Profile.Key,
                        Party = tbPartyNumber.Text,
                        Code = tbDeviceName.Text,
                        StructureOrd = structOrd,
                        StructureName = structName,
                        Gate = (m_CurrentPos == 1) ? ResultsGate1.ToArray() : ResultsGate2.ToArray(),
                        VTM = (m_CurrentPos == 1) ? ResultsVTM1.ToArray() : ResultsVTM2.ToArray(),
                        BVT = (m_CurrentPos == 1) ? ResultsBVT1.ToArray() : ResultsBVT2.ToArray(),
                        ATU = (m_CurrentPos == 1) ? ResultsATU1.ToArray() : ResultsATU2.ToArray(),
                        GateTestParameters = Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray(),
                        VTMTestParameters = Profile.TestParametersAndNormatives.OfType<Types.SL.TestParameters>().ToArray(),
                        BVTTestParameters = Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray(),
                        Position = m_CurrentPos,
                        IsHeightMeasureEnabled = true,
                        IsHeightOk = result,
                        IsSentToServer = isSent
                    }, (m_CurrentPos == 1) ? m_Errors1 : m_Errors2);
                }

                if (m_CurrentPos == 1)
                {
                    labelHeightResult1.Content = result ? "OK" : "Not OK";
                    labelHeightResult1.Background = result ? Brushes.LightGreen : Brushes.Tomato;
                }
                else
                {
                    labelHeightResult2.Content = result ? "OK" : "Not OK";
                    labelHeightResult2.Background = result ? Brushes.LightGreen : Brushes.Tomato;
                }
                tbDeviceName.Text = "";
                tbStructureOrder.Text = "";
                tbStructureName.Text = "";
                tbDeviceName.Focus();
            }
            else
                ClearStatus(m_CurrentPos == 1, true);
        }
        */

        internal void SetResultAll(DeviceState State)
        {
            if (State == DeviceState.None || State == DeviceState.Heating)
                return;

            Cache.Main.AccountButtonVisibility = (State == DeviceState.InProcess ? Visibility.Hidden : Visibility.Visible);
            Cache.Main.GoTechButtonVisibility = (State == DeviceState.InProcess ? Visibility.Hidden : Visibility.Visible);

            if (State == DeviceState.InProcess)
                ClearStatus(m_CurrentPos == 1, true);
            else
            {
                Cache.Net.StartHeating(Profile.Temperature);

                if (m_TwoPosRequested)
                {
                    m_TwoPosRequested = false;
                    m_CurrentPos = 2;

                    StartN(2);
                }
                else IsRunning = false;

                var paramsClamp = new Types.Clamping.TestParameters
                {
                    StandardForce = Types.Clamping.ClampingForceInternal.Custom,
                    CustomForce = Profile.ParametersClamp,
                    IsHeightMeasureEnabled = Profile.IsHeightMeasureEnabled
                };

                bool HeightMeasureResult = false;

                if (paramsClamp.IsHeightMeasureEnabled && State == DeviceState.Success)
                {
                    //спрашиваем у пользователя результаты измерения высоты (он выполняет это измерение вручную с помощью калибра)
                    measureDialog = new MeasureDialog(paramsClamp)
                    {
                        Top = 0,
                        Left = 0
                    };

                    HeightMeasureResult = measureDialog.ShowDialog() ?? false;
                }

                CustomControl.ValidatingTextBox tbNumber = null;
                bool needSave = false;

                if (this.Profile != null)
                {
                    switch (ProfileRoutines.CalcSubjectForMeasure(this.Profile.Name))
                    {
                        case SubjectForMeasure.PSD:
                            needSave = (!String.IsNullOrWhiteSpace(tbPsdJob.Text) && !String.IsNullOrWhiteSpace(tbPsdSerialNumber.Text));
                            tbNumber = tbPsdSerialNumber;
                            break;

                        case SubjectForMeasure.PSE:
                            needSave = (!String.IsNullOrWhiteSpace(tbPseJob.Text) && !String.IsNullOrWhiteSpace(tbPseNumber.Text));
                            tbNumber = tbPseNumber;
                            break;

                        default:
                            needSave = false;
                            tbNumber = null;
                            break;
                    }
                }

                if (needSave)
                {
                    ResultItem DataForSave = new ResultItem
                    {
                        Timestamp = DateTime.Now,
                        User = Cache.Main.AccountName,
                        MmeCode = Cache.Main.MmeCode,
                        ProfileKey = Profile.Key,
                        PsdJob = tbPsdJob.Text,
                        PsdSerialNumber = tbPsdSerialNumber.Text,
                        PseJob = tbPseJob.Text,
                        PseNumber = tbPseNumber.Text,
                        Gate = (m_CurrentPos == 1) ? ResultsGate1.ToArray() : ResultsGate2.ToArray(),
                        VTM = (m_CurrentPos == 1) ? ResultsVTM1.ToArray() : ResultsVTM2.ToArray(),
                        BVT = (m_CurrentPos == 1) ? ResultsBVT1.ToArray() : ResultsBVT2.ToArray(),
                        ATU = (m_CurrentPos == 1) ? ResultsATU1.ToArray() : ResultsATU2.ToArray(),
                        QrrTq = (m_CurrentPos == 1) ? ResultsQrrTq1.ToArray() : ResultsQrrTq2.ToArray(),
                        RAC = (m_CurrentPos == 1) ? ResultsRAC1.ToArray() : ResultsRAC2.ToArray(),
                        GateTestParameters = Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray(),
                        VTMTestParameters = Profile.TestParametersAndNormatives.OfType<Types.SL.TestParameters>().ToArray(),
                        BVTTestParameters = Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray(),
                        ATUTestParameters = Profile.TestParametersAndNormatives.OfType<Types.ATU.TestParameters>().ToArray(),
                        QrrTqTestParameters = Profile.TestParametersAndNormatives.OfType<Types.QrrTq.TestParameters>().ToArray(),
                        RACTestParameters = Profile.TestParametersAndNormatives.OfType<Types.RAC.TestParameters>().ToArray(),
                        Position = m_CurrentPos,
                        IsHeightMeasureEnabled = paramsClamp.IsHeightMeasureEnabled,
                        IsHeightOk = HeightMeasureResult
                    };

                    List<string> errors = (m_CurrentPos == 1) ? m_Errors1 : m_Errors2;

                    try
                    {
                        //сохраняем результаты измерений в центральную базу данных
                        Cache.Net.WriteResultServer(DataForSave, errors);
                        DataForSave.IsSentToServer = true;
                    }
                    catch (Exception)
                    {
                        DataForSave.IsSentToServer = false;
                    }

                    //сохраняем результаты измерений в локальную базу данных
                    Cache.Net.WriteResultLocal(DataForSave, errors);

                    //вычисляем класс только что измеренного изделия и выводим его на форму
                    if (DataForSave.IsSentToServer)
                        CalcDeviceClass(tbNumber, true);
                }

                if (paramsClamp.IsHeightMeasureEnabled)
                {
                    if (m_CurrentPos == 1)
                    {
                        labelHeightResult1.Content = HeightMeasureResult ? "OK" : "Not OK";
                        labelHeightResult1.Background = HeightMeasureResult ? Brushes.LightGreen : Brushes.Tomato;
                    }
                    else
                    {
                        labelHeightResult2.Content = HeightMeasureResult ? "OK" : "Not OK";
                        labelHeightResult2.Background = HeightMeasureResult ? Brushes.LightGreen : Brushes.Tomato;
                    }
                }

                tbPseNumber.Text = "";
                tbPsdSerialNumber.Text = "";

                if (tbNumber.Visibility == Visibility.Visible)
                    tbNumber.Focus();
            }
        }

        private int _gateCounter;

        internal void SetResultGateKelvin(DeviceState state, bool isKelvinOk, long testTypeId)
        {
            if (state == DeviceState.InProcess)
            {
                _gateCounter++;
            }
            m_StateGate = state;

            var gateResults = GetGateItemContainer();

            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            var labelKelvinResult = FindChild<Label>(presenter, "labelKelvinResult1");
            if (labelKelvinResult != null)
                SetLabel(labelKelvinResult, state, isKelvinOk, isKelvinOk ? Properties.Resources.Ok : Properties.Resources.Fault);

            if (state != DeviceState.InProcess)
            {
                ((m_CurrentPos == 1)
                    ? ResultsGate1.Find(g => g.TestTypeId.Equals(testTypeId))
                    : ResultsGate2.Find(g => g.TestTypeId.Equals(testTypeId))).IsKelvinOk = isKelvinOk;

                if (!isKelvinOk)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_KELVIN");
            }
        }

        internal void SetResultGateResistance(DeviceState state, float resistance, long testTypeId)
        {
            m_StateGate = state;

            var gateResults = GetGateItemContainer();

            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            var labelRgResult = FindChild<Label>(presenter, "labelRgResult1");
            if (labelRgResult != null)
                SetLabel(labelRgResult, state,
                     resistance <= (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].Resistance,
                     string.Format("{0}", resistance));

            if (state != DeviceState.InProcess)
            {
                ((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).Resistance = resistance;

                if (resistance > (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].Resistance)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_RG");
            }
        }

        internal void SetResultGateGate(DeviceState state, float igt, float vgt, IList<short> arrayI,
                                        IList<short> arrayV, long testTypeId)
        {
            m_StateGate = state;

            var gateResults = GetGateItemContainer();

            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            ((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).IGT = igt;
            ((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).VGT =
                (float)Math.Round((decimal)(vgt / 1000.0), 2, MidpointRounding.ToEven);

            var labelIgtResult = FindChild<Label>(presenter, "labelIgtResult1");
            if (labelIgtResult != null)
                SetLabel(labelIgtResult, state, igt <= (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].IGT,
                     string.Format("{0}", igt));

            var labelVgtResult = FindChild<Label>(presenter, "labelVgtResult1");
            if (labelVgtResult != null)
                SetLabel(labelVgtResult, state,
                        ((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).VGT <= (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].VGT,
                        string.Format("{0}", ((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).VGT));

            if (state != DeviceState.InProcess)
            {
                if (igt > (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].IGT)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IGT");
                if (((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).VGT > (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].VGT)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_VGT");
            }

            if (state == DeviceState.Success && Settings.Default.PlotUserGate)
            {
                PlotX(m_CurrentPos, "Igt", m_XRed.Color, arrayI);
                PlotX(m_CurrentPos, Properties.Resources.Vgt, m_XOrange.Color, arrayV);
            }
        }

        internal void SetResultGateIh(DeviceState state, float ih, IList<short> array, long testTypeId)
        {
            m_StateGate = state;

            var gateResults = GetGateItemContainer();

            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            var labelIhResult = FindChild<Label>(presenter, "labelIhResult1");
            if (labelIhResult != null)
                SetLabel(labelIhResult, state, ih <= (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].IH,
                     string.Format("{0}", ih));

            if (state != DeviceState.InProcess)
            {
                ((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).IH = ih;

                if (ih > (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].IH)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IH");
            }

            if (state == DeviceState.Success && Settings.Default.PlotUserGate)
                PlotX(m_CurrentPos, "Ih", m_XGreen.Color, array);
        }

        internal void SetResultGateIl(DeviceState state, float il, long testTypeId)
        {
            m_StateGate = state;

            var gateResults = GetGateItemContainer();

            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            var labelIlResult = FindChild<Label>(presenter, "labelIlResult1");
            if (labelIlResult != null)
                SetLabel(labelIlResult, state, il <= (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].IL, string.Format("{0}", il));

            if (state != DeviceState.InProcess)
            {
                ((m_CurrentPos == 1) ? ResultsGate1[_gateCounter] : ResultsGate2[_gateCounter]).IL = il;

                if (il > (Profile.TestParametersAndNormatives.OfType<Types.Gate.TestParameters>().ToArray())[_gateCounter].IL)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IL");
            }
        }

        internal void SetGateWarning(HWWarningReason Warning)
        {
            var gateResults = GetGateItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            var labelGateWarning = FindChild<Label>(presenter, "labelGateWarning1");
            if (labelGateWarning != null && (labelGateWarning).Visibility != Visibility.Visible)
            {
                labelGateWarning.Content = Warning.ToString();
                labelGateWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetGateProblem(HWProblemReason Problem)
        {
            var gateResults = GetGateItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            var labelGateWarning = FindChild<Label>(presenter, "labelGateWarning1");
            if (labelGateWarning != null)
            {
                labelGateWarning.Content = Problem.ToString();
                labelGateWarning.Visibility = Visibility.Visible;
            }

            switch (Problem)
            {
                case Types.Gate.HWProblemReason.HoldReachTimeout:
                case Types.Gate.HWProblemReason.LatchCurrentHigh:
                case Types.Gate.HWProblemReason.LatchFollowingError:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IHL_PROBLEM");
                    break;
                case Types.Gate.HWProblemReason.GateCurrentHigh:
                case Types.Gate.HWProblemReason.GateFollowingError:
                case Types.Gate.HWProblemReason.GateIgtOverload:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_GT_PROBLEM");
                    break;
            }
        }

        internal void SetGateFault(HWFaultReason Fault)
        {
            var gateResults = GetGateItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(gateResults[_gateCounter]);

            var labelGateFault = FindChild<Label>(presenter, "labelGateFault1");
            if (labelGateFault != null)
            {
                labelGateFault.Content = Fault.ToString();
                labelGateFault.Visibility = Visibility.Visible;
            }

            switch (Fault)
            {
                case Types.Gate.HWFaultReason.HoldProcessError:
                case Types.Gate.HWFaultReason.LatchProcessError:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IHL_PROBLEM");
                    break;
                case Types.Gate.HWFaultReason.GateProcessError:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_GT_PROBLEM");
                    break;
            }

            IsRunning = false;
        }

        private int slCounter;

        private List<DependencyObject> GetVtmItemContainer()
        {
            var gateResults = new List<DependencyObject>(5);

            bool isNewlyRealized;

            if (m_CurrentPos == 1)
            {
                for (int i = 0; i < ListViewResults1.Items.Count; i++)
                {
                    if (ListViewResults1.Items[i] is Types.SL.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults1.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            gateResults.Add(child);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ListViewResults2.Items.Count; i++)
                {
                    if (ListViewResults2.Items[i] is Types.SL.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults2.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            gateResults.Add(child);
                        }
                    }
                }
            }

            return gateResults;
        }

        internal void SetResultSl(DeviceState state, Types.SL.TestResults result)
        {
            m_StateVtm = state;
            if (state == DeviceState.InProcess)
                slCounter++;

            if (m_CurrentPos == 1)
                ResultsVTM1[slCounter] = result;
            else
                ResultsVTM2[slCounter] = result;

            ((m_CurrentPos == 1) ? ResultsVTM1[slCounter] : ResultsVTM2[slCounter]).Voltage =
                (float)Math.Round((decimal)(result.Voltage / 1000.0), 2, MidpointRounding.ToEven);

            var vtmResults = GetVtmItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(vtmResults[slCounter]);

            var labelVtmResult = FindChild<Label>(presenter, "labelVtmResult1");
            if (labelVtmResult != null)
                SetLabel(labelVtmResult, state,
                     ((m_CurrentPos == 1) ? ResultsVTM1[slCounter] : ResultsVTM2[slCounter]).Voltage <= (Profile.TestParametersAndNormatives.OfType<Types.SL.TestParameters>().ToArray())[slCounter].VTM,
                     string.Format("{0}", ((m_CurrentPos == 1) ? ResultsVTM1[slCounter] : ResultsVTM2[slCounter]).Voltage));

            if (state != DeviceState.InProcess)
                if (((m_CurrentPos == 1) ? ResultsVTM1[slCounter] : ResultsVTM2[slCounter]).Voltage > (Profile.TestParametersAndNormatives.OfType<Types.SL.TestParameters>().ToArray())[slCounter].VTM)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_VTM");

            //ток. не используем реализацию SetLabel т.к. нам не надо устанавливать Background
            var labelItmResult = FindChild<Label>(presenter, "labelItmResult1");
            if (labelItmResult != null)
                labelItmResult.Content = string.Format("{0}", ((m_CurrentPos == 1) ? ResultsVTM1[slCounter] : ResultsVTM2[slCounter]).Current);

            if (state == DeviceState.Success && Settings.Default.PlotUserSL)
            {
                PlotX(m_CurrentPos, @"Itm", m_XRed.Color, result.ITMArray);
                PlotX(m_CurrentPos, @"Vtm", m_XOrange.Color, result.VTMArray);
            }
        }

        internal void SetSLWarning(Types.SL.HWWarningReason Warning)
        {
            var vtmResults = GetVtmItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(vtmResults[slCounter]);

            var labelVtmWarning = FindChild<Label>(presenter, "labelVtmWarning1");
            if (labelVtmWarning != null && labelVtmWarning.Visibility != Visibility.Visible)
            {
                labelVtmWarning.Content = Warning.ToString();
                labelVtmWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetSLProblem(Types.SL.HWProblemReason Problem)
        {
            var vtmResults = GetVtmItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(vtmResults[slCounter]);

            var labelVtmWarning = FindChild<Label>(presenter, "labelVtmWarning1");
            if (labelVtmWarning != null)
            {
                labelVtmWarning.Content = Problem.ToString();
                labelVtmWarning.Visibility = Visibility.Visible;
            }

            switch (Problem)
            {
                case Types.SL.HWProblemReason.NoCurrent:
                case Types.SL.HWProblemReason.FollowingError:
                case Types.SL.HWProblemReason.SCurveRateIsTooLarge:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_ITM_PROBLEM");
                    break;
                case Types.SL.HWProblemReason.VTMOverload:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_VTM_PROBLEM");
                    break;
            }
        }

        internal void SetSLFault(Types.SL.HWFaultReason Fault)
        {
            var vtmResults = GetVtmItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(vtmResults[slCounter]);

            var labelVtmFault = FindChild<Label>(presenter, "labelVtmFault1");
            if (labelVtmFault != null)
            {
                labelVtmFault.Content = Fault.ToString();
                labelVtmFault.Visibility = Visibility.Visible;
            }

            switch (Fault)
            {
                case Types.SL.HWFaultReason.ThermalCell1:
                case Types.SL.HWFaultReason.ThermalCell2:
                case Types.SL.HWFaultReason.ThermalCell3:
                case Types.SL.HWFaultReason.ThermalCell4:
                case Types.SL.HWFaultReason.ThermalCell5:
                case Types.SL.HWFaultReason.Timeout:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_ITM_PROBLEM");
                    break;
            }

            IsRunning = false;
        }

        private int bvtCounter;

        private List<DependencyObject> GetBvtItemContainer()
        {
            var gateResults = new List<DependencyObject>(5);

            bool isNewlyRealized;

            if (m_CurrentPos == 1)
            {
                for (int i = 0; i < ListViewResults1.Items.Count; i++)
                {
                    if (ListViewResults1.Items[i] is Types.BVT.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults1.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            gateResults.Add(child);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ListViewResults2.Items.Count; i++)
                {
                    if (ListViewResults2.Items[i] is Types.BVT.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults2.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            gateResults.Add(child);
                        }
                    }
                }
            }

            return gateResults;
        }

        internal void SetResultBvtAll(DeviceState state)
        {
            if (state == DeviceState.InProcess)
                bvtCounter++;
        }

        internal void SetResultBvtDirect(DeviceState state, Types.BVT.TestResults result)
        {
            m_StateBvt = state;


            if (m_CurrentPos == 1)
            {
                ResultsBVT1[bvtCounter].IDRM = result.IDRM;
                ResultsBVT1[bvtCounter].VDRM = result.VDRM;
            }
            else
            {
                ResultsBVT2[bvtCounter].IDRM = result.IDRM;
                ResultsBVT2[bvtCounter].VDRM = result.VDRM;
            }

            var bvtItemContainer = GetBvtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(bvtItemContainer[bvtCounter]);

            if ((Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].MeasurementMode == Types.BVT.BVTMeasurementMode.ModeV)
            {
                var labelBvtIdrmResult = FindChild<Label>(presenter, "labelBvtIdrmResult1");
                if (labelBvtIdrmResult != null)
                    labelBvtIdrmResult.Content = result.IDRM.ToString(CultureInfo.InvariantCulture);

                var labelBvtVdrmResult = FindChild<Label>(presenter, "labelBvtVdrmResult1");
                if (labelBvtVdrmResult != null)
                    SetLabel(labelBvtVdrmResult, state, result.VDRM > (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].VDRM, result.VDRM.ToString(CultureInfo.InvariantCulture));

                if (state != DeviceState.InProcess)
                    if (result.VDRM <= (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].VDRM)
                        ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_VDRM");
            }
            if ((Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].MeasurementMode == Types.BVT.BVTMeasurementMode.ModeI)
            {
                var labelBvtVdrmResult = FindChild<Label>(presenter, "labelBvtVdrmResult1");
                if (labelBvtVdrmResult != null)
                    labelBvtVdrmResult.Content = result.VDRM.ToString(CultureInfo.InvariantCulture);

                var labelBvtIdrmResult = FindChild<Label>(presenter, "labelBvtIdrmResult1");
                if (labelBvtIdrmResult != null)
                    SetLabel(labelBvtIdrmResult, state, result.IDRM <= (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IDRM,
                         result.IDRM.ToString(CultureInfo.InvariantCulture));

                if (state != DeviceState.InProcess)
                    if (result.IDRM > (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IDRM)
                        ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IDRM");
            }

            if (state == DeviceState.Success && Settings.Default.PlotUserBVT)
                PlotYX(m_CurrentPos, "Direct", m_XRed.Color, result.VoltageData, result.CurrentData);
        }

        internal void SetResultReverseBvt(DeviceState state, Types.BVT.TestResults result)
        {
            m_StateBvt = state;

            if (m_CurrentPos == 1)
            {
                ResultsBVT1[bvtCounter].IRRM = result.IRRM;
                ResultsBVT1[bvtCounter].VRRM = result.VRRM;
            }
            else
            {
                ResultsBVT2[bvtCounter].IRRM = result.IRRM;
                ResultsBVT2[bvtCounter].VRRM = result.VRRM;
            }

            var bvtItemContainer = GetBvtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(bvtItemContainer[bvtCounter]);

            if ((Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].MeasurementMode == Types.BVT.BVTMeasurementMode.ModeV)
            {
                var labelBvtIrrmResult = FindChild<Label>(presenter, "labelBvtIrrmResult1");
                if (labelBvtIrrmResult != null)
                    labelBvtIrrmResult.Content = result.IRRM.ToString(CultureInfo.InvariantCulture);


                var labelBvtVrrmResult = FindChild<Label>(presenter, "labelBvtVrrmResult1");
                if (labelBvtVrrmResult != null)
                    SetLabel(labelBvtVrrmResult, state, result.VRRM > (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].VRRM,
                         result.VRRM.ToString(CultureInfo.InvariantCulture));

                if (state != DeviceState.InProcess)
                    if (result.VRRM <= (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].VRRM)
                        ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_VRRM");
            }
            if ((Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].MeasurementMode == Types.BVT.BVTMeasurementMode.ModeI)
            {
                var labelBvtVrrmResult = FindChild<Label>(presenter, "labelBvtVrrmResult1");
                if (labelBvtVrrmResult != null)
                    labelBvtVrrmResult.Content = result.VRRM.ToString(CultureInfo.InvariantCulture);

                var labelBvtIrrmResult = FindChild<Label>(presenter, "labelBvtIrrmResult1");
                if (labelBvtIrrmResult != null)
                    SetLabel(labelBvtIrrmResult, state, result.IRRM <= (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IRRM,
                         result.IRRM.ToString(CultureInfo.InvariantCulture));

                if (state != DeviceState.InProcess)
                    if (result.IRRM > (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IRRM)
                        ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IRRM");
            }

            if (state == DeviceState.Success && Settings.Default.PlotUserBVT)
                PlotYX(m_CurrentPos, "Reverse", m_XGreen3.Color, result.VoltageData, result.CurrentData);
        }

        internal void SetResultBvtUdsmUrsmDirect(DeviceState state, Types.BVT.TestResults result)
        {
            m_StateBvt = state;

            if (m_CurrentPos == 1)
            {
                ResultsBVT1[bvtCounter].IDSM = result.IDSM;
                ResultsBVT1[bvtCounter].VDSM = result.VDSM;
            }
            else
            {
                ResultsBVT2[bvtCounter].IDSM = result.IDSM;
                ResultsBVT2[bvtCounter].VDSM = result.VDSM;
            }

            var bvtItemContainer = GetBvtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(bvtItemContainer[bvtCounter]);

            var labelBvtVdsmResult = FindChild<Label>(presenter, "labelBvtVdsmResult1");
            if (labelBvtVdsmResult != null)
                labelBvtVdsmResult.Content = result.VDSM.ToString(CultureInfo.InvariantCulture);

            var labelBvtIdsmResult = FindChild<Label>(presenter, "labelBvtIdsmResult1");
            if (labelBvtIdsmResult != null)
                SetLabel(labelBvtIdsmResult, state, result.IDSM < (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IDRM, result.IDSM.ToString(CultureInfo.InvariantCulture));

            if (state != DeviceState.InProcess)
                if (result.IDSM >= (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IDRM)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IDSM");
        }

        internal void SetResultBvtUdsmUrsmReverse(DeviceState state, Types.BVT.TestResults result)
        {
            m_StateBvt = state;

            if (m_CurrentPos == 1)
            {
                ResultsBVT1[bvtCounter].IRSM = result.IRSM;
                ResultsBVT1[bvtCounter].VRSM = result.VRSM;
            }
            else
            {
                ResultsBVT2[bvtCounter].IRSM = result.IRSM;
                ResultsBVT2[bvtCounter].VRSM = result.VRSM;
            }

            var bvtItemContainer = GetBvtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(bvtItemContainer[bvtCounter]);

            var labelBvtVrsmResult = FindChild<Label>(presenter, "labelBvtVrsmResult1");
            if (labelBvtVrsmResult != null)
                labelBvtVrsmResult.Content = result.VRSM.ToString(CultureInfo.InvariantCulture);

            var labelBvtIrsmResult = FindChild<Label>(presenter, "labelBvtIrsmResult1");
            if (labelBvtIrsmResult != null)
                SetLabel(labelBvtIrsmResult, state, result.IRSM < (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IRRM, result.IRSM.ToString(CultureInfo.InvariantCulture));

            if (state != DeviceState.InProcess)
                if (result.IRSM >= (Profile.TestParametersAndNormatives.OfType<Types.BVT.TestParameters>().ToArray())[bvtCounter].IRRM)
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_IRSM");
        }

        internal void SetBvtWarning(Types.BVT.HWWarningReason Warning)
        {
            var bvtItemContainer = GetBvtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(bvtItemContainer[bvtCounter]);

            var labelBvtWarning = FindChild<Label>(presenter, "labelBvtWarning1");

            if (labelBvtWarning != null && labelBvtWarning.Visibility != Visibility.Visible)
            {
                if (labelBvtWarning.Content.ToString() != string.Empty)
                    labelBvtWarning.Content += " ";

                labelBvtWarning.Content += Warning.ToString();
                labelBvtWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetBvtProblem(Types.BVT.HWProblemReason Problem)
        {
            var bvtItemContainer = GetBvtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(bvtItemContainer[bvtCounter]);
            var labelBvtWarning = FindChild<Label>(presenter, "labelBvtWarning1");

            if (labelBvtWarning != null)
            {
                if (labelBvtWarning.Content.ToString() != string.Empty)
                    labelBvtWarning.Content += " ";

                labelBvtWarning.Content += Problem.ToString();
                labelBvtWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetBvtFault(Types.BVT.HWFaultReason Fault)
        {
            var bvtItemContainer = GetBvtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(bvtItemContainer[bvtCounter]);

            var labelBvtFault = FindChild<Label>(presenter, "labelBvtFault1");
            if (labelBvtFault != null)
            {
                if (labelBvtFault.Content.ToString() != string.Empty)
                    labelBvtFault.Content += " ";

                labelBvtFault.Content += Fault.ToString();
                labelBvtFault.Visibility = Visibility.Visible;
            }
            switch (Fault)
            {
                case Types.BVT.HWFaultReason.BridgeOverload:
                case Types.BVT.HWFaultReason.TemperatureOverload:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_RM_OVERLOAD");
                    break;
            }

            IsRunning = false;
        }

        private int dvdtCounter;

        private List<DependencyObject> GetDvDtItemContainer()
        {
            var results = new List<DependencyObject>(5);

            bool isNewlyRealized;

            if (m_CurrentPos == 1)
            {
                for (int i = 0; i < ListViewResults1.Items.Count; i++)
                {
                    if (ListViewResults1.Items[i] is Types.dVdt.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults1.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            results.Add(child);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ListViewResults2.Items.Count; i++)
                {
                    if (ListViewResults2.Items[i] is Types.dVdt.TestParameters)
                    {
                        IItemContainerGenerator generator = ListViewResults2.ItemContainerGenerator;

                        var position = generator.GeneratorPositionFromIndex(i);
                        using (generator.StartAt(position, GeneratorDirection.Forward, true))
                        {
                            var child = generator.GenerateNext(out isNewlyRealized);
                            generator.PrepareItemContainer(child);
                            results.Add(child);
                        }
                    }
                }
            }

            return results;
        }

        internal void SetResultdVdt(DeviceState state, Types.dVdt.TestResults Result)
        {
            m_StatedVdt = state;

            if (m_StatedVdt == DeviceState.InProcess)
                dvdtCounter++;

            if (m_CurrentPos == 1)
            {
                _dvdTestResults1[dvdtCounter] = Result;
            }
            else
            {
                _dvdTestResults2[dvdtCounter] = Result;
            }

            var dvDtItemContainer = GetDvDtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(dvDtItemContainer[dvdtCounter]);

            var labelResult = FindChild<Label>(presenter, "labelResult");
            if (labelResult != null)
            {
                SetLabel(labelResult, state, Result.Passed, Result.Passed ? "OK" : "Not OK");
            }

            if (state == DeviceState.Success)
            {
                var labelDvdTVoltageRate = FindChild<Label>(presenter, "labelVoltageRate");
                if (labelDvdTVoltageRate != null)
                {
                    SetLabel(labelDvdTVoltageRate, state, true, Result.VoltageRate.ToString());
                }
            }
        }

        internal void SetDVdtWarning(Types.dVdt.HWWarningReason Warning)
        {
            var dvDtItemContainer = GetDvDtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(dvDtItemContainer[dvdtCounter]);

            var labelDvdtWarning = FindChild<Label>(presenter, "labelDvdtWarning");

            if (labelDvdtWarning != null && labelDvdtWarning.Visibility != Visibility.Visible)
            {
                labelDvdtWarning.Content = Warning.ToString();
                labelDvdtWarning.Visibility = Visibility.Visible;
            }
        }

        internal void SetDVdtFault(Types.dVdt.HWFaultReason Fault)
        {
            var dvDtItemContainer = GetDvDtItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(dvDtItemContainer[dvdtCounter]);

            var labelDvdtFault = FindChild<Label>(presenter, "labelDvdtFault");
            if (labelDvdtFault != null)
            {
                labelDvdtFault.Content = Fault.ToString();
                labelDvdtFault.Visibility = Visibility.Visible;
            }
            switch (Fault)
            {
                case Types.dVdt.HWFaultReason.LinkCell1:
                case Types.dVdt.HWFaultReason.LinkCell2:
                case Types.dVdt.HWFaultReason.LinkCell3:
                case Types.dVdt.HWFaultReason.LinkCell4:
                case Types.dVdt.HWFaultReason.LinkCell5:
                case Types.dVdt.HWFaultReason.LinkCell6:
                case Types.dVdt.HWFaultReason.NotReadyCell1:
                case Types.dVdt.HWFaultReason.NotReadyCell2:
                case Types.dVdt.HWFaultReason.NotReadyCell3:
                case Types.dVdt.HWFaultReason.NotReadyCell4:
                case Types.dVdt.HWFaultReason.NotReadyCell5:
                case Types.dVdt.HWFaultReason.NotReadyCell6:
                    ((m_CurrentPos == 1) ? m_Errors1 : m_Errors2).Add("ERR_ITM_PROBLEM");
                    break;
            }

            IsRunning = false;
        }

        private int ATUCounter;

        private List<DependencyObject> GetATUItemContainer()
        {
            var results = new List<DependencyObject>(5);
            ListView ListView = null;

            switch (m_CurrentPos)
            {
                case (1):
                    ListView = ListViewResults1;
                    break;

                default:
                    ListView = ListViewResults2;
                    break;
            }

            bool isNewlyRealized;

            for (int i = 0; i < ListView.Items.Count; i++)
            {
                if (ListView.Items[i] is Types.ATU.TestParameters)
                {
                    IItemContainerGenerator generator = ListView.ItemContainerGenerator;

                    var position = generator.GeneratorPositionFromIndex(i);
                    using (generator.StartAt(position, GeneratorDirection.Forward, true))
                    {
                        var child = generator.GenerateNext(out isNewlyRealized);
                        generator.PrepareItemContainer(child);
                        results.Add(child);
                    }
                }
            }

            return results;
        }

        internal void SetResultATU(DeviceState state, Types.ATU.TestResults result)
        {
            m_StateATU = state;

            if (m_StateATU == DeviceState.InProcess)
                ATUCounter++;

            if (m_CurrentPos == 1)
            {
                ResultsATU1[ATUCounter].UBR = result.UBR;
                ResultsATU1[ATUCounter].UPRSM = result.UPRSM;
                ResultsATU1[ATUCounter].IPRSM = result.IPRSM;
                ResultsATU1[ATUCounter].PRSM = result.PRSM;
            }
            else
            {
                ResultsATU2[ATUCounter].UBR = result.UBR;
                ResultsATU2[ATUCounter].UPRSM = result.UPRSM;
                ResultsATU2[ATUCounter].IPRSM = result.IPRSM;
                ResultsATU2[ATUCounter].PRSM = result.PRSM;
            }

            if (state == DeviceState.Success)
            {
                List<DependencyObject> ATUItemContainer = GetATUItemContainer();
                ContentPresenter presenter = FindVisualChild<ContentPresenter>(ATUItemContainer[ATUCounter]);

                Label labelMeasure = FindChild<Label>(presenter, "lbAtuUBR");

                if (labelMeasure != null)
                {
                    SetLabel(labelMeasure, state, true, result.UBR.ToString());
                }

                labelMeasure = FindChild<Label>(presenter, "lbAtuUPRSM");

                if (labelMeasure != null)
                {
                    SetLabel(labelMeasure, state, true, result.UPRSM.ToString());
                }

                labelMeasure = FindChild<Label>(presenter, "lbAtuIPRSM");

                if (labelMeasure != null)
                {
                    SetLabel(labelMeasure, state, true, result.IPRSM.ToString());
                }

                labelMeasure = FindChild<Label>(presenter, "lbAtuPRSM");

                if (labelMeasure != null)
                {
                    SetLabel(labelMeasure, state, true, result.PRSM.ToString());
                }
            }
        }

        internal void SetATUWarning(ushort Warning)
        {
            var ATUItemContainer = GetATUItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(ATUItemContainer[ATUCounter]);

            var labelATUWarning = FindChild<Label>(presenter, "lbAtuWarning");

            if (labelATUWarning != null && labelATUWarning.Visibility != Visibility.Visible)
            {
                Types.ATU.HWWarningReason WarningReason = (Types.ATU.HWWarningReason)Warning;
                labelATUWarning.Content = WarningReason.ToString();

                labelATUWarning.Visibility = Visibility.Visible;
            }

            IsRunning = false;
        }

        internal void SetATUFault(ushort Fault)
        {
            var ATUItemContainer = GetATUItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(ATUItemContainer[ATUCounter]);

            var labelATUFault = FindChild<Label>(presenter, "lbAtuFaultReason");

            if (labelATUFault != null && labelATUFault.Visibility != Visibility.Visible)
            {
                Types.ATU.HWFaultReason FaultReason = (Types.ATU.HWFaultReason)Fault;
                labelATUFault.Content = FaultReason.ToString();

                labelATUFault.Visibility = Visibility.Visible;
            }
        }

        private int QrrTqCounter;

        private List<DependencyObject> GetQrrTqItemContainer()
        {
            var results = new List<DependencyObject>(7);
            ListView ListView = null;

            switch (m_CurrentPos)
            {
                case (1):
                    ListView = ListViewResults1;
                    break;

                default:
                    ListView = ListViewResults2;
                    break;
            }

            bool isNewlyRealized;

            for (int i = 0; i < ListView.Items.Count; i++)
            {
                if (ListView.Items[i] is Types.QrrTq.TestParameters)
                {
                    IItemContainerGenerator generator = ListView.ItemContainerGenerator;

                    var position = generator.GeneratorPositionFromIndex(i);
                    using (generator.StartAt(position, GeneratorDirection.Forward, true))
                    {
                        var child = generator.GenerateNext(out isNewlyRealized);
                        generator.PrepareItemContainer(child);
                        results.Add(child);
                    }
                }
            }

            return results;
        }

        internal void SetResultQrrTq(DeviceState state, Types.QrrTq.TestResults result)
        {
            m_StateQrrTq = state;

            if (m_StateQrrTq == DeviceState.InProcess)
                QrrTqCounter++;

            if (m_CurrentPos == 1)
            {
                ResultsQrrTq1[QrrTqCounter].OffStateVoltage = result.OffStateVoltage;
                ResultsQrrTq1[QrrTqCounter].OsvRate = result.OsvRate;

                ResultsQrrTq1[QrrTqCounter].Idc = result.Idc;
                ResultsQrrTq1[QrrTqCounter].Qrr = result.Qrr;
                ResultsQrrTq1[QrrTqCounter].Irr = result.Irr;
                ResultsQrrTq1[QrrTqCounter].Trr = result.Trr;
                ResultsQrrTq1[QrrTqCounter].DCFactFallRate = result.DCFactFallRate;
                ResultsQrrTq1[QrrTqCounter].Tq = result.Tq;
            }
            else
            {
                ResultsQrrTq2[QrrTqCounter].OffStateVoltage = result.OffStateVoltage;
                ResultsQrrTq2[QrrTqCounter].OsvRate = result.OsvRate;

                ResultsQrrTq2[QrrTqCounter].Idc = result.Idc;
                ResultsQrrTq2[QrrTqCounter].Qrr = result.Qrr;
                ResultsQrrTq2[QrrTqCounter].Irr = result.Irr;
                ResultsQrrTq2[QrrTqCounter].Trr = result.Trr;
                ResultsQrrTq2[QrrTqCounter].DCFactFallRate = result.DCFactFallRate;
                ResultsQrrTq2[QrrTqCounter].Tq = result.Tq;
            }

            if (state == DeviceState.Success)
            {
                List<DependencyObject> QrrTqItemContainer = GetQrrTqItemContainer();
                ContentPresenter presenter = FindVisualChild<ContentPresenter>(QrrTqItemContainer[QrrTqCounter]);

                //var profileArr = (Profile.TestParametersAndNormatives.OfType<Types.QrrTq.TestParameters>().ToArray())[QrrTqCounter];

                //OffStateVoltage
                Label labelMeasure = FindChild<Label>(presenter, "lbOffStateVoltage");
                if (labelMeasure != null)
                {
                    if (result.Mode == Types.QrrTq.TMode.QrrTq)
                        SetLabel(labelMeasure, state, false, result.OffStateVoltage.ToString(), false);
                }

                //OsvRate
                labelMeasure = FindChild<Label>(presenter, "lbOsvRate");
                if (labelMeasure != null)
                {
                    if (result.Mode == Types.QrrTq.TMode.QrrTq)
                        SetLabel(labelMeasure, state, false, result.OsvRate.ToString(), false);
                }

                //Idc
                labelMeasure = FindChild<Label>(presenter, "lbIdc");
                if (labelMeasure != null)
                    SetLabel(labelMeasure, state, false, result.Idc.ToString(), false);

                //Qrr
                labelMeasure = FindChild<Label>(presenter, "lbQrr");
                if (labelMeasure != null)
                {
                    if (result.Mode == Types.QrrTq.TMode.Qrr)
                        SetLabel(labelMeasure, state, true, result.Qrr.ToString());
                }

                //Irr
                labelMeasure = FindChild<Label>(presenter, "lbIrr");
                if (labelMeasure != null)
                {
                    if (result.Mode == Types.QrrTq.TMode.Qrr)
                        SetLabel(labelMeasure, state, true, result.Irr.ToString());
                }

                //Trr
                labelMeasure = FindChild<Label>(presenter, "lbTrr");
                if (labelMeasure != null)
                {
                    if (result.Mode == Types.QrrTq.TMode.Qrr)
                        SetLabel(labelMeasure, state, true, result.Trr.ToString());
                }

                labelMeasure = FindChild<Label>(presenter, "lbDCFactFallRate");
                if (labelMeasure != null)
                    SetLabel(labelMeasure, state, false, result.DCFactFallRate.ToString(), false);

                //Tq
                labelMeasure = FindChild<Label>(presenter, "lbTq");
                if (labelMeasure != null)
                {
                    if (result.Mode == Types.QrrTq.TMode.QrrTq)
                        SetLabel(labelMeasure, state, true, result.Tq.ToString());
                }
            }

            if (state == DeviceState.Success && Settings.Default.PlotUserQrrTq)
            {
                PlotX(m_CurrentPos, @"I", m_XRed.Color, result.CurrentData);
                PlotX(m_CurrentPos, @"V", m_XOrange.Color, result.VoltageData);
            }
        }

        internal void SetQrrTqProblem(ushort Problem)
        {
            var QrrTqItemContainer = GetQrrTqItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(QrrTqItemContainer[QrrTqCounter]);

            var label = FindChild<Label>(presenter, "lbProblem");

            if (label != null && label.Visibility != Visibility.Visible)
            {
                Types.QrrTq.HWProblemReason ProblemReason = (Types.QrrTq.HWProblemReason)Problem;
                label.Content = ProblemReason.ToString();

                label.Visibility = Visibility.Visible;

                label = FindChild<Label>(presenter, "lbTittleProblem");
                label.Visibility = Visibility.Visible;
            }

            IsRunning = false;
        }

        internal void SetQrrTqWarning(ushort Warning)
        {
            var QrrTqItemContainer = GetQrrTqItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(QrrTqItemContainer[QrrTqCounter]);

            var label = FindChild<Label>(presenter, "lbWarning");

            if (label != null && label.Visibility != Visibility.Visible)
            {
                Types.QrrTq.HWWarningReason WarningReason = (Types.QrrTq.HWWarningReason)Warning;

                label.Content = WarningReason.ToString();
                label.Visibility = Visibility.Visible;

                label = FindChild<Label>(presenter, "lbTittleWarning");
                label.Visibility = Visibility.Visible;
            }

            IsRunning = false;
        }

        internal void SetQrrTqFault(ushort Fault)
        {
            var QrrTqItemContainer = GetQrrTqItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(QrrTqItemContainer[QrrTqCounter]);

            var label = FindChild<Label>(presenter, "lbFaultReason");

            if (label != null && label.Visibility != Visibility.Visible)
            {
                Types.QrrTq.HWFaultReason FaultReason = (Types.QrrTq.HWFaultReason)Fault;

                label.Content = FaultReason.ToString();
                label.Visibility = Visibility.Visible;

                label = FindChild<Label>(presenter, "lbTittleFaultReason");
                label.Visibility = Visibility.Visible;
            }
        }

        internal void RefreshKindOfFreezing(ushort KindOfFreezing)
        {
            btnStart.Content = string.Format(Properties.Resources.Start + " ({0})", KindOfFreezing);
        }

        private int RACCounter;

        private List<DependencyObject> GetRACItemContainer()
        {
            var results = new List<DependencyObject>(7);
            ListView ListView = null;

            switch (m_CurrentPos)
            {
                case (1):
                    ListView = ListViewResults1;
                    break;

                default:
                    ListView = ListViewResults2;
                    break;
            }

            bool isNewlyRealized;

            for (int i = 0; i < ListView.Items.Count; i++)
            {
                if (ListView.Items[i] is Types.RAC.TestParameters)
                {
                    IItemContainerGenerator generator = ListView.ItemContainerGenerator;

                    var position = generator.GeneratorPositionFromIndex(i);
                    using (generator.StartAt(position, GeneratorDirection.Forward, true))
                    {
                        var child = generator.GenerateNext(out isNewlyRealized);
                        generator.PrepareItemContainer(child);
                        results.Add(child);
                    }
                }
            }

            return results;
        }

        internal void SetResultRAC(DeviceState state, Types.RAC.TestResults result)
        {
            m_StateRAC = state;

            if (m_StateRAC == DeviceState.InProcess)
                RACCounter++;

            if (m_CurrentPos == 1)
            {
                ResultsRAC1[RACCounter].ResultR = result.ResultR;
            }
            else
            {
                ResultsRAC2[RACCounter].ResultR = result.ResultR;
            }

            if (state == DeviceState.Success)
            {
                List<DependencyObject> RACItemContainer = GetRACItemContainer();
                ContentPresenter presenter = FindVisualChild<ContentPresenter>(RACItemContainer[RACCounter]);

                Label labelMeasure = FindChild<Label>(presenter, "lbResultR");

                if (labelMeasure != null)
                {
                    SetLabel(labelMeasure, state, true, result.ResultR.ToString());
                }
            }
        }

        internal void SetRACProblem(ushort Problem)
        {
            var RACItemContainer = GetRACItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(RACItemContainer[RACCounter]);

            var label = FindChild<Label>(presenter, "lbProblem");

            if (label != null && label.Visibility != Visibility.Visible)
            {
                Types.RAC.HWProblemReason ProblemReason = (Types.RAC.HWProblemReason)Problem;
                label.Content = ProblemReason.ToString();

                label.Visibility = Visibility.Visible;

                label = FindChild<Label>(presenter, "lbTittleProblem");
                label.Visibility = Visibility.Visible;
            }

            IsRunning = false;
        }

        internal void SetRACWarning(ushort Warning)
        {
            var RACItemContainer = GetRACItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(RACItemContainer[RACCounter]);

            var label = FindChild<Label>(presenter, "lbWarning");

            if (label != null && label.Visibility != Visibility.Visible)
            {
                Types.RAC.HWWarningReason WarningReason = (Types.RAC.HWWarningReason)Warning;

                label.Content = WarningReason.ToString();
                label.Visibility = Visibility.Visible;

                label = FindChild<Label>(presenter, "lbTittleWarning");
                label.Visibility = Visibility.Visible;
            }

            IsRunning = false;
        }

        internal void SetRACFault(ushort Fault)
        {
            var RACItemContainer = GetRACItemContainer();
            var presenter = FindVisualChild<ContentPresenter>(RACItemContainer[RACCounter]);

            var label = FindChild<Label>(presenter, "lbFaultReason");

            if (label != null && label.Visibility != Visibility.Visible)
            {
                Types.RAC.HWFaultReason FaultReason = (Types.RAC.HWFaultReason)Fault;

                label.Content = FaultReason.ToString();
                label.Visibility = Visibility.Visible;

                label = FindChild<Label>(presenter, "lbTittleFaultReason");
                label.Visibility = Visibility.Visible;
            }
        }

        public void SetSettingTemperature(int temeprature)
        {
            SettingTemperatureLabel.Content = temeprature;
        }

        public void SetTopTemp(int temeprature)
        {
            TopTempLabel.Content = temeprature;
            var bottomTemp = Profile.Temperature - 2;
            var topTemp = Profile.Temperature + 2;
            if (temeprature < bottomTemp || temeprature > topTemp)
            {
                TopTempLabel.Background = Brushes.Tomato;
            }
            else
            {
                TopTempLabel.Background = Brushes.LightGreen;
            }
        }

        public void SetBottomTemp(int temeprature)
        {
            BotTempLabel.Content = temeprature;
            var bottomTemp = Profile.Temperature - 2;
            var topTemp = Profile.Temperature + 2;
            if (temeprature < bottomTemp || temeprature > topTemp)
            {
                BotTempLabel.Background = Brushes.Tomato;
            }
            else
            {
                BotTempLabel.Background = Brushes.LightGreen;
            }
        }

        private static void SetLabel(ContentControl Target, DeviceState State, bool IsFitWithNormatives, string Value, bool UseIsFitWithNormatives = true)
        {
            switch (State)
            {
                case DeviceState.InProcess:
                    Target.Background = Brushes.Gold;
                    break;
                case DeviceState.Stopped:
                    Target.Content = Properties.Resources.Stopped;
                    Target.Background = Brushes.LightGreen;
                    break;
                case DeviceState.Success:
                    Target.Content = Value;
                    Target.Background = UseIsFitWithNormatives ? (IsFitWithNormatives ? Brushes.LightGreen : Brushes.LightPink) : Brushes.Transparent;
                    break;
                case DeviceState.Problem:
                    Target.Content = Value;
                    Target.Background = Brushes.Gold;
                    break;
                case DeviceState.Fault:
                    Target.Content = Properties.Resources.Fault;
                    Target.Background = Brushes.Tomato;
                    break;
                case DeviceState.None:
                    Target.Content = "";
                    Target.Background = Brushes.Transparent;
                    break;
            }
        }

        private static void ResetLabel(ContentControl Target)
        {
            Target.Content = "";
            Target.Background = Brushes.Transparent;
        }

        #endregion

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                childItem childOfChild = FindVisualChild<childItem>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
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

        private void ClearStatus(bool Position1, bool Position2)
        {
            if (Position1)
            {
                for (int i = 0; i < ListViewResults1.Items.Count; i++)
                {
                    var element = (ListViewResults1.ItemContainerGenerator.ContainerFromIndex(i));
                    if (element != null)
                    {
                        ListViewResults1.UpdateLayout();

                        if (ListViewResults1.Items[i] is Types.Gate.TestParameters)
                        {
                            ClearResultsGate(element);
                            continue;
                        }

                        if (ListViewResults1.Items[i] is Types.SL.TestParameters)
                        {
                            ClearVtmResults(element);
                            continue;
                        }

                        if (ListViewResults1.Items[i] is Types.BVT.TestParameters)
                        {
                            ClearBvtResults(element);
                        }

                        if (ListViewResults1.Items[i] is Types.dVdt.TestParameters)
                        {
                            ClearResultsDvDt(element);
                        }

                        if (ListViewResults1.Items[i] is Types.ATU.TestParameters)
                        {
                            ClearResultsATU(element);
                        }

                        if (ListViewResults1.Items[i] is Types.QrrTq.TestParameters)
                        {
                            ClearResultsQrrTq(element);
                        }

                        if (ListViewResults1.Items[i] is Types.RAC.TestParameters)
                        {
                            ClearResultsRAC(element);
                        }
                    }
                }

                ResetLabel(labelHeightResult1);
                chartPlotter1.Children.RemoveAll(typeof(LineGraph));
            }

            if (Position2)
            {
                for (int i = 0; i < ListViewResults2.Items.Count; i++)
                {
                    var element = (ListViewResults2.ItemContainerGenerator.ContainerFromIndex(i));
                    if (element != null)
                    {
                        ListViewResults2.UpdateLayout();
                        if (ListViewResults2.Items[i] is Types.Gate.TestParameters)
                        {
                            ClearResultsGate(element);
                            continue;
                        }
                        if (ListViewResults2.Items[i] is Types.SL.TestParameters)
                        {
                            ClearVtmResults(element);
                            continue;
                        }
                        if (ListViewResults2.Items[i] is Types.BVT.TestParameters)
                        {
                            ClearBvtResults(element);
                        }
                        if (ListViewResults2.Items[i] is Types.dVdt.TestParameters)
                        {
                            ClearResultsDvDt(element);
                        }

                        if (ListViewResults2.Items[i] is Types.ATU.TestParameters)
                        {
                            ClearResultsATU(element);
                        }

                        if (ListViewResults2.Items[i] is Types.QrrTq.TestParameters)
                        {
                            ClearResultsQrrTq(element);
                        }

                        if (ListViewResults2.Items[i] is Types.RAC.TestParameters)
                        {
                            ClearResultsRAC(element);
                        }
                    }
                }

                ResetLabel(labelHeightResult2);
                chartPlotter2.Children.RemoveAll(typeof(LineGraph));
            }
        }

        private void ClearBvtResults(DependencyObject element)
        {
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(element);

            var labelBvtWarning1 = FindChild<Label>(presenter, "labelBvtWarning1");
            if (labelBvtWarning1 != null)
            {
                labelBvtWarning1.Visibility = Visibility.Collapsed;
                ResetLabel(labelBvtWarning1);
            }

            var labelBvtFault1 = FindChild<Label>(presenter, "labelBvtFault1");
            if (labelBvtFault1 != null)
            {
                labelBvtFault1.Visibility = Visibility.Collapsed;
                ResetLabel(labelBvtFault1);
            }

            var labelBvtVdrmResult1 = FindChild<Label>(presenter, "labelBvtVdrmResult1");
            if (labelBvtVdrmResult1 != null)
                ResetLabel(labelBvtVdrmResult1);

            var labelBvtVrrmResult1 = FindChild<Label>(presenter, "labelBvtVrrmResult1");
            if (labelBvtVrrmResult1 != null)
                ResetLabel(labelBvtVrrmResult1);

            var labelBvtIdrmResult1 = FindChild<Label>(presenter, "labelBvtIdrmResult1");
            if (labelBvtIdrmResult1 != null)
                ResetLabel(labelBvtIdrmResult1);

            var labelBvtIrrmResult1 = FindChild<Label>(presenter, "labelBvtIrrmResult1");
            if (labelBvtIrrmResult1 != null)
                ResetLabel(labelBvtIrrmResult1);


            var labelBvtVdsmResult1 = FindChild<Label>(presenter, "labelBvtVdsmResult1");
            if (labelBvtVdsmResult1 != null)
                ResetLabel(labelBvtVdsmResult1);

            var labelBvtVrsmResult1 = FindChild<Label>(presenter, "labelBvtVrsmResult1");
            if (labelBvtVrsmResult1 != null)
                ResetLabel(labelBvtVrsmResult1);

            var labelBvtIdsmResult1 = FindChild<Label>(presenter, "labelBvtIdsmResult1");
            if (labelBvtIdsmResult1 != null)
                ResetLabel(labelBvtIdsmResult1);

            var labelBvtIrsmResult1 = FindChild<Label>(presenter, "labelBvtIrsmResult1");
            if (labelBvtIrsmResult1 != null)
                ResetLabel(labelBvtIrsmResult1);
        }

        private void ClearVtmResults(DependencyObject element)
        {
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(element);

            var labelVtmWarning1 = FindChild<Label>(presenter, "labelVtmWarning1");
            if (labelVtmWarning1 != null)
                labelVtmWarning1.Visibility = Visibility.Collapsed;

            var labelVtmFault1 = FindChild<Label>(presenter, "labelVtmFault1");
            if (labelVtmFault1 != null)
                labelVtmFault1.Visibility = Visibility.Collapsed;

            var labelVtmResult1 = FindChild<Label>(presenter, "labelVtmResult1");
            if (labelVtmResult1 != null)
                ResetLabel(labelVtmResult1);

            var labelItmResult1 = FindChild<Label>(presenter, "labelItmResult1");
            if (labelItmResult1 != null)
                ResetLabel(labelItmResult1);
        }

        private void ClearResultsGate(DependencyObject element)
        {
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(element);

            var labelWarning = FindChild<Label>(presenter, "labelGateWarning1");
            if (labelWarning != null)
                labelWarning.Visibility = Visibility.Collapsed;

            var labelFault = FindChild<Label>(presenter, "labelGateFault1");
            if (labelWarning != null)
                labelFault.Visibility = Visibility.Collapsed;

            var labelKelvinResult1 = FindChild<Label>(presenter, "labelKelvinResult1");
            if (labelKelvinResult1 != null)
                ResetLabel(labelKelvinResult1);

            var labelRgResult1 = FindChild<Label>(presenter, "labelRgResult1");
            if (labelRgResult1 != null)
                ResetLabel(labelRgResult1);

            var labelIgtResult1 = FindChild<Label>(presenter, "labelIgtResult1");
            if (labelIgtResult1 != null)
                ResetLabel(labelIgtResult1);

            var labelVgtResult1 = FindChild<Label>(presenter, "labelVgtResult1");
            if (labelVgtResult1 != null)
                ResetLabel(labelVgtResult1);

            var labelIhResult1 = FindChild<Label>(presenter, "labelIhResult1");
            if (labelIhResult1 != null)
                ResetLabel(labelIhResult1);

            var labelIlResult1 = FindChild<Label>(presenter, "labelIlResult1");
            if (labelIlResult1 != null)
                ResetLabel(labelIlResult1);

        }

        private void ClearResultsDvDt(DependencyObject element)
        {
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(element);

            var labelWarning = FindChild<Label>(presenter, "labelGateWarning");
            if (labelWarning != null)
                labelWarning.Visibility = Visibility.Collapsed;

            var labelFault = FindChild<Label>(presenter, "labelGateFault");
            if (labelWarning != null)
                labelFault.Visibility = Visibility.Collapsed;

            var labelResult = FindChild<Label>(presenter, "labelResult");
            if (labelResult != null)
                ResetLabel(labelResult);

        }

        private void ClearResultsATU(DependencyObject element)
        {
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(element);

            //чистим результаты измерений
            var label = FindChild<Label>(presenter, "lbAtuUBR");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbAtuUPRSM");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbAtuIPRSM");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbAtuPRSM");
            if (label != null) ResetLabel(label);

            //чистим Warning и Fault
            label = FindChild<Label>(presenter, "lbAtuWarning");
            if (label != null) label.Visibility = Visibility.Collapsed;

            label = FindChild<Label>(presenter, "lbAtuFaultReason");
            if (label != null) label.Visibility = Visibility.Collapsed;
        }

        private void CalcDeviceClass(CustomControl.ValidatingTextBox sourceOfdeviceCode, bool factClass)
        {
            //вычисляет класс изделия и выводит его на форму
            //factClass - флаг, позволяющий различить условия вызова данной реализации:
            // true - требуется вычислить класс изделия по факту только что выполненного измерения
            // false - требуется вычислить класс изделия при RT перед предстоящим измерением

            //чтобы исключить зависимость работоспособности приложения от работоспособности данной реализации заворачиваем её в try...catch
            try
            {
                if (sourceOfdeviceCode != null)
                {
                    //чтобы система смогла вычислить класс изделия надо чтобы обозначение изделия было введено полностью - пример 2/4-00020997
                    //для этого будем проверять количество символов после символа '/' и если оно равно 10 - будем пытаться вычислить класс этого изделия
                    string deviceCode = sourceOfdeviceCode.Text;
                    int jobFirstSimbol = deviceCode.IndexOf("/", 0);

                    if (jobFirstSimbol != -1)
                    {
                        jobFirstSimbol++;
                        string job = deviceCode.Substring(jobFirstSimbol);

                        if (job.Length == 10)
                        {
                            string sDeviceClass = string.Empty;
                            string descr = string.Empty;
                            int? deviceClass = null;

                            //пробуем вычислить значение класса
                            switch (factClass)
                            {
                                case true:
                                    deviceClass = Cache.Net.ReadDeviceClass(deviceCode, Profile.Name);
                                    descr = Properties.Resources.DeviceClass;
                                    break;

                                default:
                                    deviceClass = Cache.Net.ReadDeviceRTClass(deviceCode, Profile.Name);
                                    descr = Properties.Resources.DeviceRTClass;
                                    break;
                            }

                            switch (deviceClass == null)
                            {
                                case true:
                                    //отсутствуют измерения на основе которых можно вычислить класс изделия
                                    sDeviceClass = Properties.Resources.NoResults;
                                    break;

                                default:
                                    {
                                        //класс не null - какое-то значение получено, разбираемся что получено
                                        int iDeviceClass = (int)deviceClass;

                                        switch (iDeviceClass)
                                        {
                                            case (-1):
                                                //случай ошибки в реализации вычисления класса
                                                sDeviceClass = Properties.Resources.ErrorRealisation;
                                                break;

                                            default:
                                                //получено вменяемое значение класса изделия
                                                sDeviceClass = iDeviceClass.ToString();
                                                int iClassByProfileName = (int)m_ClassByProfileName;

                                                lblDeviceClass.Foreground = (iDeviceClass >= iClassByProfileName) ? lblDeviceClass.Foreground = Brushes.Black : lblDeviceClass.Foreground = Brushes.Red;

                                                if (!factClass)
                                                {
                                                    //смотрим на вычисленное значение класса по выбранному наименованию профиля                                               
                                                    if (m_ClassByProfileName == null)
                                                    {
                                                        //по обозначению профиля значение класса не вычислено
                                                        btnStart.IsEnabled = true;
                                                    }
                                                    else
                                                    {
                                                        //по обозначению профиля вычислено не null значение класса, сравниваем его с iDeviceClass. если RT класс изделия не соответствует (меньше), чем класс выбранного профиля. оператору надо выбрать правильный профиль, на текущем выбранном профиле выполнение теста не имеет смысла - будет всегда брак
                                                        btnStart.IsEnabled = (iDeviceClass >= iClassByProfileName) ? true : false;
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }

                            //выводим класс на форму
                            lblDeviceClass.Content = string.Format("{0}: {1}", descr, sDeviceClass);
                        }
                        else btnStart.IsEnabled = true;
                    }
                }
            }
            catch
            {
                lblDeviceClass.Content = string.Format("{0}: {1}", Properties.Resources.DeviceRTClass, Properties.Resources.ErrorRealisation);
            }
        }

        private void tbPsdSerialNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            CustomControl.ValidatingTextBox tb = sender as CustomControl.ValidatingTextBox;
            CalcDeviceClass(tb, false);
        }

        private void delme_Click(object sender, RoutedEventArgs e)
        {
            CustomControl.ValidatingTextBox tbNumber = null;

            switch (ProfileRoutines.CalcSubjectForMeasure(this.Profile.Name))
            {
                case SubjectForMeasure.PSD:
                    tbNumber = tbPsdSerialNumber;
                    break;

                case SubjectForMeasure.PSE:
                    tbNumber = tbPseNumber;
                    break;

                default:
                    tbNumber = null;
                    break;
            }

            CalcDeviceClass(tbNumber, true);
        }

        private void tbPseNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            CustomControl.ValidatingTextBox tb = sender as CustomControl.ValidatingTextBox;
            CalcDeviceClass(tb, false);
        }

        private void ClearResultsQrrTq(DependencyObject element)
        {
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(element);

            //чистим результаты измерений
            var label = FindChild<Label>(presenter, "lbIdc");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbQrr");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbIrr");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbTrr");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbDCFactFallRate");
            if (label != null) ResetLabel(label);

            label = FindChild<Label>(presenter, "lbTq");
            if (label != null) ResetLabel(label);

            //чистим Warning, Fault и Problem
            label = FindChild<Label>(presenter, "lbTittleWarning");
            if (label != null) label.Visibility = Visibility.Collapsed;
            label = FindChild<Label>(presenter, "lbWarning");
            if (label != null) label.Visibility = Visibility.Collapsed;

            label = FindChild<Label>(presenter, "lbTittleFaultReason");
            if (label != null) label.Visibility = Visibility.Collapsed;
            label = FindChild<Label>(presenter, "lbFaultReason");
            if (label != null) label.Visibility = Visibility.Collapsed;

            label = FindChild<Label>(presenter, "lbTittleProblem");
            if (label != null) label.Visibility = Visibility.Collapsed;
            label = FindChild<Label>(presenter, "lbProblem");
            if (label != null) label.Visibility = Visibility.Collapsed;
        }

        private void ClearResultsRAC(DependencyObject element)
        {
            ContentPresenter presenter = FindVisualChild<ContentPresenter>(element);

            //чистим результаты измерений
            var label = FindChild<Label>(presenter, "lbResultR");
            if (label != null) ResetLabel(label);

            //чистим Warning, Fault и Problem
            label = FindChild<Label>(presenter, "lbTittleWarning");
            if (label != null) label.Visibility = Visibility.Collapsed;
            label = FindChild<Label>(presenter, "lbWarning");
            if (label != null) label.Visibility = Visibility.Collapsed;

            label = FindChild<Label>(presenter, "lbTittleFaultReason");
            if (label != null) label.Visibility = Visibility.Collapsed;
            label = FindChild<Label>(presenter, "lbFaultReason");
            if (label != null) label.Visibility = Visibility.Collapsed;

            label = FindChild<Label>(presenter, "lbTittleProblem");
            if (label != null) label.Visibility = Visibility.Collapsed;
            label = FindChild<Label>(presenter, "lbProblem");
            if (label != null) label.Visibility = Visibility.Collapsed;
        }

        private bool IsRunning
        {
            get { return m_IsRunning; }

            set
            {
                m_IsRunning = value;
                btnBack.IsEnabled = !value;
                btnStart.IsEnabled = !value;
                tbPsdJob.IsEnabled = !value;
                tbPsdSerialNumber.IsEnabled = !value;
                tbPseNumber.IsEnabled = !value;
            }
        }

        private void PlotX(int Position, string LineName, Color LineColor, IEnumerable<short> UPoints)
        {
            var points = UPoints.Select((T, I) => new PointF(I, T)).ToList();
            var dataSource = new EnumerableDataSource<PointF>(points);

            dataSource.SetXMapping(P => P.X);
            dataSource.SetYMapping(P => P.Y);

            ((Position == 1) ? chartPlotter1 : chartPlotter2).AddLineGraph(dataSource, LineColor, 3, LineName);
            ((Position == 1) ? chartPlotter1 : chartPlotter2).FitToView();
        }

        private void PlotYX(int Position, string LineName, Color LineColor, ICollection<short> UxPoints,
                            IEnumerable<short> UyPoints)
        {
            var crop = UxPoints.Count - DATA_LENGTH;

            var dataI = UxPoints.Skip(crop).Select(I => (Math.Abs(I) <= 2 ? (short)0 : I));
            var dataV = UyPoints.Skip(crop);

            if (dataI.Any() && (dataI.Min() < -7 * 10 || dataI.Max() > 7 * 10) || wasCurrentMore)
            {
                wasCurrentMore = true;
                chartPlotter1.Viewport.Restrictions.Clear();
            }
            else
            {
                chartPlotter1.Viewport.Restrictions.Clear();
                var restr = new ViewportAxesRangeRestriction { YRange = new DisplayRange(-7, 7) };
                chartPlotter1.Viewport.Restrictions.Add(restr);
            }

            var points =
                dataI.Zip(dataV, (I, V) => new PointF(V, I / 10.0f))
                     .Select((P => (Math.Abs(P.X) < 200 ? new PointF(P.X, 0) : P)));

            var dataSource = new EnumerableDataSource<PointF>(points);
            dataSource.SetXMapping(P => P.X);
            dataSource.SetYMapping(P => P.Y);

            ((Position == 1) ? chartPlotter1 : chartPlotter2).AddLineGraph(dataSource, LineColor, 3);

            var scalerSource = new EnumerableDataSource<PointF>(new[] { new PointF(0, 5), new PointF(0, -5) });
            scalerSource.SetXMapping(P => P.X);
            scalerSource.SetYMapping(P => P.Y);

            ((Position == 1) ? chartPlotter1 : chartPlotter2).AddLineGraph(scalerSource, Brushes.Transparent.Color);

            ((Position == 1) ? chartPlotter1 : chartPlotter2).LegendVisible = false;
            ((Position == 1) ? chartPlotter1 : chartPlotter2).FitToView();
        }

        private void StartInternal(int Position, Types.Gate.TestParameters ParamsGate,
                                   Types.SL.TestParameters ParamsVTM,
                                   Types.BVT.TestParameters ParamsBVT, Types.QrrTq.TestParameters ParamsQrrTq, Types.RAC.TestParameters ParamsRAC, Types.IH.TestParameters ParamsIH, Types.RCC.TestParameters ParamsRCC, Types.Commutation.TestParameters ParamsComm, Types.Clamping.TestParameters ParamsClamp, Types.ATU.TestParameters ParamsATU)
        {
            if (this.Profile != null)
            {
                switch (ProfileRoutines.CalcSubjectForMeasure(this.Profile.Name))
                {
                    case SubjectForMeasure.PSD:
                        tbPsdJob.BorderBrush = String.IsNullOrWhiteSpace(tbPsdJob.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPsdJob.Text = tbPsdJob.Text.Replace(',', '.');

                        tbPsdSerialNumber.BorderBrush = String.IsNullOrWhiteSpace(tbPsdSerialNumber.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPsdSerialNumber.Text = tbPsdSerialNumber.Text.Replace(',', '.');
                        break;

                    case SubjectForMeasure.PSE:
                        tbPseJob.BorderBrush = String.IsNullOrWhiteSpace(tbPseJob.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPseJob.Text = tbPseJob.Text.Replace(',', '.');

                        tbPseNumber.BorderBrush = String.IsNullOrWhiteSpace(tbPseNumber.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPseNumber.Text = tbPseNumber.Text.Replace(',', '.');
                        break;
                }
            }

            if (!Cache.Net.Start(ParamsGate, ParamsVTM, ParamsBVT, ParamsATU, ParamsQrrTq, ParamsRAC, ParamsIH, ParamsRCC, ParamsComm, ParamsClamp))
                return;

            ClearStatus(Position == 1, true);
            IsRunning = true;
        }

        private void Start_Click(object Sender, RoutedEventArgs E)
        {
            wasCurrentMore = false;
            //Cache.Net.StopMeasuringTemp();
            StartFirst();
        }

        internal void StartFirst()
        {
            if (IsRunning)
                return;

            m_Errors1.Clear();
            m_Errors2.Clear();

            m_CurrentPos = 1;
            /*
            было до 21.05.2019           
            m_TwoPosRequested =
                            !(m_Profile.ParametersComm == Types.Commutation.ModuleCommutationType.MT1 ||   
                              m_Profile.ParametersComm == Types.Commutation.ModuleCommutationType.MD1 ||   
                              m_Profile.ParametersComm == Types.Commutation.ModuleCommutationType.Direct); 
            */
            m_TwoPosRequested =
                !(Profile.ParametersComm == Types.Commutation.ModuleCommutationType.MT1 ||
                  Profile.ParametersComm == Types.Commutation.ModuleCommutationType.MD1 ||
                  Profile.ParametersComm == Types.Commutation.ModuleCommutationType.Direct);

            StartN(1);
        }

        private void StartN(int Position)
        {
            var options = ConverterUtil.MapCommutationType(Profile.ParametersComm, Position);

            m_ResultsGate1 = new List<TestResults>();
            m_ResultsVTM1 = new List<Types.SL.TestResults>();
            m_ResultsBVT1 = new List<Types.BVT.TestResults>();
            _dvdTestResults1 = new List<Types.dVdt.TestResults>();
            m_ResultsATU1 = new List<Types.ATU.TestResults>();
            m_ResultsQrrTq1 = new List<Types.QrrTq.TestResults>();
            m_ResultsRAC1 = new List<Types.RAC.TestResults>();

            m_ResultsGate2 = new List<TestResults>();
            m_ResultsVTM2 = new List<Types.SL.TestResults>();
            m_ResultsBVT2 = new List<Types.BVT.TestResults>();
            _dvdTestResults2 = new List<Types.dVdt.TestResults>();
            m_ResultsATU2 = new List<Types.ATU.TestResults>();
            m_ResultsQrrTq2 = new List<Types.QrrTq.TestResults>();
            m_ResultsRAC2 = new List<Types.RAC.TestResults>();

            _gateCounter = -1;
            slCounter = -1;
            bvtCounter = -1;
            dvdtCounter = -1;
            ATUCounter = -1;
            QrrTqCounter = -1;
            RACCounter = -1;

            var parameters = Profile.TestParametersAndNormatives.ToList();

            foreach (var baseTestParametersAndNormativese in parameters)
            {
                var parGate = baseTestParametersAndNormativese as Types.Gate.TestParameters;
                if (parGate != null)
                {
                    m_ResultsGate1.Add(new TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    m_ResultsGate2.Add(new TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    parGate.IsEnabled &= options.Item3;
                    continue;
                }

                var parVtm = baseTestParametersAndNormativese as Types.SL.TestParameters;
                if (parVtm != null)
                {
                    m_ResultsVTM1.Add(new Types.SL.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    m_ResultsVTM2.Add(new Types.SL.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    parVtm.UseLsqMethod = Settings.Default.UseVTMPostProcessing;
                    continue;
                }

                var parBvt = baseTestParametersAndNormativese as Types.BVT.TestParameters;
                if (parBvt != null)
                {
                    m_ResultsBVT1.Add(new Types.BVT.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    m_ResultsBVT2.Add(new Types.BVT.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    switch (parBvt.TestType)
                    {
                        case Types.BVT.BVTTestType.Both:
                            parBvt.TestType = options.Item2;
                            break;
                        case Types.BVT.BVTTestType.Direct:
                            if (options.Item2 == Types.BVT.BVTTestType.Reverse)
                                parBvt.IsEnabled = false;
                            break;
                    }

                    //запоминаем значение вычисленного класса по наименованию профиля (см. UserTestPage_OnLoaded), чтобы при выполнении теста UDSM/URSM по нему вычислить значение напряжения при котором проводится тест UDSM/URSM
                    Profile.ParametersBVT.ClassByProfileName = m_ClassByProfileName;
                    parBvt.ClassByProfileName = m_ClassByProfileName;
                    continue;
                }

                var parDvDt = baseTestParametersAndNormativese as Types.dVdt.TestParameters;
                if (parDvDt != null)
                {
                    _dvdTestResults1.Add(new Types.dVdt.TestResults() { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    _dvdTestResults2.Add(new Types.dVdt.TestResults() { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    continue;
                }

                var parATU = baseTestParametersAndNormativese as Types.ATU.TestParameters;
                if (parATU != null)
                {
                    m_ResultsATU1.Add(new Types.ATU.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    m_ResultsATU2.Add(new Types.ATU.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    continue;
                }

                var parQrrTq = baseTestParametersAndNormativese as Types.QrrTq.TestParameters;
                if (parQrrTq != null)
                {
                    m_ResultsQrrTq1.Add(new Types.QrrTq.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    m_ResultsQrrTq2.Add(new Types.QrrTq.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    continue;
                }

                var parRAC = baseTestParametersAndNormativese as Types.RAC.TestParameters;
                if (parRAC != null)
                {
                    m_ResultsRAC1.Add(new Types.RAC.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    m_ResultsRAC2.Add(new Types.RAC.TestResults { TestTypeId = baseTestParametersAndNormativese.TestTypeId });
                    continue;
                }
            }

            var paramsComm = new Types.Commutation.TestParameters
            {
                BlockIndex = (!Cache.Clamp.UseTmax) ? Types.Commutation.HWBlockIndex.Block1 : Types.Commutation.HWBlockIndex.Block2,

                //было до 21.05.2019 CommutationType = ConverterUtil.MapCommutationType(m_Profile.ParametersComm),
                CommutationType = ConverterUtil.MapCommutationType(Profile.ParametersComm),

                Position =
                        (Position == 1)
                            ? Types.Commutation.HWModulePosition.Position1
                            : Types.Commutation.HWModulePosition.Position2
            };

            var paramsClamp = new Types.Clamping.TestParameters
            {
                StandardForce = Types.Clamping.ClampingForceInternal.Custom,
                CustomForce = Profile.ParametersClamp,
                Height = (ushort)Profile.Height,
                IsHeightMeasureEnabled = Profile.IsHeightMeasureEnabled,
                SkipClamping = false
            };

            StartInternal(Position, paramsComm, paramsClamp, parameters);
        }

        private void StartInternal(int position, TestParameters paramsComm, Types.Clamping.TestParameters paramsClamp, List<BaseTestParametersAndNormatives> parameters)
        {
            if (this.Profile != null)
            {
                switch (ProfileRoutines.CalcSubjectForMeasure(this.Profile.Name))
                {
                    case SubjectForMeasure.PSD:
                        tbPsdJob.BorderBrush = String.IsNullOrWhiteSpace(tbPsdJob.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPsdJob.Text = tbPsdJob.Text.Replace(',', '.');

                        tbPsdSerialNumber.BorderBrush = String.IsNullOrWhiteSpace(tbPsdSerialNumber.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPsdSerialNumber.Text = tbPsdSerialNumber.Text.Replace(',', '.');
                        break;

                    case SubjectForMeasure.PSE:
                        tbPseJob.BorderBrush = String.IsNullOrWhiteSpace(tbPseJob.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPseJob.Text = tbPseJob.Text.Replace(',', '.');

                        tbPseNumber.BorderBrush = String.IsNullOrWhiteSpace(tbPseNumber.Text) ? Brushes.Tomato : m_TbBrush;
                        tbPseNumber.Text = tbPseNumber.Text.Replace(',', '.');
                        break;
                }
            }

            if (!Cache.Net.Start(paramsComm, paramsClamp, parameters))
                return;

            ClearStatus(position == 1, true);
            IsRunning = true;
        }

        private void Stop_Click(object Sender, RoutedEventArgs E)
        {
            Cache.Net.StopByButtonStop();
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
            {
                OnLeaveNotify();

                switch (Cache.WorkMode)
                {
                    //в режиме наладчика и в режиме специальных измерений возвращаемся к редактированию профиля, иначе к выбору профиля без возможности редактирования
                    case (UserWorkMode.ServiceMan):
                    case (UserWorkMode.SpecialMeasure):
                        NavigationService.Navigate(Cache.ProfileEdit);
                        break;

                    default:
                        NavigationService.Navigate(Cache.ProfileSelection);
                        break;
                }
            }
        }

        internal void OnLeaveNotify()
        {
            Cache.Net.StopHeating();
        }

        private void UserTestPage_OnLoaded(object Sender, RoutedEventArgs E)
        {
            //вычисляем класс по выбранному пользователем профилю
            m_ClassByProfileName = ProfileRoutines.DeviceClassByProfileName(Profile?.Name);

            tbPsdJob.Text = "";
            tbPsdSerialNumber.Text = "";
            tbPseNumber.Text = "";
            tbPseJob.Text = "";

            ClearStatus(true, true);
            lblDeviceClass.Content = string.Empty;

            //в режиме специальных измерений пользователь не должен вводить идентификационную информацию, поле для вывода предупреждений по поводу не заполненных полей для ввода идентификационной информации становится не нужным
            if (Cache.WorkMode == UserWorkMode.SpecialMeasure)
            {
                lbPsdJob.Visibility = Visibility.Collapsed;
                tbPsdJob.Visibility = Visibility.Collapsed;

                lbPsdSerialNumber.Visibility = Visibility.Collapsed;
                tbPsdSerialNumber.Visibility = Visibility.Collapsed;

                lbPseNumber.Visibility = Visibility.Collapsed;
                tbPseNumber.Visibility = Visibility.Collapsed;

                lbPseJob.Visibility = Visibility.Collapsed;
                tbPseJob.Visibility = Visibility.Collapsed;
            }
            else
            {
                //случай режима отличного от 'режим специальных измерений'. поля для ввода идентификационных данных должны быть введены, поэтому делаем их видимыми
                Types.SubjectForMeasure subjectForMeasure = ProfileRoutines.CalcSubjectForMeasure(this.Profile.Name);

                switch (subjectForMeasure)
                {
                    case Types.SubjectForMeasure.PSD:
                        {
                            //видны
                            lbPsdJob.Visibility = Visibility.Visible;
                            tbPsdJob.Visibility = Visibility.Visible;

                            lbPsdSerialNumber.Visibility = Visibility.Visible;
                            tbPsdSerialNumber.Visibility = Visibility.Visible;

                            //не видны
                            lbPseNumber.Visibility = Visibility.Collapsed;
                            tbPseNumber.Visibility = Visibility.Collapsed;

                            lbPseJob.Visibility = Visibility.Collapsed;
                            tbPseJob.Visibility = Visibility.Collapsed;

                            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,
                                                   new Action(delegate ()
                                                   {
                                                       tbPsdJob.Focus();
                                                   }));

                            break;
                        }

                    case Types.SubjectForMeasure.PSE:
                        {
                            lbPseNumber.Visibility = Visibility.Visible;
                            tbPseNumber.Visibility = Visibility.Visible;

                            lbPseJob.Visibility = Visibility.Visible;
                            tbPseJob.Visibility = Visibility.Visible;

                            //не видны
                            lbPsdJob.Visibility = Visibility.Collapsed;
                            tbPsdJob.Visibility = Visibility.Collapsed;

                            lbPsdSerialNumber.Visibility = Visibility.Collapsed;
                            tbPsdSerialNumber.Visibility = Visibility.Collapsed;

                            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,
                                                   new Action(delegate ()
                                                   {
                                                       tbPseJob.Focus();
                                                   }));

                            break;
                        }
                }
            }

            Cache.Main.TopTitle = Properties.Resources.UserTestPage_Title + ", " + Properties.Resources.Profile.ToLower() + ": " + "\n" + Profile.Name;
        }

        private void UserTestPage_PreviewKeyDown(object Sender, KeyEventArgs E)
        {
            var uie = E.OriginalSource as UIElement;

            if (E.Key == Key.Enter)
            {
                E.Handled = true;
                if (uie != null)
                    uie.MoveFocus(
                        new TraversalRequest(
                            FocusNavigationDirection.Next));
            }
        }

        #region INotifyPropertyChangedImplementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string PropertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion
    }

    public class MultiIdentificationFieldsToTextConverter : IMultiValueConverter
    {
        public object Convert(object[] Values, Type TargetType, object Parameter, CultureInfo Culture)
        {
            string profileName = Values[0]?.ToString();
            string psdJob = Values[1]?.ToString();
            string psdSerialNumber = Values[2]?.ToString();
            string pseJob = Values[3]?.ToString();
            string pseNumber = Values[4]?.ToString();

            bool specialMeasureMode;
            bool parseResult = bool.TryParse(Values[5]?.ToString(), out specialMeasureMode);
            if (!parseResult)
                return "Flag 'specialMeasureMode' parsing error!";

            SubjectForMeasure subjectForMeasure = ProfileRoutines.CalcSubjectForMeasure(profileName);

            if (specialMeasureMode)
            {
                //в режиме специальных измерений нет смысла показывать предупреждения о не заполненных полях
                return null;
            }
            else
            {
                switch (subjectForMeasure)
                {
                    case SubjectForMeasure.PSD:
                        if (!string.IsNullOrEmpty(psdJob) && !string.IsNullOrEmpty(psdSerialNumber))
                            return null;
                        else
                            return Properties.Resources.ResultsWillNotBeSaved;

                    case SubjectForMeasure.PSE:
                        if (!string.IsNullOrEmpty(pseJob) && !string.IsNullOrEmpty(pseNumber))
                            return null;
                        else
                            return Properties.Resources.ResultsWillNotBeSaved;
                }
            }

            return Properties.Resources.ResultsWillNotBeSaved;
        }

        public object[] ConvertBack(object Value, Type[] TargetTypes, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in MultiIdentificationFieldsToVisibilityConverter");
        }
    }
}