using System.Windows;
using SCME.CustomControls;
using SCME.dbViewer.ForParameters;

namespace SCME.dbViewer
{
    public static class AssemblyProtocolReport
    {
        public delegate string SaveAssemblyProtocol();
        private static string BuildAssemblyReportInExcel(SaveAssemblyProtocol saveAssemblyProtocolHandler, double systemScale, int itav, int deviceTypeID, string constructive, string sDeviceClass, string modification, ReportData reportData)
        {
            //формируем в Excel протокол сборки
            //возвращает:
            // null - отчёт успешно сформирован;
            // не null - описание причины отказа в формировании отчёта;

            string result = null;

            //проверяем введённые пользователем значения:
            //конструктив
            //может быть задан как символом "X" так и числом в интервале [1, 999]
            if (int.TryParse(constructive, out int iConstructive))
            {
                //задан числом
                if (!((iConstructive >= 1) && (iConstructive <= 999)))
                {
                    result = string.Concat(Properties.Resources.Constructive, ". ", Properties.Resources.OutOfRange, ": [1, 999].");

                    return result;
                }
            }
            else
            {
                //допустим только символ "X"
                if (constructive != "X")
                {
                    result = string.Concat(Properties.Resources.Constructive, ". ", Properties.Resources.WrongDescription, ".");

                    return result;
                }
            }

            //проверяем средний ток
            if (!((itav >= 1) && (itav <= 99999)))
            {
                result = string.Concat(Properties.Resources.AverageCurrent, ". ", Properties.Resources.OutOfRange, ": [1, 99999].");

                return result;
            }

            //проверяем класс
            if (int.TryParse(sDeviceClass, out int deviceClass))
            {
                if (!((deviceClass >= 1) && (deviceClass <= 100)))
                {
                    result = string.Concat(Properties.Resources.DeviceClass, ". ", Properties.Resources.OutOfRange, ": [1, 100].");

                    return result;
                }
            }
            else
            {
                result = string.Concat(Properties.Resources.DeviceClass, ". ", Properties.Resources.WrongDescription, ".");

                return result;
            }

            //чтобы не получилось, что пользователь сформировал отчёт по не сохранённому протоколу сборки - принудительно выполняем сохранение протокола сборки
            // и если сохранение было успешным - только тогда формируем отчёт
            result = saveAssemblyProtocolHandler?.Invoke();

            if (result == null)
                reportData.AssemblyReportToExcel(systemScale, itav, deviceTypeID, constructive, modification);

            return result;
        }

        public static void Build(int assemblyProtocolID, SaveAssemblyProtocol saveAssemblyProtocolHandler, double systemScale, int assemblyReportRecordCount, string assemblyJob, string deviceDescr, string deviceTypeRU, string omnity, string tq, string trr, string qrr, string dUdt, string tgt, int itav, int deviceTypeID, string constructive, string modification, string deviceClass)
        {
            //формирование отчёта по протоколу сборки
            //в нулевом элементе списка будем хранить значения ревизитов установленных пользователем для данного протокола сборки
            DynamicObj row = Routines.UserPropertiesOfAssemblyProtocol(assemblyProtocolID, assemblyReportRecordCount, assemblyJob, deviceDescr, deviceTypeRU, omnity, tq, trr, qrr, dUdt, tgt);

            if (row != null)
            {
                ReportData reportData = new ReportData();

                //под индексом 0 сохраняем в reportData запись с данными, которые установил пользователь в шапке протокола сборки
                ReportRecord reportRecord = new ReportRecord(reportData, row);
                reportData.Add(reportRecord);

                string errorDescription = null;

                switch (saveAssemblyProtocolHandler == null)
                {
                    case true:
                        //данные в reportData загружаются без использования кеша (данные для его построения считываются из базы данных напрямую в reportData без использования кеша, сохранение данных формируемого отчёта в этом режиме его формирования не может быть в принципе)
                        errorDescription = reportData.LoadFromDataBase(assemblyProtocolID, out int recordCount);

                        //запоминаем количество записей в отчёте (оно будет выведено в самом низу отчёта)
                        row.SetMember(Constants.AssemblyReportRecordCount, recordCount);
                        break;

                    default:
                        //данные в reportData загружаются с использованием механизмов кеша (интерактивный режим формирования отчёта - пользователь может выбирать параметры формирования отчёта, которые при его формировании будут сохранены в базу данных)
                        errorDescription = reportData.Fill(assemblyReportRecordCount);
                        break;
                }

                if (errorDescription != null)
                {
                    MessageBox.Show(errorDescription, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                errorDescription = BuildAssemblyReportInExcel(saveAssemblyProtocolHandler, systemScale, itav, deviceTypeID, constructive, deviceClass, modification, reportData);
                if (errorDescription != null)
                {
                    MessageBox.Show(errorDescription, Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
        }
    }
}
