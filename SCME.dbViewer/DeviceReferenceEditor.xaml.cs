using SCME.Types;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for DeviceReferenceEditor.xaml
    /// </summary>
    public partial class DeviceReferenceEditor : Window
    {
        public DeviceReferenceEditor()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        public bool? ShowModal(int? deviceReferenceID, out int? createdDeviceReferenceID, int itav, int deviceTypeID, string constructive, string modification, int? igtMax, decimal? ugtMax, decimal? tgtMax, int? ubrMin, int? udsmMin, int? ursmMin, decimal? utmMax, decimal? ufmMax, int? idrmMax, int? irrmMax, int? dUdtMin, int? prsmMin, decimal? trrMin, decimal? tqMin, int? risolMin, int? uisolMin, int? qrrMax, int? tjMax, string caseType, decimal? utmCorrection)
        {
            //данная реализация принимает на вход идентификатор записи deviceReferenceID и её реквизиты, отображает их значения в данной форме
            //в out параметре createdDeviceReferenceID возвращается идентификатор созданной записи. если запись редактировалась или удалялась - возвращает null
            //возвращает True - реквизиты принятой записи были обновлены пользователем - пользователь нажал кнопку OK. на момент получения такого результата в базу данных уже было выполнено сохранение этих изменений;
            //возвращает False - пользователь закрыл форму, т.е. отказался от редактирования

            createdDeviceReferenceID = null;

            this.TbItav.Text = itav.ToString();

            string[] deviceTypeItem = this.CmbDeviceType.Items.OfType<string[]>().FirstOrDefault(x => int.Parse(x[0]) == deviceTypeID);
            if (deviceTypeItem == null)
            {
                this.CmbDeviceType.SelectedIndex = -1;
            }
            else
                this.CmbDeviceType.SelectedItem = deviceTypeItem;

            this.TbConstructive.Text = constructive;
            this.TbModification.Text = modification;

            this.TbIgtMax.Text = igtMax?.ToString();
            this.TbUgtMax.Text = (ugtMax == null) ? null : string.Format("{0:N2}", ugtMax);
            this.TbTgtMax.Text = (tgtMax == null) ? null : string.Format("{0:N2}", tgtMax);
            this.TbUbrMin.Text = ubrMin?.ToString();
            this.TbUdsmMin.Text = udsmMin.ToString();
            this.TbUrsmMin.Text = ursmMin.ToString();
            this.TbUtmMax.Text = (utmMax == null) ? null : string.Format("{0:N2}", utmMax);
            this.TbUfmMax.Text = (ufmMax == null) ? null : string.Format("{0:N2}", ufmMax);
            this.TbIdrmMax.Text = idrmMax?.ToString();
            this.TbIrrmMax.Text = irrmMax.ToString();
            this.TbdUdtMin.Text = dUdtMin.ToString();
            this.TbPrsmMin.Text = prsmMin?.ToString();
            this.TbTrrMin.Text = (trrMin == null) ? null : string.Format("{0:N2}", trrMin);
            this.TbTqMin.Text = (tqMin == null) ? null : string.Format("{0:N2}", tqMin);
            this.TbRisolMin.Text = risolMin.ToString();
            this.TbUisolMin.Text = uisolMin.ToString();
            this.TbQrrMax.Text = qrrMax?.ToString();
            this.TbTjMax.Text = tjMax?.ToString();
            this.TbCaseType.Text = caseType;
            this.TbUtmCorrection.Text = (utmCorrection == null) ? null : string.Format("{0:N2}", utmCorrection);

            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                //пользователь хочет сохранить сделанные изменения
                string tabNum = ((MainWindow)this.Owner).TabNum;

                //проверяем, что он аутентифицирован в системе
                if (string.IsNullOrEmpty(tabNum))
                {
                    MessageBox.Show(Properties.Resources.PleaseAuthenticate, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                if (this.CheckData())
                {
                    int editedItav = int.Parse(TbItav.Text);
                    string[] selectedItem = this.CmbDeviceType.SelectedItem as string[];
                    int editedDeviceTypeID = int.Parse(selectedItem[0]);
                    string editedConstructive = this.TbConstructive.Text.Trim();
                    string editedModification = string.IsNullOrEmpty(this.TbModification.Text.Trim()) ? null : this.TbModification.Text.Trim();

                    int? editedIgtMax = string.IsNullOrEmpty(this.TbIgtMax.Text.Trim()) ? null : (int?)int.Parse(this.TbIgtMax.Text.Trim());
                    decimal? editedUgtMax = string.IsNullOrEmpty(this.TbUgtMax.Text.Trim()) ? null : (decimal?)decimal.Parse(this.TbUgtMax.Text.Trim());
                    decimal? editedTgtMax = string.IsNullOrEmpty(this.TbTgtMax.Text.Trim()) ? null : (decimal?)decimal.Parse(this.TbTgtMax.Text.Trim());
                    int? editedUbrMin = string.IsNullOrEmpty(this.TbUbrMin.Text.Trim()) ? null : (int?)int.Parse(this.TbUbrMin.Text.Trim());
                    int? editedUdsmMin = string.IsNullOrEmpty(this.TbUdsmMin.Text.Trim()) ? null : (int?)int.Parse(this.TbUdsmMin.Text.Trim());
                    int? editedUrsmMin = string.IsNullOrEmpty(this.TbUrsmMin.Text.Trim()) ? null : (int?)int.Parse(this.TbUrsmMin.Text.Trim());
                    decimal? editedUtmMax = string.IsNullOrEmpty(this.TbUtmMax.Text.Trim()) ? null : (decimal?)decimal.Parse(this.TbUtmMax.Text.Trim());
                    decimal? editedUfmMax = string.IsNullOrEmpty(this.TbUfmMax.Text.Trim()) ? null : (decimal?)decimal.Parse(this.TbUfmMax.Text.Trim());
                    int? editedIdrmMax = string.IsNullOrEmpty(this.TbIdrmMax.Text.Trim()) ? null : (int?)int.Parse(this.TbIdrmMax.Text.Trim());
                    int? editedIrrmMax = string.IsNullOrEmpty(this.TbIrrmMax.Text.Trim()) ? null : (int?)int.Parse(this.TbIrrmMax.Text.Trim());
                    int? editeddUdtMin = string.IsNullOrEmpty(this.TbdUdtMin.Text.Trim()) ? null : (int?)int.Parse(this.TbdUdtMin.Text.Trim());
                    int? editedPrsmMin = string.IsNullOrEmpty(this.TbPrsmMin.Text.Trim()) ? null : (int?)int.Parse(this.TbPrsmMin.Text.Trim());
                    decimal? editedTrrMin = string.IsNullOrEmpty(this.TbTrrMin.Text.Trim()) ? null : (decimal?)decimal.Parse(this.TbTrrMin.Text.Trim());
                    decimal? editedTqMin = string.IsNullOrEmpty(this.TbTqMin.Text.Trim()) ? null : (decimal?)decimal.Parse(this.TbTqMin.Text.Trim());
                    int? editedRisolMin = string.IsNullOrEmpty(this.TbRisolMin.Text.Trim()) ? null : (int?)int.Parse(this.TbRisolMin.Text.Trim());
                    int? editedUisolMin = string.IsNullOrEmpty(this.TbUisolMin.Text.Trim()) ? null : (int?)int.Parse(this.TbUisolMin.Text.Trim());
                    int? editedQrrMax = string.IsNullOrEmpty(this.TbQrrMax.Text.Trim()) ? null : (int?)int.Parse(this.TbQrrMax.Text.Trim());
                    int? editedTjMax = string.IsNullOrEmpty(this.TbTjMax.Text.Trim()) ? null : (int?)int.Parse(this.TbTjMax.Text.Trim());
                    string editedCaseType = this.TbCaseType.Text.Trim();
                    decimal? editedUtmCorrection = string.IsNullOrEmpty(this.TbUtmCorrection.Text.Trim()) ? null : (decimal?)decimal.Parse(this.TbUtmCorrection.Text.Trim());

                    //раскоммент
                    createdDeviceReferenceID = DbRoutines.SaveToDeviceReferences(deviceReferenceID, editedItav, editedDeviceTypeID, editedConstructive, editedModification, editedIgtMax, editedUgtMax, editedTgtMax, editedUbrMin, editedUdsmMin, editedUrsmMin, editedUtmMax, editedUfmMax, editedIdrmMax, editedIrrmMax, editeddUdtMin, editedPrsmMin, editedTrrMin, editedTqMin, editedRisolMin, editedUisolMin, editedQrrMax, editedTjMax, editedCaseType, editedUtmCorrection, tabNum);
                }
            }

            return result;
        }

        private bool CheckData()
        {
            //выполняет проверку введённых пользователем данных с точки зрения возможности выполнения сохранения в базу данных
            if (string.IsNullOrEmpty(TbItav.Text.Trim()) || (!int.TryParse(TbItav.Text, out int itav)))
            {
                MessageBox.Show(string.Concat(Properties.Resources.AverageCurrent, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            //проверяем значение типа изделия
            if (this.CmbDeviceType.SelectedItem == null)
            {
                MessageBox.Show(string.Format(Properties.Resources.SubjectNotSetted, Properties.Resources.DeviceType, Properties.Resources.NotSetted), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            string[] selectedItem = CmbDeviceType.SelectedItem as string[];
            if (!int.TryParse(selectedItem[0], out int deviceTypeID))
                throw new Exception(string.Format("The '{0}' is not an integer value", selectedItem[0]));

            if (string.IsNullOrEmpty(TbConstructive.Text))
            {
                MessageBox.Show(string.Concat(Properties.Resources.Constructive, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            //значение TbModification.Text может быть любым - не делаем его проверку

            if (!string.IsNullOrEmpty(TbIgtMax.Text) && !int.TryParse(TbIgtMax.Text, out int igtMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.IgtMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbUgtMax.Text) && !decimal.TryParse(TbUgtMax.Text, out decimal ugtMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UgtMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbTgtMax.Text) && !decimal.TryParse(TbTgtMax.Text, out decimal tgtMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.TgtMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbUbrMin.Text) && !int.TryParse(TbUbrMin.Text, out int ubrMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UbrMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbUdsmMin.Text) && !int.TryParse(TbUdsmMin.Text, out int udsmMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UdsmMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbUrsmMin.Text) && !int.TryParse(TbUrsmMin.Text, out int ursmMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UrsmMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbUtmMax.Text) && !decimal.TryParse(TbUtmMax.Text, out decimal utmMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UtmMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbUfmMax.Text) && !decimal.TryParse(TbUfmMax.Text, out decimal ufmMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UfmMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            //если значение введено - оно должно без ошибки преобразовываться в тип int
            if (!string.IsNullOrEmpty(TbIdrmMax.Text) && !int.TryParse(TbIdrmMax.Text, out int idrmMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.IdrmMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbIrrmMax.Text) && !int.TryParse(TbIrrmMax.Text, out int irrmMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.IrrmMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbdUdtMin.Text) && !int.TryParse(TbdUdtMin.Text, out int dUdtMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.dUdtMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbPrsmMin.Text) && !int.TryParse(TbPrsmMin.Text, out int prsmMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.PrsmMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbTrrMin.Text) && !decimal.TryParse(TbTrrMin.Text, out decimal trrMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.TrrMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbTqMin.Text) && !decimal.TryParse(TbTqMin.Text, out decimal tqMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.TqMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbRisolMin.Text) && !int.TryParse(TbRisolMin.Text, out int risolMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.RisolMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbUisolMin.Text) && !int.TryParse(TbUisolMin.Text, out int uisolMin))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UisolMin, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbQrrMax.Text) && !int.TryParse(TbQrrMax.Text, out int qrrMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.QrrMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (!string.IsNullOrEmpty(TbTjMax.Text) && !int.TryParse(TbTjMax.Text, out int tjMax))
            {
                MessageBox.Show(string.Concat(Properties.Resources.TjMax, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            //значение TbCaseType.Text может быть любым - не делаем его проверку

            if (!string.IsNullOrEmpty(TbUtmCorrection.Text) && !decimal.TryParse(TbUtmCorrection.Text, out decimal utmCorrection))
            {
                MessageBox.Show(string.Concat(Properties.Resources.UtmCorrection, ". ", Properties.Resources.DataWillNotBeSaved), Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.DialogResult = false;
        }

        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            if (this.CheckData())
                this.DialogResult = true;
        }

        private void TbIdrmMax_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender is TextBox tb) && (!string.IsNullOrEmpty(tb.Text)))
            {
                if (string.IsNullOrEmpty(this.TbIrrmMax.Text))
                    this.TbIrrmMax.Text = tb.Text;
            }
        }

        private void TbIrrmMax_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender is TextBox tb) && (!string.IsNullOrEmpty(tb.Text)))
            {
                if (string.IsNullOrEmpty(this.TbIdrmMax.Text))
                    this.TbIdrmMax.Text = tb.Text;
            }
        }
    }
}
