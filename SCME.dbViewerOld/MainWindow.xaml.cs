using SCME.dbViewer.ForParameters;
using SCME.dbViewer.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public const int cDevID =      0;
        public const int cProfileID =  1;
        public const int cTSZeroTime = 2;
        public const int cGroupName =  3;
        public const int cCode =       4;
        public const int cSilN1 =      5;
        public const int cSilN2 =      8;
        public const int cParamType =  9;

        private SqlConnection Connection = null;

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

            CreateDeviceColumns();
            KeyPreview();
            Connection = CreateConnection();
        }

        private int DevID(object[] itemArray)
        {
            return int.Parse(itemArray?[cDevID].ToString());
        }

        private string ProfileID(object[] itemArray)
        {
            return itemArray?[cProfileID].ToString();
        }

        private DateTime TSZeroTime(object[] itemArray)
        {
            return DateTime.Parse(itemArray?[cTSZeroTime].ToString());
        }

        private string GroupName(object[] itemArray)
        {
            return itemArray?[cGroupName].ToString();
        }

        private string Code(object[] itemArray)
        {
            return itemArray?[cCode].ToString();
        }

        private string SilN1(object[] itemArray)
        {
            return itemArray?[cSilN1].ToString();
        }

        private string SilN2(object[] itemArray)
        {
            return itemArray?[cSilN2].ToString();
        }

        private string ParamType(object[] itemArray)
        {
            return itemArray?[cParamType].ToString();
        }

        private SqlConnection CreateConnection()
        {
            string strCon = "server=192.168.0.134, 1444;uid=sa;pwd=Hpl1520; database=SCME_ResultsDB";

            return new SqlConnection(strCon);
        }

        public void KeyEventHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                LoadDevices();

            if (sender is DatePicker)
            {
                if ((e.Key == Key.Delete) || (e.Key == Key.Back) || (e.Key == Key.Escape))
                {
                    DatePicker dt = (DatePicker)sender;
                    dt.SelectedDate = null;
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

        private void CreateColumns()
        {
            CreateDeviceColumns();
        }

        private void CreateDeviceColumns()
        {
            dgDevices.ClearColumns();

            DataGridColumn column = dgDevices.NewColumn("Идентификатор изделия", "DEV_ID");
            column.Visibility = Visibility.Collapsed;

            column = dgDevices.NewColumn("Идентификатор профиля", "PROFILE_ID");
            column.Visibility = Visibility.Collapsed;

            column = dgDevices.NewColumn("Дата измерений с нулевым временем", "TSZEROTIME");
            column.Visibility = Visibility.Collapsed;

            dgDevices.NewColumn("Номер ПЗ", "GROUP_NAME");
            dgDevices.NewColumn("Сер. номер прибора", "CODE");
            dgDevices.NewColumn("Партия ППЭ", "SIL_N_1");
            dgDevices.NewColumn("Номер ППЭ", "SIL_N_2");
            dgDevices.NewColumn("Дата измерений", "TS");
            dgDevices.NewColumn("Профиль", "PROF_NAME");
            dgDevices.NewColumn("Тип прибора", "DEVICETYPE");
        }

        private void LoadDevices()
        {
            string SqlText = "SELECT x.DEV_ID, x.PROFILE_ID, x.TSZEROTIME, x.GROUP_NAME, x.CODE, x.SIL_N_1, x.SIL_N_2, x.TS, x.PROF_NAME, x.DEVICETYPE" +
                             " FROM" + 
                             " (" + 
                                "SELECT D.DEV_ID, D.PROFILE_ID, dbo.DateTimeToDateZeroTime(D.TS) AS TSZEROTIME, G.GROUP_NAME, D.CODE, D.SIL_N_1, D.SIL_N_2, D.TS, P.PROF_NAME, dbo.DeviceTypeByProfileName(P.PROF_NAME) AS DEVICETYPE" +
                                " FROM GROUPS G" +
                                " INNER JOIN DEVICES AS D ON G.GROUP_ID=D.GROUP_ID" +
                                " INNER JOIN PROFILES AS P ON P.PROF_GUID=D.PROFILE_ID" +
                                " WHERE ISNULL(P.IS_DELETED, 0)=0" +
                              " ) AS x";

            string dateBeg = null;
            if (dpBegin.SelectedDate != null)
                dateBeg = dpBegin.SelectedDate.Value.Date.ToShortDateString();

            string dateEnd = null;
            if (dpEnd.SelectedDate != null)
                dateEnd = dpEnd.SelectedDate.Value.Date.ToShortDateString();

            SqlText += ((dateBeg != null) || (dateEnd != null)) ? " WHERE" : "";

            if (dateBeg != null)
                SqlText += string.Format(" x.TSZEROTIME>='{0}'", dateBeg);

            if (dateEnd != null)
            {
                if (dateBeg != null)
                    SqlText += " AND";

                SqlText += string.Format(" x.TSZEROTIME<='{0}'", dateEnd);
            }

            dgDevices.ViewSqlResult(Connection, SqlText);
        }

        static void DispatcherUnhandledException(object Sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs E)
        {
            MessageBox.Show(E.Exception.ToString(), "Unhandled exception");
        }

        private static void CurrentDomainOnUnhandledException(object Sender, UnhandledExceptionEventArgs Args)
        {
            MessageBox.Show(Args.ExceptionObject.ToString(), "Unhandled exception");
        }

        private void dpBeg_CalendarClosed(object sender, RoutedEventArgs e)
        {
            LoadDevices();
        }

        private void dpEnd_CalendarClosed(object sender, RoutedEventArgs e)
        {
            LoadDevices();
        }

        private void dgDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Connection == null)
                Connection = CreateConnection();

            DeviceParameters parameters = FindResource("parameters") as DeviceParameters;
            DataGrid dg = (DataGrid)sender;
            DataRowView row = dg.SelectedItem as DataRowView;

            if (row != null)
            {
                object[] itemArray = row.Row.ItemArray;
                string paramType = ParamType(itemArray);

                Parameters parameters = new Parameters();
                parameters.

                switch (p == null)
                {
                    case true:
                        parameters.Clear();
                        break;

                    default:
                        parameters.add(p);
                        p.Load(Connection, DevID(itemArray), ProfileID(itemArray), GroupName(itemArray), Code(itemArray), SilN1(itemArray), SilN2(itemArray));
                        break;
                }
            }
        }

        private void btReport_Click(object sender, RoutedEventArgs e)
        {
            if ((dgDevices.ItemsSource == null) || (dgDevices.Items.Count == 0))
            {
                MessageBox.Show("Список изделий для построения отчёта пуст. Формирование отчёта не имеет смысла.", "Нет данных для построения отчёта", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ReportByDevices report = new ReportByDevices();

            if (Connection.State != ConnectionState.Open)
                Connection.Open();

            try
            {
                foreach (System.Data.DataRowView drv in dgDevices.ItemsSource)
                {
                    object[] itemArray = drv.Row.ItemArray;

                    int devID = DevID(itemArray);   //int.Parse(drv[cDevID].ToString());
                    string profileID = ProfileID(itemArray); //drv[cProfileID].ToString();
                    string groupName = GroupName(itemArray); // drv[cGroupName].ToString();
                    string code = Code(itemArray);  //drv[cCode].ToString();
                    string silN1 = SilN1(itemArray); //drv[cSilN1].ToString();
                    string silN2 = SilN2(itemArray);  //drv[cSilN2].ToString();
                    string paramType = ParamType(itemArray); //drv[cParamType].ToString();

                    Parameters p = NewParameters(paramType);

                    if (p != null)
                    {
                        report.Add(p);
                        p.Load(Connection, devID, profileID, groupName, code, silN1, silN2);
                    }
                }
            }

            finally
            {
                Connection.Close();
            }

            //выводим отчёт в Excel
            report.ToExcel();
        }

    }

    public class DeviceParameters : ObservableCollection<Parameters>
    {
        public ObservableCollection<Parameters> list
        {
            get
            {
                return this;
            }
        }

        public void add(Parameters p)
        {
            //в этом списке всегда будет один единственный параметр
            this.Clear();
            this.Add(p);
        }

        private SqlConnection CreateConnection()
        {
            string strCon = "server=192.168.0.134, 1444;uid=sa;pwd=Hpl1520; database=SCME_ResultsDB";

            return new SqlConnection(strCon);
        }
    }
}
