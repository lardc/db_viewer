using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using SCME.Types;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for CalibrationPage.xaml
    /// </summary>
    public partial class CalibrationPage : INotifyPropertyChanged
    {
        private Types.Gate.CalibrationParameters m_ParamsGate;
        private Types.SL.CalibrationParameters m_ParamsVtm;
        private Types.BVT.CalibrationParams m_ParamsBvt;
        private Types.Clamping.CalibrationParams m_ParamsClamping;
        private Types.dVdt.CalibrationParams m_ParamsdVdt;
        private int m_Index;

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String Info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Info));
        }

        #endregion

        #region Bounded properties

        public Types.Gate.CalibrationParameters ParamsGate
        {
            get { return m_ParamsGate; }
            set
            {
                m_ParamsGate = value;
                NotifyPropertyChanged("ParamsGate");
            }
        }


        public Types.SL.CalibrationParameters ParamsVtm
        {
            get { return m_ParamsVtm; }
            set
            {
                m_ParamsVtm = value;
                NotifyPropertyChanged("ParamsVtm");
            }
        }


        public Types.BVT.CalibrationParams ParamsBvt
        {
            get { return m_ParamsBvt; }
            set
            {
                m_ParamsBvt = value;
                NotifyPropertyChanged("ParamsBvt");
            }
        }

        public Types.Clamping.CalibrationParams ParamsClamping
        {
            get { return m_ParamsClamping; }
            set
            {
                m_ParamsClamping = value;
                NotifyPropertyChanged("ParamsClamping");
            }
        }

        public Types.dVdt.CalibrationParams ParamsdVdt
        {
            get { return m_ParamsdVdt; }
            set
            {
                m_ParamsdVdt = value;
                NotifyPropertyChanged("ParamsdVdt");
            }
        }

        #endregion

        public string ErrorGate { get; set; }
        public string ErrorVtm { get; set; }
        public string ErrorBvt { get; set; }
        public string ErrorClamping { get; set; }
        public string ErrordVdt { get; set; }

        public CalibrationPage()
        {
            InitializeComponent();

            ParamsGate = new Types.Gate.CalibrationParameters();
            ParamsVtm = new Types.SL.CalibrationParameters();
            ParamsBvt = new Types.BVT.CalibrationParams();
            ParamsClamping = new Types.Clamping.CalibrationParams();

            btnWrite.Visibility = Visibility.Collapsed;
            btnGoTest.Visibility = Visibility.Collapsed;
        }

        internal void AreButtonsEnabled(TypeCommon.InitParams Param)
        {
            btnGate.IsEnabled = Param.IsGateEnabled;
            btnVtm.IsEnabled = Param.IsSLEnabled;
            btnBvt.IsEnabled = Param.IsBVTEnabled;
            btnClamp.IsEnabled = Param.IsClampEnabled;
            btndVdt.IsEnabled = Param.IsdVdtEnabled;
        }

        private static ComplexParts IndexToDevice(int Index)
        {
            var device = ComplexParts.None;

            switch (Index)
            {
                case 1:
                    device = ComplexParts.Gate;
                    break;
                case 2:
                    device = ComplexParts.SL;
                    break;
                case 3:
                    device = ComplexParts.BVT;
                    break;
                case 4:
                    device = ComplexParts.Clamping;
                    break;
                case 5:
                    device = ComplexParts.DvDt;
                    break;
            }

            return device;
        }

        private void Button_Click(object Sender, RoutedEventArgs E)
        {
            var btn = Sender as Button;
            if (btn == null)
                return;

            m_Index = Convert.ToInt16(btn.CommandParameter);
            tabControl.SelectedIndex = m_Index;
            btnWrite.Visibility = m_Index != 0 ? Visibility.Visible : Visibility.Collapsed;
            btnGoTest.Visibility = m_Index != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Back_Click(object Sender, RoutedEventArgs E)
        {
            if (tabControl.SelectedIndex != 0)
            {
                m_Index = 0;
                tabControl.SelectedIndex = 0;
                btnWrite.Visibility = Visibility.Collapsed;
                btnGoTest.Visibility = Visibility.Collapsed;
            }
            else if (NavigationService != null)
                NavigationService.GoBack();
        }

        private void ShowError(ComplexParts Device, string Error)
        {
            switch (Device)
            {
                case ComplexParts.Gate:
                    ErrorGate = Error;
                    break;
                case ComplexParts.SL:
                    ErrorVtm = Error;
                    break;
                case ComplexParts.BVT:
                    ErrorBvt = Error;
                    break;
                case ComplexParts.Clamping:
                    ErrorClamping = Error;
                    break;
                case ComplexParts.DvDt:
                    ErrordVdt = Error;
                    break;
            }
        }

        private void Read(ComplexParts Device)
        {
            try
            {
                switch (Device)
                {
                    case ComplexParts.Gate:
                        ParamsGate = Cache.Net.GateReadCalibrationParameters();
                        ErrorGate = string.Empty;
                        break;
                    case ComplexParts.SL:
                        ParamsVtm = Cache.Net.SLReadCalibrationParameters();
                        ErrorVtm = string.Empty;
                        break;
                    case ComplexParts.BVT:
                        ParamsBvt = Cache.Net.BvtReadCalibrationParameters();
                        ErrorBvt = string.Empty;
                        break;
                    case ComplexParts.Clamping:
                        ParamsClamping = Cache.Net.CSReadCalibrationParameters();
                        ErrorClamping = string.Empty;
                        break;
                    case ComplexParts.DvDt:
                        ParamsdVdt = Cache.Net.DvDtReadCalibrationParameters();
                        ErrordVdt = string.Empty;
                        break;
                }
            }
            catch (FaultException<FaultData> ex)
            {
                ShowError(Device, string.Format("{0}. {1} : {2}", ex.Detail.Device, ex.Message, ex.Detail.Message));
            }
        }

        private void Write_Click(object Sender, RoutedEventArgs E)
        {
            var device = IndexToDevice(m_Index);

            try
            {
                switch (device)
                {
                    case ComplexParts.Gate:
                        Cache.Net.GateWriteCalibrationParameters(ParamsGate);
                        break;
                    case ComplexParts.SL:
                        Cache.Net.SLWriteCalibrationParameters(ParamsVtm);
                        break;
                    case ComplexParts.BVT:
                        Cache.Net.BvtWriteCalibrationParameters(ParamsBvt);
                        break;
                    case ComplexParts.Clamping:
                        Cache.Net.CSWriteCalibrationParameters(ParamsClamping);

                        Cache.Storage.WriteItem("ForceN", ParamsClamping.ForceFineN);
                        Cache.Storage.WriteItem("ForceD", ParamsClamping.ForceFineD);
                        Cache.Storage.WriteItem("ForceOffset", ParamsClamping.ForceOffset);
                        Cache.Storage.Save();
                        break;
                    case ComplexParts.DvDt:
                        Cache.Net.DvDtWriteCalibrationParameters(ParamsdVdt);
                        break;
                }

                Read(device);
            }
            catch (FaultException<FaultData> ex)
            {
                ShowError(device, string.Format("{0}. {1} : {2}", ex.Detail.Device, ex.Message, ex.Detail.Message));
            }
        }

        private void GoTest_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService == null)
                return;

            switch (m_Index)
            {
                case 1:
                    NavigationService.Navigate(Cache.Gate);
                    break;
                case 2:
                    NavigationService.Navigate(Cache.SL);
                    break;
                case 3:
                    NavigationService.Navigate(Cache.Bvt);
                    break;
                case 4:
                    NavigationService.Navigate(Cache.Clamp);
                    break;
                case 5:
//                    NavigationService.Navigate(Cache.dVdt);
                    break;
            }
        }

        private void TabControl_SelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            if (m_Index != 0)
                Read(IndexToDevice(m_Index));
        }

        private void Pulse_Gate_Click(object Sender, RoutedEventArgs E)
        {
            try
            {
                var result = Cache.Net.GatePulseCalibrationGate(ushort.Parse(calibrationCurrent.Text));

                actualCurrent.Content = result.Current.ToString(CultureInfo.InvariantCulture);
                actualVoltage.Content = result.Voltage.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
            }
        }

        private void Pulse_Main_Click(object Sender, RoutedEventArgs E)
        {
            try
            {
                var result = Cache.Net.GatePulseCalibrationMain(ushort.Parse(calibrationCurrent.Text));

                actualCurrent.Content = result.ToString(CultureInfo.InvariantCulture);
                actualVoltage.Content = "0";
            }
            catch
            {
            }
        }

        //internal void PatchClamp()
        //{
        //    ParamsClamping.ForceFineN =
        //        UInt16.Parse(
        //            (Cache.Storage.Collection.FirstOrDefault(It => It.Name == "ForceN") ??
        //             new CStorageItem {Name = "ForceN", Value = "1000"}).Value);
        //    ParamsClamping.ForceFineD =
        //        UInt16.Parse(
        //            (Cache.Storage.Collection.FirstOrDefault(It => It.Name == "ForceD") ??
        //             new CStorageItem { Name = "ForceD", Value = "1000" }).Value);
        //    ParamsClamping.ForceOffset =
        //        Int16.Parse(
        //            (Cache.Storage.Collection.FirstOrDefault(It => It.Name == "ForceOffset") ??
        //             new CStorageItem {Name = "ForceOffset", Value = "0"}).Value);

        //    Cache.Net.CSWriteCalibrationParameters(ParamsClamping);
        //}
    }
}