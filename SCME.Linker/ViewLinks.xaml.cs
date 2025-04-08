using SCME.Common;
using SCME.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
/*
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
*/
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
//using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
/*
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
*/
using System.Windows.Threading;
using static SCME.Types.DbRoutines;

namespace SCME.Linker
{
    /// <summary>
    /// Interaction logic for ViewLinks.xaml
    /// </summary>
    public partial class ViewLinks : Window
    {
        public ViewLinks()
        {
            InitializeComponent();

            this.Owner = System.Windows.Application.Current.MainWindow;
            btSetGroupName.Content = string.Concat(Properties.Resources.SetValue, " ", Properties.Resources.GroupName);
        }

        private string FGroupName = null;
        private ObservableCollection<RecordOfAssembly> FListData = null;

        public bool? ShowModal(string selectedGroupName)
        {
            //устанавливаем ПЗ равным принятому selectedGroupName
            if (selectedGroupName == null)
            {
                //оператор отказался от выбора ПЗ из списка и хочет ввести обозначение ПЗ вручную
                this.ManualEntryGroupName();
            }
            else
                this.SetGroupName(selectedGroupName);

            bool? result = this.ShowDialog();

            if (result ?? false)
            {


            }
            else
            {


            }

            return result;
        }

        private string FirstLower(string value)
        {
            //перевод певого символа принятого value в нижний регистр
            if (string.IsNullOrEmpty(value))
                return value;

            return string.Concat(value[0].ToString().ToLower(), value.Substring(1));
        }







        

