using SCME.Types;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for DeviceReferences.xaml
    /// </summary>
    public partial class DeviceReferences : Window, INotifyPropertyChanged
    {
        public DeviceReferences()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        DataTable FDataTable = new DataTable();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public DataView DeviceReferencesList
        {
            get
            {
                DbRoutines.GetDeviceReferences(this.FDataTable);

                return this.FDataTable.DefaultView;
            }
        }

        public void ShowModal()
        {
            this.ShowDialog();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    this.DialogResult = false;
                    break;
            }
        }

        private void DgDeviceReferences_Loaded(object sender, RoutedEventArgs e)
        {
            if ((sender is DataGrid dg) && (this.DgDeviceReferences.Items.Count > 0))
                this.DgDeviceReferences.SelectedIndex = 0;
        }

        private void MnuCreateClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanManageDeviceReferences(((MainWindow)this.Owner).PermissionsLo))
            {
                DeviceReferenceEditor deviceReferenceEditor = new DeviceReferenceEditor();

                if (deviceReferenceEditor.ShowModal(null, out int? createdDeviceReferenceID, 0, -1, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null) ?? false)
                {
                    //пользователь выполнил сохранение
                    this.OnPropertyChanged();

                    //ищем в списке созданную запись
                    if (createdDeviceReferenceID != null)
                    {
                        object item = this.DgDeviceReferences.Items.OfType<DataRowView>().FirstOrDefault(row => int.Parse(row["DEVICEREFERENCEID"].ToString()) == createdDeviceReferenceID);

                        if (item != null)
                        {
                            this.DgDeviceReferences.UpdateLayout();
                            this.DgDeviceReferences.ScrollIntoView(item);
                        }
                    }
                }
            }
        }

        private int SelectedDeviceReferenceID()
        {
            //считывает идентификатор выбранной пользователем записи
            //возвращает:
            //-1 - пользователь не выбрал запись в DgDeviceReferences
            //иначе - идентификатор записи
            int deviceReferenceID = 0;

            //если ячейка в DgDeviceReferences выбрана - считываем идентификатор записи
            if (this.DgDeviceReferences.CurrentCell.IsValid)
            {
                deviceReferenceID = Convert.ToInt32(this.DgDeviceReferences.ValueFromSelectedRow("DEVICEREFERENCEID"));
            }

            return (deviceReferenceID == 0) ? -1 : deviceReferenceID;
        }

        private void MnuEditClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanManageDeviceReferences(((MainWindow)this.Owner).PermissionsLo))
            {
                int deviceReferenceID = this.SelectedDeviceReferenceID();

                if (deviceReferenceID == -1)
                {
                    MessageBox.Show(Properties.Resources.NoEditingObjectSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                int itav = int.Parse(this.DgDeviceReferences.ValueFromSelectedRow("ITAV").ToString());
                int deviceTypeID = int.Parse(this.DgDeviceReferences.ValueFromSelectedRow("DEVICETYPEID").ToString());
                string constructive = this.DgDeviceReferences.ValueFromSelectedRow(Common.Constants.Constructive).ToString();
                object obj = this.DgDeviceReferences.ValueFromSelectedRow("MODIFICATION");
                string modification = (obj == DBNull.Value) ? null : obj.ToString().Trim();

                obj = this.DgDeviceReferences.ValueFromSelectedRow("IGTMAX");
                int? igtMax = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("UGTMAX");
                decimal? ugtMax = (obj == DBNull.Value) ? null : (decimal?)decimal.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("TGTMAX");
                decimal? tgtMax = (obj == DBNull.Value) ? null : (decimal?)decimal.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("UBRMIN");
                int? ubrMin = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("UDSMMIN");
                int? udsmMin = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("URSMMIN");
                int? ursmMin = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("UTMMAX");
                decimal? utmMax = (obj == DBNull.Value) ? null : (decimal?)decimal.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("UFMMAX");
                decimal? ufmMax = (obj == DBNull.Value) ? null : (decimal?)decimal.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("IDRMMAX");
                int? idrmMax = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("IRRMMAX");
                int? irrmMax = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("DUDTMIN");
                int? dUdtMin = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("PRSMMIN");
                int? prsmMin = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("TRRMIN");
                decimal? trrMin = (obj == DBNull.Value) ? null : (decimal?)decimal.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("TQMIN");
                decimal? tqMin = (obj == DBNull.Value) ? null : (decimal?)decimal.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("RISOLMIN");
                int? risolMin = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("UISOLMIN");
                int? uisolMin = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("QRRMAX");
                int? qrrMax = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                obj = this.DgDeviceReferences.ValueFromSelectedRow("TJMAX");
                int? tjMax = (obj == DBNull.Value) ? null : (int?)int.Parse(obj.ToString());

                string caseType = this.DgDeviceReferences.ValueFromSelectedRow("CASETYPE").ToString();

                obj = this.DgDeviceReferences.ValueFromSelectedRow("UTMCORRECTION");
                decimal? utmCorrection = (obj == DBNull.Value) ? null : (decimal?)decimal.Parse(obj.ToString());

                DeviceReferenceEditor deviceReferenceEditor = new DeviceReferenceEditor();

                if (deviceReferenceEditor.ShowModal(deviceReferenceID, out int? createdDeviceReferenceID, itav, deviceTypeID, constructive, modification, igtMax, ugtMax, tgtMax, ubrMin, udsmMin, ursmMin, utmMax, ufmMax, idrmMax, irrmMax, dUdtMin, prsmMin, trrMin, tqMin, risolMin, uisolMin, qrrMax, tjMax, caseType, utmCorrection) ?? false)
                {
                    //пользователь выполнил сохранение параметра
                    OnPropertyChanged();
                }
            }
        }

        private void MnuDeleteClick(object sender, RoutedEventArgs e)
        {
            if (Common.Routines.IsUserCanManageDeviceReferences(((MainWindow)this.Owner).PermissionsLo))
            {
                int deviceReferenceID = this.SelectedDeviceReferenceID();

                if (deviceReferenceID == -1)
                {
                    MessageBox.Show(Properties.Resources.ObjectForDeleteNotSelected, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (MessageBox.Show(string.Concat(Properties.Resources.DeleteCurrentRecord, "?"), Application.ResourceAssembly.GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    DbRoutines.DeleteFromDeviceReferences(deviceReferenceID);

                    //удаление записи выполнено
                    this.OnPropertyChanged();
                }
            }
        }
    }
}