        private void SetData()
        {
            //считываем запущенное по ПЗ количество изделий
            double qtyReleased = DbRoutines.QtyReleased(this.FGroupName);

            //считываем применяемость ППЭ из текущей структуры изделия
            //но для этого сначала читаем код изделия, который получается в результате выполнения ПЗ 
            string description;
            string topItem = DbRoutines.ItemByGroupName(this.FGroupName, out description);

            if (topItem == null)
            {
                this.lbData1ByGroupName.Content = Properties.Resources.NoData;
                this.lbData2ByGroupName.Content = Properties.Resources.NoData;
            }
            else
            {
                //читаем код ПЗ текущей структуры изделия для считанного topItem
                string suffix;
                string job = DbRoutines.JobFromItemStructure(topItem, out suffix);

                int amountOfElements;
                bool applicabilityReaded = DbRoutines.AmountOfElements(job, suffix, out amountOfElements);

                string applicabilityDescr = string.Empty;
                if (applicabilityReaded)
                {
                    //если применяемость ППЭ больше 1 - надо показывать столбец с обозначением второго элемента, иначе - его надо спрятать
                    int columnIndex = dgLinks.Columns.IndexOf(dgLinks.Columns.FirstOrDefault(c => c.Header.ToString() == Properties.Resources.Element2));
                    dgLinks.Columns[columnIndex].Visibility = (amountOfElements > 1) ? Visibility.Visible : Visibility.Collapsed;

                    applicabilityDescr = string.Format(Properties.Resources.Applicability, amountOfElements.ToString(), Properties.Resources.Pieces);

                    //по считанному значению применяемости либо показываем либо прячем поле для ввода второго кода ППЭ
                    Visibility device2Visibility = (amountOfElements == 2) ? Visibility.Visible : Visibility.Collapsed;
                    lbDevice2.Visibility = tbDeviceCode2.Visibility = device2Visibility;

                    grDevice.RowDefinitions.Clear();

                    if (device2Visibility == Visibility.Visible)
                    {
                        //применяемость 2
                        RowDefinition rd = new RowDefinition() { Height = new GridLength(1.1, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);

                        rd = new RowDefinition() { Height = new GridLength(1.8, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);

                        rd = new RowDefinition() { Height = new GridLength(1.1, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);

                        rd = new RowDefinition() { Height = new GridLength(1.8, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);

                        Grid.SetRow(lbDevice, 0);
                        Grid.SetRow(tbDeviceCode, 1);
                        Grid.SetRow(lbDevice2, 2);
                        Grid.SetRow(tbDeviceCode2, 3);
                    }
                    else
                    {
                        //применяемость 1
                        RowDefinition rd = new RowDefinition() { Height = new GridLength(1.1, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);
                        rd = new RowDefinition() { Height = new GridLength(1.2, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);
                        rd = new RowDefinition() { Height = new GridLength(1.8, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);
                        rd = new RowDefinition() { Height = new GridLength(1.1, GridUnitType.Star) };
                        grDevice.RowDefinitions.Add(rd);

                        Grid.SetRow(lbDevice, 1);
                        Grid.SetRow(tbDeviceCode, 2);
                    }
                }

                //читаем тип корпуса
                string package = Routines.PackageTypeByItem(topItem);

                string qtyReleasedDescr = string.Format(Properties.Resources.QtyReleased, qtyReleased.ToString(), Properties.Resources.Pieces);
                string packageDescr = string.Format(string.Concat(Properties.Resources.Package, " '{0}'"), package);

                this.lbData1ByGroupName.Content = qtyReleasedDescr;

                this.lbData2ByGroupName.Content = string.Concat(
                                                                 packageDescr, "\r\n",
                                                                 applicabilityDescr
                                                               );
            }

            this.RefreshCounters();
        }

        private void RefreshCounters()
        {
            //обновление отображения значения счётчика созданных связей по ПЗ
            const string cNewLine = "\r\n";
            int index = this.lbData1ByGroupName.Content.ToString().IndexOf(cNewLine);

            string lbData1ByGroupNameContent;

            switch (index)
            {
                case -1:
                    lbData1ByGroupNameContent = this.lbData1ByGroupName.Content.ToString();
                    break;

                default:
                    lbData1ByGroupNameContent = this.lbData1ByGroupName.Content.ToString().Substring(0, index);
                    break;
            }

            string recordsShown = string.Format(Properties.Resources.CreatedLinksTotal, this.FListData.Count, Properties.Resources.Pieces);
            this.lbData1ByGroupName.Content = string.Concat(lbData1ByGroupNameContent, "\r\n",
                                                            recordsShown);
        }

        private void rbAssembly_Click(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).IsChecked ?? false)
            {
                //случай сборки
                btSave.IsEnabled = (this.FGroupName != null);
                grPackage.RowDefinitions.Clear();
                reDelimeter.Visibility = Visibility.Visible;
                grDevice.Visibility = Visibility.Visible;

                RowDefinition rd = new RowDefinition() { Height = new GridLength(1.1, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                rd = new RowDefinition() { Height = new GridLength(1.2, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                rd = new RowDefinition() { Height = new GridLength(1.8, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                rd = new RowDefinition() { Height = new GridLength(1.1, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                Grid.SetRow(lbOldPackageSerialNum, 1);
                Grid.SetRow(tbOldPackageSerialNum, 2);
                tbOldPackageSerialNum.Background = Brushes.White;

                lbNewPackageSerialNum.Visibility = Visibility.Hidden;
                tbNewPackageSerialNum.Visibility = Visibility.Hidden;
            }
        }

        private void rbRelabeling_Click(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).IsChecked ?? false)
            {
                //случай перемаркировки
                btSave.IsEnabled = ((this.FGroupName != null) && (this.FListData != null) && (this.FListData.Count != 0));

                grPackage.RowDefinitions.Clear();
                lbNewPackageSerialNum.Visibility = Visibility.Visible;
                tbNewPackageSerialNum.Visibility = Visibility.Visible;
                reDelimeter.Visibility = Visibility.Hidden;
                grDevice.Visibility = Visibility.Hidden;

                RowDefinition rd = new RowDefinition() { Height = new GridLength(1.2, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                rd = new RowDefinition() { Height = new GridLength(1.8, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                rd = new RowDefinition() { Height = new GridLength(1.2, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                rd = new RowDefinition() { Height = new GridLength(1.8, GridUnitType.Star) };
                grPackage.RowDefinitions.Add(rd);

                Grid.SetRow(lbOldPackageSerialNum, 0);
                Grid.SetRow(tbOldPackageSerialNum, 1);
                tbOldPackageSerialNum.Background = Brushes.White;

                Grid.SetRow(lbNewPackageSerialNum, 2);
                Grid.SetRow(tbNewPackageSerialNum, 3);
                tbNewPackageSerialNum.Background = Brushes.White;
            }
        }

        private void Load()
        {
            //загрузка списка ранее созданных связей ППЭ-корпус из БД в this.RecordOfAssemblyList
            if (this.FGroupName == null)
            {
                System.Windows.MessageBox.Show(Properties.Resources.GroupNameNotSelected);

                return;
            }

            //грузим список созданных связей корпус-ППЭ
            this.FListData = new ObservableCollection<RecordOfAssembly>();
            dgLinks.ItemsSource = this.FListData;
            DbRoutines.LoadFromAssemblyToList(this.FListData, this.FGroupName);

            this.SetData();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    this.Close();
                    break;

                case Key.Enter:
                    e.Handled = true;

                    if (rbAssembly.IsChecked ?? false)
                    {
                        bool packageIsGood = (this.CheckPackageByGroupNameFull(tbOldPackageSerialNum) && this.IsAssemblyExistsFull(tbOldPackageSerialNum, false));

                        //ввод серийного номера корпуса завершён
                        if (tbOldPackageSerialNum.IsFocused)
                        {
                            if (packageIsGood)
                                tbDeviceCode.Focus();

                            return;
                        }

                        bool deviceCodeIsGood = (IsLotIssuedToGroupNameFull(tbDeviceCode) && !IsDeviceCodeUsedFull(tbDeviceCode));

                        if (tbDeviceCode.IsFocused)
                        {
                            if (tbDeviceCode2.Visibility == Visibility.Visible)
                            {
                                tbDeviceCode2.Focus();
                                return;
                            }
                        }

                        bool deviceCodeIsGood2 = (((tbDeviceCode2.Visibility == Visibility.Visible) && this.IsLotIssuedToGroupNameFull(tbDeviceCode2) && !IsDeviceCodeUsedFull(tbDeviceCode2)) || (tbDeviceCode2.Visibility != Visibility.Visible));

                        if (packageIsGood && deviceCodeIsGood && deviceCodeIsGood2)
                            this.btSave.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    }

                    if (rbRelabeling.IsChecked ?? false)
                    {
                        //вхождение в допустимые границы для ранее введённого серийного номера не проверяем, но наличие серийного номера проверяем
                        bool packageIsGood = this.IsAssemblyExistsFull(tbOldPackageSerialNum, true);

                        if (tbOldPackageSerialNum.IsFocused)
                        {
                            if (packageIsGood)
                                tbNewPackageSerialNum.Focus();

                            return;
                        }

                        bool newPackageIsGood = (this.CheckPackageByGroupNameFull(tbNewPackageSerialNum) && this.IsAssemblyExistsFull(tbNewPackageSerialNum, false));

                        if (tbNewPackageSerialNum.IsFocused)
                        {
                            if (packageIsGood && newPackageIsGood)
                                this.btSave.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                        }
                    }

                    break;
            }
        }

        private void SetGroupName(string groupName)
        {
            this.FGroupName = groupName.TrimEnd();

            btSave.IsEnabled = (this.rbAssembly.IsChecked == true);

            //показываем пользователю выбранный ПЗ
            btSetGroupName.Content = this.FGroupName;

            //ПЗ выбран - грузим данные из базы данных
            this.Load();

            tbOldPackageSerialNum.Focus();
        }

        private bool ManualEntryGroupName()
        {
            //true - оператор выполнил ввод ПЗ
            //false - оператор отказался от ввода ПЗ
            DialogInputString dialogInputString = new DialogInputString();

            if (dialogInputString.ShowModal(Routines.CheckGroupNameByMask, Properties.Resources.GroupName, this.FGroupName, out string selectedGroupName)) //== true
            {
                this.SetGroupName(selectedGroupName);

                return true;
            }

            return false;
        }

        private void btSetGroupName_Click(object sender, RoutedEventArgs e)
        {
            //запрашиваем у пользователя сборочный ПЗ и запоминаем его обозначение в this.FGroupName
            Jobs jobs = new Jobs();

            bool groupNameSelected = jobs.ShowModal(out string selectedGroupName);

            if (groupNameSelected && (selectedGroupName != null))
            {
                this.SetGroupName(selectedGroupName);
            }
            else
            {
                this.ManualEntryGroupName();
            }
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            if (rbAssembly.IsChecked ?? false)
            {
                //проверяем корректность введённого серийного номера изделия/корпуса. это проверка нужна и при сборке и при перемаркировке 
                bool oldPackageSerialNumIsGood = this.CheckPackageByGroupNameFull(tbOldPackageSerialNum); // && !this.IsAssemblyExistsFull(tbOldPackageSerialNum));

                //создание в БД связи между ППЭ (серийный номер ППЭ) и корпусом (серийный номер корпуса)
                //проверяем корректность кодов ППЭ и ППЭ2
                bool deviceCodeIsGood = (this.IsLotIssuedToGroupNameFull(tbDeviceCode) && !this.IsDeviceCodeUsedFull(tbDeviceCode));
                bool deviceCode2IsGood = (((tbDeviceCode2.Visibility == Visibility.Visible) && (this.IsLotIssuedToGroupNameFull(tbDeviceCode2))) || (tbDeviceCode2.Visibility != Visibility.Visible));

                if (oldPackageSerialNumIsGood && deviceCodeIsGood && deviceCode2IsGood)
                {
                    //смотрим с чем мы имеем дело: создание новой связи или редактирование уже существующей
                    string oldSerialNum = null;
                    RecordOfAssembly recordOfAssembly = null;

                    object selectedItem = dgLinks.SelectedItem;
                    if (selectedItem != null)
                    {
                        if (selectedItem is RecordOfAssembly)
                        {
                            recordOfAssembly = (RecordOfAssembly)selectedItem;
                            oldSerialNum = recordOfAssembly.PackageSerialNum;
                        }
                    }

                    /*
                    int selectedRowIndex = dgLinks.SelectedIndex;
                    RecordOfAssembly recordOfAssembly = null;
                    
                    if (selectedRowIndex != -1)
                    {
                        recordOfAssembly = this.FListData[selectedRowIndex];
                        oldSerialNum = recordOfAssembly.PackageSerialNum;
                    }
                    */

                    if ((oldSerialNum != null) && (DbRoutines.IsAssemblyExists(oldSerialNum)))
                    {
                        if (Routines.IsUserCanEditAssembly(((MainWindow)Application.Current.MainWindow).FPermissionsLo))
                        {
                            //сборка уже существует в БД - выполняется редактирование сборки
                            int packageID = DbRoutines.UpdateAssembly(oldSerialNum, tbOldPackageSerialNum.Text, this.FGroupName, tbDeviceCode.Text, tbDeviceCode2.Text, ((MainWindow)this.Owner).FTabNum);

                            if (packageID != -1)
                                DbRoutines.LoadSingleRecordFromAssembly(recordOfAssembly, packageID);
                        }
                    }
                    else
                    {
                        if (!this.IsDeviceCodeUsedFull(tbDeviceCode2) && this.IsAssemblyExistsFull(tbOldPackageSerialNum, false))
                        {
                            //сборка не существует в БД, она создаётся
                            int packageID = DbRoutines.CreateAssembly(this.FGroupName, tbDeviceCode.Text, tbDeviceCode2.Text, tbOldPackageSerialNum.Text, ((MainWindow)this.Owner).FTabNum);

                            //добавляем в самый верх списка this.FListData сборку RecordOfAssembly с реквизитами прочитанными из БД по идентификатору PackageID
                            recordOfAssembly = new RecordOfAssembly();
                            if (DbRoutines.LoadSingleRecordFromAssembly(recordOfAssembly, packageID))
                            {
                                this.FListData.Add(recordOfAssembly);
                                this.dgLinks.Items.Refresh();
                                this.dgLinks.ScrollIntoView(recordOfAssembly);

                                this.RefreshCounters();
                            }

                            tbOldPackageSerialNum.Clear();
                            tbOldPackageSerialNum.Background = Brushes.White;
                            tbOldPackageSerialNum.Focus();

                            tbDeviceCode.Clear();
                            tbDeviceCode.Background = Brushes.White;

                            tbDeviceCode2.Clear();
                            tbDeviceCode2.Background = Brushes.White;
                        }
                    }
                }
            }

            if (rbRelabeling.IsChecked ?? false)
            {
                //перемаркировка - изменение серийного номера корпуса
                bool newPackageSerialNumIsGood = this.CheckPackageByGroupNameFull(tbNewPackageSerialNum);

                if (newPackageSerialNumIsGood)
                {
                    switch (DbRoutines.Relabeling(this.FGroupName, tbOldPackageSerialNum.Text, tbNewPackageSerialNum.Text, ((MainWindow)Application.Current.MainWindow).FTabNum, out int packageID))
                    {
                        //ошибка в реализации UpdateAssembly
                        case -1:
                            System.Windows.Forms.MessageBox.Show(string.Concat(string.Format(Properties.Resources.RealisationError, "DbRoutines.UpdateAssembly"), "."), Properties.Resources.OperationError, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                            break;

                        //замена старого серийного номера корпуса успешно произведена на новый серийный номер корпуса
                        case 0:
                            tbOldPackageSerialNum.Background = Brushes.LightGreen;
                            tbNewPackageSerialNum.Background = Brushes.LightGreen;

                            //перечитываем данные изменённой записи в this.FListData
                            RecordOfAssembly recordOfAssembly = this.FListData.Where(x => x.PackageSerialNum == tbOldPackageSerialNum.Text).FirstOrDefault() as RecordOfAssembly;

                            if (recordOfAssembly != null)
                                DbRoutines.LoadSingleRecordFromAssembly(recordOfAssembly, packageID);

                            break;

                        //новый серийный номер корпуса @NewSerialNum уже используется - не корректен
                        case 1:
                            tbNewPackageSerialNum.Background = Brushes.LightPink;
                            CreateToolTip(tbNewPackageSerialNum, Properties.Resources.SerialNumIsNotValid);
                            break;

                        //2 - старый серийный номер корпуса @OldSerialNum не найден
                        case 2:
                            tbOldPackageSerialNum.Background = Brushes.LightPink;
                            CreateToolTip(tbOldPackageSerialNum, Properties.Resources.OldSerialNumIsNotFound);
                            break;
                    }
                }
            }
        }

        private CustomPopupPlacement[] PositionToolTip(Size popupSize, Size targetSize, Point offset)
        {
            double offsetY = -targetSize.Height / 2 - popupSize.Height;
            double offsetX = 0;

            return new CustomPopupPlacement[] { new CustomPopupPlacement(new Point(offsetX, offsetY), PopupPrimaryAxis.None) };
        }

        private void CreateToolTip(TextBox tb, string toolTipText)
        {
            //выводим подробное описание ошибки
            ToolTip toolTip = new ToolTip();

            StackPanel toolTipPanel = new StackPanel();
            toolTipPanel.Children.Add(new TextBlock { Text = toolTipText, FontSize = 24, Background = Brushes.LightGoldenrodYellow });
            toolTip.Content = toolTipPanel;
            tb.ToolTip = toolTip;

            toolTip.PlacementTarget = tb;
            toolTip.Placement = PlacementMode.Custom;
            toolTip.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(PositionToolTip);

            //показываем описание проблемы
            toolTip.IsOpen = true;

            //после 10 сек отображения перестаём показывать описание проблемы 
            DispatcherTimer toolTipTimer = new DispatcherTimer();
            toolTipTimer.Tick += delegate (object sender, EventArgs e)
                                 {
                                     toolTip.IsOpen = false;
                                 };

            toolTipTimer.Interval = new TimeSpan(0, 0, 15);
            toolTipTimer.Start();
        }

        private bool CheckPackageByGroupName(int packageSerialNum)
        {
            //проверяет вхождение packageSerialNum хотя бы в один интервал серийных номеров, определённых для ПЗ this.FGroupName
            //считываем описание интервала для выбранного сборочного ПЗ
            if (this.FGroupName == null)
            {
                System.Windows.Forms.MessageBox.Show(string.Concat(Properties.Resources.GroupNameIsNotValid, "."), Properties.Resources.OperationError, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

                return false;
            }
            else
            {
                List<Interval<int>> listOfInterval = new List<Interval<int>>();
                DbRoutines.IntervalsOfSerialNumsByGroupName(this.FGroupName, listOfInterval);

                if (listOfInterval.Count == 0)
                {
                    //по this.FGroupName не предполагается серийных номеров
                    return false;
                }
                else
                {
                    //проверяем, что принятый packageSerialNum входит хотя бы в один считанный интервал
                    foreach (Interval<int> interval in listOfInterval)
                    {
                        if (interval.InRange(packageSerialNum))
                            return true;
                    }

                    return false;
                }

                /*
                if (DbRoutines.IntervalOfSerialNumsByGroupName(this.FGroupName, out listOfInterval))
                {
                    //проверяем входит ли принятый packageSerialNum в считанный интервал [strartSerialNum, endSerialNum]
                    return ((packageSerialNum >= strartSerialNum) && (packageSerialNum <= endSerialNum));
                }
                else
                    return false;
                */
            }
        }

        private bool CheckPackageByGroupNameFull(TextBox tb)
        {
            int packageSerialNum;
            if (int.TryParse(tb.Text, out packageSerialNum))
            {
                if (this.CheckPackageByGroupName(packageSerialNum))
                {
                    tb.Background = Brushes.LightGreen;

                    return true;
                }
                else
                {
                    tb.Background = Brushes.LightPink;
                    CreateToolTip(tb, Properties.Resources.ValueOutOfRange);
                    tb.Focus();

                    return false;
                }
            }
            else
            {
                tb.Background = Brushes.LightPink;
                CreateToolTip(tb, Properties.Resources.ValueIsNotInteger);
                tb.Focus();

                return false;
            }
        }

        private bool IsAssemblyExistsFull(TextBox tb, bool existGood)
        {
            //existGood = true - наличие сборки это правильно
            //existGood = false - наличие сборки это не правильно
            string serialNum = tb.Text;

            if (DbRoutines.IsAssemblyExists(serialNum))
            {
                //сборка существует
                switch (existGood)
                {
                    case true:
                        //сборка существует, и она должна быть
                        tb.Background = Brushes.LightGreen;
                        return true;

                    default:
                        //сборка существует, и её не должно быть
                        tb.Background = Brushes.LightPink;
                        CreateToolTip(tb, Properties.Resources.AssemblyExists);
                        tb.Focus();
                        return false;
                }
            }
            else
            {
                //сборки не существует
                switch (existGood)
                {
                    case true:
                        //сборки не существует, и она должна быть
                        tb.Background = Brushes.LightPink;
                        CreateToolTip(tb, Properties.Resources.AssemblyExists);
                        tb.Focus();
                        return false;

                    default:
                        //сборки не существует, и её не должно быть
                        tb.Background = Brushes.LightGreen;
                        return true;
                }
            }
        }

        private string OldPartyDescr(string groupName)
        {
            //вычисляет обозначение списанной партии по старому варианту списания партии в SyteLine
            //пример: groupName="4-00021035". возвращаемый результат: "21035"

            const char delimeter = '-';
            int index = groupName.IndexOf(delimeter);

            if (index == -1)
            {
                //номнр партии извлечь из принятого groupName нельзя т.к. в нём не найден разделитель delimeter
                return null;
            }
            else
            {
                //если номер партии начинается нулями - выбрасываем их
                string lot = groupName.Substring(index + 1).TrimStart('0');

                return lot;
            }
        }

        private bool IsLotIssuedToGroupNameFull(TextBox tb)
        {
            //обозначение партии при списании может быть указано двумя вариантами:
            // старый вариант, пример: "21035"
            // новый вариант, пример: "4-00021035"
            //узнать каким способом был указан номер партии при списании нет никакой возможности кроме последовательной проверки на вариант 1 и если он был провален - проверить вариант 2 (последовательность проверки вариантов не важна)
            //если хотя бы один вариант был успешным - проверка пройдена
            const char delimeter = '/';
            int index = tb.Text.IndexOf(delimeter);

            if (index == -1)
            {
                //в поле ввода не найден delimeter - не соблюдён формат ввода
                tb.Background = Brushes.LightPink;
                CreateToolTip(tb, Properties.Resources.DescriptionWrongFormat);
                tb.Focus();

                return false;
            }
            else
            {
                //вычисляем обозначение партии по новому варианту
                string groupName = tb.Text.Substring(index + 1).TrimEnd();

                if (string.IsNullOrEmpty(groupName))
                {
                    tb.Background = Brushes.LightPink;
                    CreateToolTip(tb, Properties.Resources.DescriptionWrongFormat);
                    tb.Focus();

                    return false;
                }
                else
                {
                    //вычисляем обозначение партии по старому варианту
                    string oldPartyDescr = this.OldPartyDescr(groupName);

                    if (DbRoutines.IsLotIssuedToGroupName(groupName, this.FGroupName) || DbRoutines.IsLotIssuedToGroupName(oldPartyDescr, this.FGroupName))
                    {
                        tb.Background = Brushes.LightGreen;

                        return true;
                    }
                    else
                    {
                        tb.Background = Brushes.LightPink;
                        string lot = string.Concat("'", groupName, "', '", oldPartyDescr, "'");
                        CreateToolTip(tb, string.Format(Properties.Resources.LotNotIssuedToJob, lot, this.FGroupName));
                        tb.Focus();

                        return false;
                    }
                }
            }
        }

        private bool IsDeviceCodeUsedFull(TextBox tb)
        {
            string deviceCode = tb.Text;

            if (DbRoutines.IsDeviceCodeUsed(deviceCode))
            {
                tb.Background = Brushes.LightPink;
                CreateToolTip(tb, Properties.Resources.DeviceCodeisUsed);
                tb.Focus();

                return true;
            }
            else
            {
                tb.Background = Brushes.LightGreen;

                return false;
            }
        }

        private void SelectionChanged()
        {
            if (rbRelabeling.IsChecked ?? false)
            {
                if (dgLinks.CurrentItem is RecordOfAssembly recordOfAssembly)
                    tbOldPackageSerialNum.Text = recordOfAssembly.PackageSerialNum;
            }

            if (rbAssembly.IsChecked ?? false)
            {
                if (Common.Routines.IsUserCanEditAssembly(((MainWindow)Application.Current.MainWindow).FPermissionsLo))
                {
                    //если пользователю разрешено редактирование сборок - будем считывать значения реквизитов для редактирования в соответствующие поля
                    if (dgLinks.CurrentItem is RecordOfAssembly recordOfAssembly)
                    {
                        tbOldPackageSerialNum.Text = recordOfAssembly.PackageSerialNum;
                        tbDeviceCode.Text = recordOfAssembly.DeviceCode;
                        tbDeviceCode2.Text = recordOfAssembly.DeviceCode2;
                    }
                }
            }
        }

        private void dgLinks_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            this.SelectionChanged();
        }

        private void dgLinks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectionChanged();
        }

        private void dgLinks_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void mnuPrintClick(object sender, RoutedEventArgs e)
        {
            List<RecordOfAssembly> listData = this.dgLinks.Items.Cast<RecordOfAssembly>().ToList();

            string description;
            DbRoutines.ItemByGroupName(this.FGroupName, out description);

            string realizedByJob = lbData1ByGroupName.Content.ToString();
            string linksCount = string.Format(Properties.Resources.ShownLinksCount, listData.Count, this.FListData.Count, Properties.Resources.Pieces);
            string packageInfo = lbData2ByGroupName.Content.ToString().Replace("\r\n", ". ");

            string title = string.Concat(
                                          this.Title, ".", "\r\n",
                                          string.Format("{0} {1}, {2}. {3}", Properties.Resources.GroupName, this.FGroupName, description, realizedByJob), "\r\n",
                                          string.Format("{0} {1}", linksCount, packageInfo)
                                        );

            Printer printer = new Printer();
            printer.printDataGrid(listData, dgLinks, title);
        }

        private void SetWhiteBackgroundIfTextIsEmpty(object sender)
        {
            TextBox tb = (TextBox)sender;

            if (tb != null)
            {
                if (tb.Text == string.Empty)
                    tb.Background = Brushes.White;
            }
        }

        private void tbOldPackageSerialNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetWhiteBackgroundIfTextIsEmpty(sender);
        }

        private void tbNewPackageSerialNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetWhiteBackgroundIfTextIsEmpty(sender);
        }

        private void tbDeviceCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetWhiteBackgroundIfTextIsEmpty(sender);
        }

        private void tbDeviceCode2_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetWhiteBackgroundIfTextIsEmpty(sender);
        }

    }

    public class Printer
    {
        private void makeColumn(Table table, TableRow row, string columnTittle)
        {
            if ((table != null) && (row != null))
            {
                TableColumn tableColumn = new TableColumn();
                tableColumn.Width = new GridLength(this.columnWidthByColumnTittle(columnTittle), GridUnitType.Pixel);
                table.Columns.Add(tableColumn);

                Run run = new Run(columnTittle);
                Paragraph paragraph = new Paragraph(run);
                TableCell tableCell = new TableCell(paragraph);
                row.Cells.Add(tableCell);

                tableCell.Padding = new Thickness(4);
                tableCell.BorderBrush = Brushes.Black;
                tableCell.FontWeight = FontWeights.Bold;
                tableCell.Background = Brushes.LightGray;
                tableCell.Foreground = Brushes.White;
                tableCell.BorderThickness = new Thickness(1, 1, 1, 1);
            }
        }

        private void makeRow(TableRow row, string value)
        {
            TableCell tableCell = new TableCell(new Paragraph(new Run(value)));
            row.Cells.Add(tableCell);

            tableCell.Padding = new Thickness(4);
            tableCell.BorderBrush = Brushes.DarkGray;
            tableCell.BorderThickness = new Thickness(1, 1, 1, 1);
        }

        private double columnWidthByColumnTittle(string columnTittle)
        {
            switch (columnTittle)
            {
                case "№":
                    return 40;

                case "Корпус":
                    return 100;

                case "ППЭ":
                    return 125;

                case "ППЭ2":
                    return 125;

                case "Дата":
                    return 125;

                case "Сборщик":
                    return 110;

                case "Старый серийный номер":
                    return 128;

                default:
                    return 0;
            }
        }

        public void printDataGrid(List<RecordOfAssembly> data, DataGrid dataGrid, string title)
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() ?? false)
            {
                FlowDocument fd = new FlowDocument();
                fd.TextAlignment = TextAlignment.Center;
                fd.PageWidth = printDialog.PrintableAreaWidth;
                fd.PageHeight = printDialog.PrintableAreaHeight;
                fd.ColumnWidth = printDialog.PrintableAreaWidth;
                fd.BringIntoView();

                Paragraph p = new Paragraph(new Run(title));
                p.FontStyle = dataGrid.FontStyle;
                p.FontFamily = dataGrid.FontFamily;
                p.FontSize = 18;
                fd.Blocks.Add(p);

                Table table = new Table();
                table.BorderBrush = Brushes.Gray;
                table.BorderThickness = new Thickness(1, 1, 0, 0);
                table.FontStyle = dataGrid.FontStyle;
                table.FontFamily = dataGrid.FontFamily;
                table.FontSize = 13;
                table.CellSpacing = 0;
                fd.Blocks.Add(table);

                TableRowGroup tableRowGroup = new TableRowGroup();
                table.RowGroups.Add(tableRowGroup);

                TableRow r = new TableRow();
                tableRowGroup.Rows.Add(r);

                List<string> headerList = dataGrid.Columns.Select(e => e.Header.ToString()).ToList();

                //столбец с порядковым номером строки
                this.makeColumn(table, r, "№");

                for (int j = 0; j < headerList.Count; j++)
                {
                    this.makeColumn(table, r, headerList[j]);
                }

                for (int i = 0; i < data.Count; i++)
                {
                    RecordOfAssembly rec = data[i];

                    tableRowGroup = new TableRowGroup();
                    r = new TableRow();

                    //выводим порядковый номер записи
                    this.makeRow(r, (i + 1).ToString());

                    for (int j = 0; j < dataGrid.Columns.Count; j++)
                    {
                        string value = rec.ValueByIndex(j);
                        this.makeRow(r, value);
                    }

                    tableRowGroup.Rows.Add(r);
                    table.RowGroups.Add(tableRowGroup);
                }

                printDialog.PrintDocument(((IDocumentPaginatorSource)fd).DocumentPaginator, "");
            }
        }
    }
}
