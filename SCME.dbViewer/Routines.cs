using SCME.CustomControls;
using SCME.Types.BaseTestParams;
using SCME.Types.Profiles;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Runtime.Serialization;
using System.Linq;
using System.Xml;
using SCME.Types;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Concurrent;
using System.Windows;
using System.IO;
using System.Windows.Controls.Primitives;

namespace SCME.dbViewer
{
    public static class Routines
    {
        public const string PartOfAssemblyProtocolFileName = "AssemblyReport";
        public const string PartOfCasualReportFileName = "CasualReport";

        public static string User()
        {
            //чтение залогоненного в системе пользователя
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\')[1].ToString();
        }

        public static string EnvironmentVariableTempValue()
        {
            //возвращает значение переменной среды 'Temp'
            return System.IO.Path.GetTempPath();
        }

        public static bool IsFileLocked(string fileFullAddress)
        {
            //проверяет занят файл file или свободен

            FileInfo file = new FileInfo(fileFullAddress);

            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //файл не доступен:
                //возможно в данный момент в него происходит запись
                //или он открыт в другом потоке
                //или его не существует
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //файл не заблокирован - свободен
            return false;
        }

        public static void ClearOldReports(string path, string partOfName)
        {
            //удаление файлов в директории path при условии, что имя файла содержит partOfName

            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Name.Contains(partOfName) && !IsFileLocked(file.FullName))
                    file.Delete();
            }
        }

        public static bool Contains(this string source, string value)
        {
            //проверяет вхождение подстроки value в строку source
            return source?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static double SystemScale(System.Windows.Media.Visual visual)
        {
            //считывает из операционной системы значение масштаба (пример: 1, 1.25, 1.5)
            PresentationSource source = PresentationSource.FromVisual(visual);

            if (source != null)
            {
                return source.CompositionTarget.TransformToDevice.M11;
                //dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }

            return 1;
        }

        public static DynamicObj GetPlaceHolder(dynamic item, int page, int offset)
        {
            //принятый item предлагается заполнить данными
            return item;

            //return new Device { Name = "W [" + page + "/" + offset + "]" };
        }

        public static int ConditionsFirstIndex(DynamicObj row)
        {
            //узнаём начальный индекс conditions
            if ((row != null) && row.GetMember(Constants.ConditionsInDataSourceFirstIndex, out object valueConditionsInDataSourceFirstIndex))
            {
                if ((valueConditionsInDataSourceFirstIndex != null) && int.TryParse(valueConditionsInDataSourceFirstIndex.ToString(), out int conditionsStartIndex))
                    return conditionsStartIndex;
            }

            return -1;
        }

        public static int ParametersFirstIndex(DynamicObj row)
        {
            //узнаём начальный индекс parameters
            if ((row != null) && row.GetMember(Constants.ParametersInDataSourceFirstIndex, out object valueParametersInDataSourceFirstIndex))
            {
                if ((valueParametersInDataSourceFirstIndex != null) && int.TryParse(valueParametersInDataSourceFirstIndex.ToString(), out int parametersStartIndex))
                    return parametersStartIndex;
            }

            return -1;
        }

        public static TemperatureCondition TemperatureConditionByTemperature(double temperatureValue)
        {
            return (temperatureValue > 25) ? TemperatureCondition.TM : TemperatureCondition.RT;
        }

        public static char SystemDecimalSeparator()
        {
            return Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }

        public static string PCName(string value, out int? index)
        {
            //вид принимаемой на вход строки value 'SL_ITM2'
            //разделяет принятую строку value на строку (result='SL_ITM') и число (index=2)

            string result = null;
            index = null;

            string sIndex = null;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                switch (char.IsNumber(c))
                {
                    case true:
                        sIndex = string.IsNullOrEmpty(sIndex) ? c.ToString() : string.Concat(sIndex, c.ToString());
                        break;

                    default:
                        result = string.IsNullOrEmpty(result) ? c.ToString() : string.Concat(result, c.ToString());
                        break;
                }
            }

            if (!string.IsNullOrEmpty(sIndex))
                if (int.TryParse(sIndex, out int ind))
                    index = ind;

            return result;
        }

        public static bool IsInteger(string value, out int iValue, out bool isDouble, out double dValue)
        {
            //если value есть целое число - вернёт true, иначе false
            //в isDouble вернёт признак того, что принятый value успешно преобразуется к типу double
            NumberStyles styles = NumberStyles.Integer | NumberStyles.AllowDecimalPoint;
            bool result = int.TryParse(value, styles, System.Globalization.CultureInfo.InvariantCulture, out iValue);

            if (result)
            {
                isDouble = false;
                dValue = 0;
            }
            else
                isDouble = double.TryParse(value, out dValue);

            return result;

            /*
            if (isDouble)
                return Math.Abs(dValue % 1) <= (double.Epsilon * 100);
            else
                return false;
            */
        }

        public static bool IsBoolean(string value)
        {
            //используется для проверки описания норм на измеряемые параметры
            return ((value == "0") || (value == "1"));
        }

        public static string VtoU(string name)
        {
            //если первый символ имени начинается на V - заменяем его на U 
            if ((name != null) && (name.Substring(0, 1).ToUpper() == "V"))
            {
                return string.Concat('U', name.Remove(0, 1));
            }
            else
                return name;
        }

        public static byte ComparisonToByte(string value)
        {
            switch (value)
            {
                case "=":
                case " IS ":
                    return 0;

                case "<":
                    return 1;

                case "<=":
                    return 2;

                case ">":
                    return 3;

                case ">=":
                    return 4;

                case " LIKE ":
                    return 5;

                default:
                    return 0;
            }
        }

        public static string CalcDeviceDescr(string deviceTypeRU, string deviceTypeEN, bool export, string constructive, string dUDt, string tq, string trr, string tgt, string modification, string climatic, string averageCurrent, string deviceClass)
        {
            //строим обозначение изделия-результата для протокола сборки
            string calculatedValue = string.Empty;
            string deviceType = export ? deviceTypeEN : deviceTypeRU;

            switch (deviceTypeRU)
            {
                case "Т":
                case "МТ":
                case "МТД":
                case "ТЛ":
                    calculatedValue = string.Concat(dUDt, tq);
                    break;

                case "Д":
                case "ДЛ":
                case "МД":
                case "МДТ":
                case "МДЧ":
                    calculatedValue = string.Empty;
                    break;

                case "ДЧ":
                case "ДЧЛ":
                case "МДЧЛ":
                    calculatedValue = trr;
                    break;

                case "ТБ":
                case "ТБИ":
                case "ТБЧ":
                case "МТБ":
                case "МТБЧ":
                    calculatedValue = string.Concat(dUDt, tq, tgt);
                    break;
            }

            //нам нужен вот такой вид результата: "{0}{1}-{2}-{3}-{4} {5}"

            //2
            if (!string.IsNullOrEmpty(averageCurrent))
                averageCurrent = string.Concat("-", averageCurrent);

            //3
            if (!string.IsNullOrEmpty(deviceClass))
                deviceClass = string.Concat("-", deviceClass);

            //4
            if (!string.IsNullOrEmpty(calculatedValue))
                calculatedValue = string.Concat("-", calculatedValue);

            //5
            string calculatedClimatic = string.Empty;

            switch (deviceTypeRU)
            {
                case "МТ":
                case "МД":
                case "МТД":
                case "МДТ":
                case "МДЧЛ":
                    calculatedClimatic = string.Concat(modification, "-", climatic);
                    break;

                default:
                    calculatedClimatic = climatic;
                    break;
            }

            string result = string.Format("{0}{1}{2}{3}{4}-{5}", deviceType, constructive, averageCurrent, deviceClass, calculatedValue, calculatedClimatic);

            return result;
        }

        public static DynamicObj UserPropertiesOfAssemblyProtocol(int assemblyProtocolID, int assemblyReportRecordCount, string assemblyJob, string deviceDescr, string deviceTypeRU, string omnity, string tq, string trr, string qrr, string dUdt, string tgt)
        {
            //assemblyReportRecordCount - сколько записей содержит формируемый отчёт
            //запоминаем в возвращаемом результате значения реквизитов, которые пользователь установил для протокола сборки (по которому формируется отчёт)
            //возвращает:
            // DynamicObj - данная реализация успешно запомнила реквизиты протокола сборки в возращаемом DynamicObj;
            // Null - данная реализация не смогла запомнить реквизиты протокола сборки - обнаружила ошибки в сохраняемых данных

            DynamicObj result = new DynamicObj();

            string packageType = null;
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

            //запоминаем тип корпуса
            result.SetMember(Constants.PackageType, packageType);

            //запоминаем идентификатор протокола сборки
            result.SetMember(Constants.AssemblyProtocolID, assemblyProtocolID);

            //запоминаем количество записей в отчёте
            result.SetMember(Constants.AssemblyReportRecordCount, assemblyReportRecordCount);

            //запоминаем сборочное ПЗ
            result.SetMember(Constants.AssemblyJob, assemblyJob);

            //запоминаем обозначение изделия
            result.SetMember(Constants.Device, deviceDescr);

            //запоминаем RU обозначение типа изделия
            result.SetMember(Constants.DeviceTypeRu, deviceTypeRU);

            //запоминаем значение КОФ
            result.SetMember(Constants.Omnity, omnity);

            //запоминаем значение Tq
            result.SetMember(Constants.Tq, tq);

            //запоминаем значение Trr
            result.SetMember(Constants.Trr, trr);

            //запоминаем значение Qrr
            result.SetMember(Constants.Qrr, qrr);

            //запоминаем значение dUdt
            result.SetMember(Constants.dUdt, dUdt);

            //запоминаем значение Tgt
            result.SetMember(Constants.Tgt, tgt);

            return result;
        }

        public static IEnumerable<string> DelimetedStringsToEnumerable(string delimetedStrings, string delimeter)
        {
            //преобразование строки являющейся множеством строк записанных через разделитель delimeter в IEnumerable<string>
            return delimetedStrings.Split(new[] { delimeter }, StringSplitOptions.None) as IEnumerable<string>;
        }

        public static string RemoveDuplicates(string stringWithDuplicates)
        {
            //удаляет дубликаты из принятой строки stringWithDuplicates
            if ((stringWithDuplicates != null) && (stringWithDuplicates != string.Empty))
            {
                IEnumerable<string> strings = DelimetedStringsToEnumerable(stringWithDuplicates, SCME.Common.Constants.cString_AggDelimeter.ToString());
                strings = strings.Where(x => x != string.Empty).Distinct();

                return string.Join(Constants.DelimeterForStringConcatenate.ToString(), strings.ToArray());
            }

            return stringWithDuplicates;
        }

        public static string RemoveEmptyValuesAndDuplicates(string stringWithDuplicates)
        {
            //удаляет значения "EMPTY" и дубликаты из принятой строки stringWithDuplicates
            if ((stringWithDuplicates != null) && (stringWithDuplicates != string.Empty))
            {
                IEnumerable<string> strings = DelimetedStringsToEnumerable(stringWithDuplicates, SCME.Common.Constants.cString_AggDelimeter.ToString());
                strings = strings.Where(x => (x != string.Empty) && (x != "EMPTY")).Distinct();

                return string.Join(Constants.DelimeterForStringConcatenate.ToString(), strings.ToArray());
            }

            return stringWithDuplicates;
        }

        public static string StringByIndex(string delimetedStrings, string delimeter, int index)
        {
            //рассматривает строку delimetedStrings как перечисление строк через разделитель delimeter
            //возвращает элемент последовательности с индексом index
            if (string.IsNullOrEmpty(delimetedStrings))
                return null;

            List<string> strings = DelimetedStringsToEnumerable(delimetedStrings, delimeter).ToList();

            string result = strings[index];

            return result;
        }

        public static string FirstInList(string delimetedStrings, string delimeter)
        {
            string result = StringByIndex(delimetedStrings, delimeter, 0);

            return result;
        }

        public static string ChangeString_AggDelimeterToDelimeter(string groupedStrings, string delimeter)
        {
            //если принятый groupedStrings=null - вернёт null
            return groupedStrings?.Replace(SCME.Common.Constants.cString_AggDelimeter.ToString(), delimeter);
        }

        public static string ChangeString_AggDelimeterToDelimeter(string groupedStrings, char delimeter)
        {
            //если принятый groupedStrings=null - вернёт null
            return groupedStrings?.Replace(SCME.Common.Constants.cString_AggDelimeter, delimeter);
        }

        public static string MinValue(string value)
        {
            //рассматривает принятую строку value как строки, написанные через разделитель Constants.cString_AggDelimeter и вычисляет среди них минимальное значение
            string result = null;

            if (value != null)
            {
                IEnumerable<string> strings = DelimetedStringsToEnumerable(value, SCME.Common.Constants.cString_AggDelimeter.ToString());
                result = strings.Where(x => x != string.Empty).Distinct().Min();
            }

            return result;
        }

        public static string NrmDescr(object minValue, object maxValue)
        {
            //формирует строковое описание норм
            //если граница нормы задана, то она всегда входит в диапазон, т.е. формируем строку вида [min, max]
            //возвращает:
            // null - обе границы норм не заданы;
            // (-∞, maxValue] либо [minValue, +∞) - задана одна из границ нормы;
            // [minValue, maxValue] - заданы обе границы
            string result = null;

            if ((minValue != null) || (maxValue != null))
            {
                result = (minValue == null) ? "(-∞, " : string.Concat("[", minValue, ", ");
                result += (maxValue == null) ? "+∞)" : string.Concat(maxValue, "]");
            }

            return result;
        }

        public static string NrmDescr(string name, object minValue, object maxValue)
        {
            string result = null;

            if (!string.IsNullOrEmpty(name))
            {
                if ((minValue != null) && (maxValue != null))
                {
                    result = string.Format("{0}≤{1}≤{2}", minValue, name, maxValue);
                }
                else
                {
                    //или minValue=null или maxValue=null, либо они оба равны null
                    if ((minValue != null) || (maxValue != null))
                    {
                        if (minValue == null)
                            result = string.Format("{0}≤{1}", name, maxValue);

                        if (maxValue == null)
                            result = string.Format("{0}≤{1}", minValue, name);
                    }
                }
            }

            return result;
        }

        /*
        public static string DescrByTemperatureMode(string temperatureModes, string values)
        {
            //по принятым temperatureMode (RT;TM) и value (20;19) вычисляет строку вида RT:20;TM:19
            string result = null;

            if ((temperatureModes != null) && (temperatureModes != string.Empty) && (values != null) && (values != string.Empty))
            {
                IEnumerable<string> temperatureModeStrings = DelimetedStringsToEnumerable(temperatureModes, SCME.Common.Constants.cString_AggDelimeter.ToString());
                IEnumerable<string> valueStrings = DelimetedStringsToEnumerable(values, SCME.Common.Constants.cString_AggDelimeter.ToString());

                if (temperatureModeStrings.Count() == valueStrings.Count())
                {
                    //принимаемые строки должны иметь одинаковое количество значений, разделённых разделителями
                    for (int i = 0; i < temperatureModeStrings.Count(); i++)
                    {
                        if (result != null)
                            result = string.Concat(result, Constants.DelimeterForStringConcatenate);

                        result = string.Concat(result, temperatureModeStrings.ElementAt(i), ":", valueStrings.ElementAt(i));
                    }
                }
            }

            return result;
        }
        */

        /*
        public static string CalcTotalStatus(string temperatureModes, string statuses)
        {
            //вычисление итогового статуса по принятой последовательности статусов, написанных через разделитель Constants.cString_AggDelimeter
            if ((statuses != null) && (statuses != string.Empty))
            {
                IEnumerable<string> statusStrings = DelimetedStringsToEnumerable(statuses, SCME.Common.Constants.cString_AggDelimeter.ToString());

                //если есть хотя-бы один не OK - результат Fault
                if (statusStrings.Where(x => (x != Constants.GoodSatatus)).Count() != 0)
                {
                    return Constants.FaultSatatus;
                }
                else
                {
                    //все статусы в принятой строке statuses есть OK, чтобы вычислить результат в OK необходимо убедится, что эти статусы получены при температурных режимах и RT и TM
                    //если нет хотя-бы одного из температурных режимов (нет RT или TM) - возвращаем не определённый статус - пустую строку
                    return ((temperatureModes.IndexOf("RT", StringComparison.OrdinalIgnoreCase) == -1) || (temperatureModes.IndexOf("TM", StringComparison.OrdinalIgnoreCase) == -1)) ? string.Empty : Constants.GoodSatatus;
                }
            }

            return Constants.FaultSatatus;
        }
        */

        private static Type TypeOfValue(string value, out string correctedValue)
        {
            //вычисляет тип принятого value по принципу: всё что не double - то string
            //не будем ограничивать пользователя в выборе системного разделителя дробной части от целой части - вернём в correctedValue корректное значение для любого значения системного разделителя дробной части от целой части
            bool parsedAsDouble = double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out double d);
            correctedValue = parsedAsDouble ? d.ToString() : value;

            return parsedAsDouble ? typeof(double) : typeof(string);
        }

        private static string ColumnNameInDataGrid(string temperatureCondition, string test, string name)
        {
            //строит имя параметра для использования в DataGrid по принятым temperatureCondition, test и name
            //построение имени параметра с учётом его принадлежности к температурному режиму
            return string.Format("{0}/{1}{2}{3}", temperatureCondition, test, Constants.StringDelimeter, name);
        }

        public static string ColumnNameInDataSource(string temperatureCondition, string test, Common.Routines.XMLValues subject, string name)
        {
            //строит имя условия/параметра для использования в DataSource по принятым temperatureCondition и name
            string subj = (subject == Common.Routines.XMLValues.ManuallyParameters) ? "Parameters" : subject.ToString();

            return string.Format("{0}{1}{2}{3}{4}", temperatureCondition, test, subj, SCME.Common.Constants.FromXMLNameSeparator, name);
        }

        public static int? MinDeviceClass(int? value1, int? value2)
        {
            int? result;

            if ((value1 == null) && (value2 == null))
            {
                //оба значения равны null, сравнивать нечего
                result = null;
            }
            else
            {
                if ((value1 != null) && (value2 != null))
                {
                    //оба значения не null
                    result = Math.Min((int)value1, (int)value2);
                }
                else
                {
                    //одно из значений не null, а другое null
                    result = null;
                }
            }

            return result;
        }

        public static object MaxInt(object value1, object value2)
        {
            object result;

            if ((value1 == DBNull.Value) && (value2 == DBNull.Value))
            {
                //оба значения равны null, сравнивать нечего
                result = DBNull.Value;
            }
            else
            {
                if ((value1 != DBNull.Value) && (value2 != DBNull.Value))
                {
                    //оба значения не null
                    result = Math.Max((int)value1, (int)value2);
                }
                else
                {
                    //одно из значений не null, а другое null
                    result = (value1 == DBNull.Value) ? value2 : value1;
                }
            }

            return result;
        }

        public static string EndingNumber(string value)
        {
            //читает из принятого value цифры до первой встреченной не цифры двигаясь с конца value к её началу
            string number = System.Text.RegularExpressions.Regex.Match(value, @"\d+$", System.Text.RegularExpressions.RegexOptions.RightToLeft).ToString();

            return number;
        }

        public static int EndingNumberFromValue(string value)
        {
            //читает число, которое value содержит в конце себя
            //если value заканчивается числом - возвращает это число, иначе возвращает 1 
            System.Text.RegularExpressions.Match result = System.Text.RegularExpressions.Regex.Match(value, @"\d+$", System.Text.RegularExpressions.RegexOptions.RightToLeft);

            return result.Success ? Convert.ToInt32(result.Value) : 1;
        }


        /*
                public static bool ValueAsDouble(string value, out double correctedValue)
                {
                    return double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out correctedValue);
                }
        */
        public static bool ValueAsDouble(string value, out double correctedValue)
        {
            return double.TryParse(value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out correctedValue);
        }

        public static string ExtractConditionParameterName(string fullName)
        {
            //извлечение имени условия/измеренного параметра из принятого bindingPath вида "RTSL©ITM3"

            //получаем имя с концевыми числами
            string result = null;

            if (fullName != null)
            {
                result = fullName.Substring(fullName.IndexOf(SCME.Common.Constants.FromXMLNameSeparator) + 1);

                //избавляемся от концевых чисел
                result = SCME.Common.Routines.RemoveEndingNumber(result);
            }

            return result;
        }

        public static string CorrectTestTypeName(string testTypeName)
        {
            return (testTypeName == "SL") ? "StaticLoses" : testTypeName;
        }

        public static TestParametersType? StrToTestParametersType(string testTypeName)
        {
            //преобразование строкового обозначения теста в тип TestParametersType?
            TestParametersType testType;

            if (Enum.TryParse(CorrectTestTypeName(testTypeName), true, out testType))
                return testType;
            else
                return null;
        }

        [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
        private enum EType
        {
            [EnumMember]
            Unknown = 0,

            [EnumMember]
            Diode = 1,

            [EnumMember]
            Thyristor = 2
        }

        private static EType ETypeByDeviceTypeRu(string deviceTypeRu)
        {
            //вычисляется только по обозначению типа написанному на русском языке - все его симолы русские
            EType result = EType.Unknown;

            //тиристорный тип: в deviceTypeRu первым должен встретится русский символ 'Т'
            if (deviceTypeRu.IndexOf("Т", StringComparison.InvariantCultureIgnoreCase) != -1)
                result = EType.Thyristor;

            //диодный тип: в deviceTypeRu первым должен встретится русский символ 'Д'
            if (deviceTypeRu.IndexOf("Д", StringComparison.InvariantCultureIgnoreCase) != -1)
                result = EType.Diode;

            return result;
        }

        public static List<string> ConditionNamesByDeviceTypeRu(TestParametersType testType, string deviceTypeRu, TemperatureCondition temperatureCondition)
        {
            //возвращает список условий, которые надо показывать пользователю
            List<string> result = null;

            if (!string.IsNullOrEmpty(deviceTypeRu))
            {
                switch (testType)
                {
                    case TestParametersType.StaticLoses:
                        result = new List<string>
                        {
                            "SL_ITM" //в БД это же условие используется как IFM
                        };
                        break;

                    case TestParametersType.Bvt:
                        //отображаются безусловно
                        result = new List<string>
                        {
                            "BVT_UdsmUrsm_VD",
                            "BVT_UdsmUrsm_VR"
                        };

                        //холодное измерение
                        if (temperatureCondition == TemperatureCondition.RT)
                        {
                            result.Add("BVT_I");

                            EType eType = ETypeByDeviceTypeRu(deviceTypeRu);

                            if ((eType == EType.Diode) || (eType == EType.Thyristor))
                                result.Add("BVT_VR");
                        }

                        //горячее измерение
                        if (temperatureCondition == TemperatureCondition.TM)
                        {
                            EType eType = ETypeByDeviceTypeRu(deviceTypeRu);

                            switch (eType)
                            {
                                //тиристорный тип
                                case EType.Thyristor:
                                    result.Add("BVT_VD");
                                    result.Add("BVT_VR");
                                    break;

                                case EType.Diode:
                                    result.Remove("BVT_UdsmUrsm_VD");
                                    result.Add("BVT_VR");
                                    break;
                            }
                        }
                        break;

                    case TestParametersType.Gate:
                    case TestParametersType.Commutation:
                        result = new List<string>();
                        break;

                    case TestParametersType.Clamping:
                        result = new List<string>();
                        //result.Add("CLAMP_Temperature");
                        break;

                    case TestParametersType.Dvdt:
                        result = new List<string>
                        {
                            "DVDT_VoltageRate"
                        };
                        break;

                    case TestParametersType.ATU:
                        result = new List<string>();
                        break;

                    case TestParametersType.RAC:
                    case TestParametersType.IH:
                    case TestParametersType.RCC:
                    case TestParametersType.Sctu:
                    case TestParametersType.QrrTq:
                        result = new List<string>();
                        break;
                }
            }

            return result;
        }

        public static List<string> MeasuredParametersByTestType(TestParametersType testType)
        {
            //возвращает список имён измеряемых параметров, которые надо показывать пользователю
            //если данная реализация возвращает null - значит никаких ограничений на имена измеряемых параметров для отображения пользователю нет - их надо показывать все
            List<string> result = null;

            switch (testType)
            {
                case TestParametersType.ATU:
                    result = new List<string>
                    {
                        "PRSM"
                    };
                    break;

                case TestParametersType.Commutation:
                case TestParametersType.Clamping:

                case TestParametersType.Sctu:
                    result = new List<string>();
                    break;

                case TestParametersType.QrrTq:
                    result = new List<string>
                    {
                        "IRR",
                        Common.Constants.Tq,
                        Common.Constants.Trr,
                        "IrM",
                        Common.Constants.Qrr
                    };

                    break;

                case TestParametersType.Gate:
                    result = new List<string>
                    {
                        "RG",
                        "IL",
                        "IGT",
                        "VGT",
                        "IH"
                    };
                    break;

                case TestParametersType.StaticLoses:
                    result = new List<string>
                    {
                        "VTM"
                    };
                    break;

                case TestParametersType.Bvt:
                    result = new List<string>
                    {
                        "IDRM",
                        "IRRM",
                        "VDRM",
                        "VRRM",
                        "IDSM",
                        "IRSM"
                    };
                    //result.Add("VDSM");
                    //result.Add("VRSM");
                    break;

                case TestParametersType.Dvdt:
                case TestParametersType.RAC:
                case TestParametersType.IH:
                case TestParametersType.RCC:
                    break;
            }

            return result;
        }

        private static bool BuildDeviceClassesListByTemperatureMode(string temperatureMode, List<string> temperatureModesList, List<string> deviceClassesList, List<int> resultList)
        {
            //заполняет принятый resultList значениями классов для принятого температурного режима temperatureMode
            if ((temperatureModesList == null) || (deviceClassesList == null) || (resultList == null))
                return false;

            //количество элементов в обоих списках должно совпадать, если это не так - будем возвращать false
            if (temperatureModesList.Count != deviceClassesList.Count)
                return false;

            resultList.Clear();

            for (int i = 0; i < temperatureModesList.Count; i++)
            {
                if (temperatureModesList[i] == temperatureMode)
                {
                    //если строковое значение класса не преобразуется в тип int - возвращаем false
                    if (int.TryParse(deviceClassesList[i], out int deviceClass))
                    {
                        resultList.Add(deviceClass);
                    }
                    else
                        return false;
                }
            }

            //если мы сформировали хотя-бы одно значение в resultList - возвращаем true, иначе возвращаем false
            return (resultList.Count > 0) ? true : false;
        }

        public static object CalcDeviceStatus(byte statusColdSum, byte statusColdCount, byte statusHotSum, byte statusHotCount)
        {
            //если есть хотя-бы один статус не OK - результат Fault;
            //чтобы вычислить результат в OK необходимо проверить, что эти статусы получены при температурных режимах и Cold и Hot и все статусы есть 1 (OK);
            //если нет хотя-бы одного из температурных режимов Cold или Hot - возвращаем не определённый статус - NULL
            if ((statusColdSum == statusColdCount) && (statusHotSum == statusHotCount) && (statusColdCount != 0) && (statusHotCount != 0))
            {
                return Constants.GoodSatatus;  //"OK";
            }
            else
            {
                if ((statusColdSum == statusColdCount) && (statusHotSum == statusHotCount))
                {
                    //все статусы в состоянии 1 (OK) - значит нет измерения либо Cold либо Hot
                    return DBNull.Value;
                }
                else
                {
                    //есть статусы в состоянии 0 (не OK)
                    return Constants.FaultSatatus; //"Fault";
                }
            }
        }

        public static string CalcDeviceStatusHelp(byte statusColdSum, byte statusColdCount, byte statusHotSum, byte statusHotCount)
        {
            //возвращает историю формирования статуса сформированной группы изделий - как был получен игоговый статус группы изделий
            string result = string.Empty;

            //формируем историю вычисления холодных статусов
            for (int i = 0; i < statusColdSum; i++)
            {
                if (result != string.Empty)
                    result = string.Concat(result, ", ");

                result = string.Concat(result, "RT:OK");
            }

            int statusColdFaultCount = statusColdCount - statusColdSum;
            for (int i = 0; i < statusColdFaultCount; i++)
            {
                if (result != string.Empty)
                    result = string.Concat(result, ", ");

                result = string.Concat(result, "RT:Fault");
            }

            //формируем историю вычисления горячих статусов
            for (int i = 0; i < statusHotSum; i++)
            {
                if (result != string.Empty)
                    result = string.Concat(result, ", ");

                result = string.Concat(result, "TM:OK");
            }

            int statusHotFaultCount = statusHotCount - statusHotSum;
            for (int i = 0; i < statusHotFaultCount; i++)
            {
                if (result != string.Empty)
                    result = string.Concat(result, ", ");

                result = string.Concat(result, "TM:Fault");
            }

            return result;
        }

        public static object CalcDeviceClass(object objDeviceClassColdMax, object objDeviceClassHotMax)
        {
            switch (objDeviceClassColdMax == DBNull.Value)
            {
                //холодного класса нет - возвращаем значение горячего класса
                case true:
                    return objDeviceClassHotMax;

                default:
                    //вычисляем минимум из значений холодного и горячего классов если они оба определены
                    return (objDeviceClassHotMax == DBNull.Value) ? objDeviceClassColdMax : Math.Min(Convert.ToInt16(objDeviceClassColdMax), Convert.ToInt16(objDeviceClassHotMax));
            }
        }

        public static string CalcDeviceClassHelp(object deviceClassColdMax, object deviceClassHotMax)
        {
            string result = (deviceClassColdMax == DBNull.Value) ? string.Empty : string.Format("max(RT)={0}", deviceClassColdMax);
            string deviceClassHotHelp = (deviceClassHotMax == DBNull.Value) ? string.Empty : string.Format("max(TM)={0}", deviceClassHotMax);

            if (result != string.Empty)
                result = string.Concat(result, ", ");

            result = string.Concat(result, deviceClassHotHelp);

            if (result != string.Empty)
                result = string.Concat(result, Constants.StringDelimeter);

            result = string.Concat(result, "Class=min(max(RT), max(TM))");

            return result;
        }

        public static string ParseColumnName(string columnName, string separator, out string temperatureCondition)
        {
            //первые два символа принятого columnName всегда указывают на температурный режим
            //если принятый columnName не содержит описания температурного режима - данная реализация возвращает null
            const int cTemperatureConditionStart = 0;
            const int cTemperatureConditionCount = 2;

            temperatureCondition = columnName.Substring(cTemperatureConditionStart, cTemperatureConditionCount).ToUpper();

            //проверяем что мы считали значение температурного режима
            if (Enum.TryParse(temperatureCondition, true, out TemperatureCondition tc)) //&& (Enum.IsDefined(typeof(TemperatureCondition), temperatureCondition)))
            {
                //мы считали корректное описание температурного режима
                //имя условия/параметра стоит за разделителем Constants.cNameSeparator
                int startNameIndex = columnName.IndexOf(separator);

                return (startNameIndex == -1) ? null : columnName.Substring(startNameIndex + separator.Length).ToUpper();
            }
            else
            {
                //мы не смогли считать описание температурного режима
                temperatureCondition = null;

                return null;
            }
        }

        public static string ParseColumnName(string columnName, out string temperatureCondition)
        {
            string result = ParseColumnName(columnName, SCME.Common.Constants.FromXMLNameSeparator, out temperatureCondition);

            return result;
        }

        public static string ParseColumnName(string columnName, out string test, out string temperatureCondition)
        {
            //используем реализацию, которая делает то что нам нужно, но не умеет извлекать имя теста
            string result = ParseColumnName(columnName, out temperatureCondition);

            //извлекаем имя теста
            //вырезаем описание температурного режима
            test = columnName.Remove(0, 2);

            //вырезаем описание предмета хранения
            int fromXMLNameSeparatorIndex = test.IndexOf(SCME.Common.Constants.FromXMLNameSeparator);

            if (fromXMLNameSeparatorIndex != -1)
                test = test.Remove(fromXMLNameSeparatorIndex);

            List<string> subjects = Enum.GetNames(typeof(Common.Routines.XMLValues)).ToList();

            foreach (string s in subjects)
            {
                int subjectStartIndex = test.IndexOf(s.ToLower());

                if (subjectStartIndex != -1)
                    test = test.Remove(subjectStartIndex);
            }

            test = test.ToUpper();

            return result;
        }

        public static string NameOfHiddenColumn(string columnName)
        {
            //возвращает имя скрытого столбца, предназначенного для хранения дополнительного значения для столбца с именем columnName
            return string.Concat(columnName, Constants.HiddenMarker);
        }

        public static string NameOfUnitMeasure(string columnName)
        {
            //возвращает имя скрытого столбца, предназначенного для хранения единицы измерения для столбца с именем columnName
            return string.Concat(columnName, "UnitMeasure", Constants.HiddenMarker);
        }

        public static string NameOfNrmMinParametersColumn(string columnName)
        {
            //возвращает имя скрытого столбца, предназначенного для хранения нормы Min для столбца с именем columnName
            return string.Concat(columnName, "NrmMinParameters", Constants.HiddenMarker);
        }

        public static string NameOfNrmMaxParametersColumn(string columnName)
        {
            //возвращает имя скрытого столбца, предназначенного для хранения нормы Max для столбца с именем columnName
            return string.Concat(columnName, "NrmMaxParameters", Constants.HiddenMarker);
        }

        public static string NameOfIsPairCreatedColumn()
        {
            //возвращает имя скрытого столбца, предназначенного для хранения флага образования температурной пары
            return string.Concat(Constants.IsPairCreated, Constants.HiddenMarker);
        }

        public static string NameOfRecordIsStorageColumn()
        {
            //возвращает имя скрытого столбца, предназначенного для хранения флага об использовании записи для хранения данных от других записей
            return string.Concat(Constants.RecordIsStorage, Constants.HiddenMarker);
        }

        public static bool IsColumnHidden(string columnName)
        {
            //отвечает на вопрос является ли столбец с принятм именем скрытым столбцом
            return columnName.Contains(Constants.HiddenMarker);
        }

        public static string TrimEndNumbers(string value)
        {
            //вырезает цифры начиная с конца принятого value и возвращает value без цифр
            //пример: 123abc456xyz789 => 123abc456xyz
            char[] trimChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            return value.TrimEnd(trimChars);
        }

        public static string TestNameInDataGridColumn(string test)
        {
            return (test == "StaticLoses") ? "SL" : test;
        }

        public static double TemperatureValueFromXML(XmlDocument xmlConditionDoc)
        {
            //извлечение значения условия с именем 'CLAMP_Temperature' теста 'Clamping' из XML описания условий xmlConditionDoc
            //возвращает:
            //            -1 - значение температуры не удалость прочитать из профиля;
            //            значение температуры
            double temperatureValue = -1;

            XmlNode node = xmlConditionDoc.SelectSingleNode("//T[@Test='Clamping' and @Name='CLAMP_Temperature']");

            if (node != null)
            {
                if (double.TryParse(node.Attributes["Value"].Value, out temperatureValue))
                    return temperatureValue;
            }

            return temperatureValue;
        }

        private static string CommentRequisitesFromXML(XmlAttributeCollection commentAttributes)
        {
            //строит описание реквизитов одного комментария по принятому commentAttributes
            string result = string.Empty;

            if (commentAttributes["RECORDDATE"] != null)
                result = Convert.ToDateTime(commentAttributes["RECORDDATE"].Value).ToString("dd.MM.yyyy");

            if (commentAttributes["USERID"] != null)
            {
                //считываем по идентификатору пользователя его полное имя
                string fullUserName = string.Empty;

                if (DbRoutines.FullUserNameByUserID(Convert.ToInt64(commentAttributes["USERID"].Value), out fullUserName))
                {
                    if ((fullUserName != null) && (fullUserName != string.Empty))
                    {
                        if (result != string.Empty)
                            result += " ";

                        result += fullUserName;
                    }
                }
            }

            return result;
        }

        private static string UnicComments(List<string> commentsList, out string lastDateAndCreator)
        {
            //извлекает из принятого списка комментариев commentsList текст не повторяющихся комментариев и возвращает в качестве результата
            //в out lastDateAndCreator возвращает дату и автора самого последнего комментария
            if (commentsList != null)
            {
                DateTime? maxRecordDate = null;
                List<XmlElement> listOfXmlElements = new List<XmlElement>();

                foreach (string commentXML in commentsList)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(commentXML);
                    XmlElement documentElement = xmlDoc.DocumentElement;

                    listOfXmlElements.Add(documentElement);

                    //при формировании списка listOfXmlElements вычисляем максимальную дату в данном списке
                    DateTime currentRecordDate = DateTime.Parse(documentElement.FirstChild.Attributes["RECORDDATE"].Value);

                    if (maxRecordDate == null)
                    {
                        maxRecordDate = currentRecordDate;
                    }
                    else
                    {
                        if (currentRecordDate > (DateTime)maxRecordDate)
                            maxRecordDate = currentRecordDate;
                    }
                }

                XmlAttributeCollection commentAttributes = listOfXmlElements.Where(x => DateTime.Parse(x.FirstChild.Attributes["RECORDDATE"].Value) == maxRecordDate).Select(x => x.FirstChild.Attributes).FirstOrDefault();
                lastDateAndCreator = CommentRequisitesFromXML(commentAttributes);

                IEnumerable<string> unicComments = listOfXmlElements.OrderBy(x => DateTime.Parse(x.FirstChild.Attributes["RECORDDATE"].Value)).Select(x => x.FirstChild.Attributes["COMMENTS"].Value).Distinct();

                return string.Join(Constants.StringDelimeter, unicComments);
            }

            //комментарии отсутствуют
            lastDateAndCreator = null;
            return null;
        }

        /*
        public static string ProcessingXmlDeviceComment(XmlElement documentElement)
        {
            if (documentElement != null)
            {
                XmlNode child = documentElement.ChildNodes[0];
                XmlAttributeCollection attributes = child.Attributes;

                if (attributes != null)
                {
                    string comment = CommentFromXML(attributes);

                    return comment;
                }
            }

            return null;
        }
        */

        private static void CalcNameStatistics(Dictionary<string, int> namesDict, string name)
        {
            //одно и то же имя condition/parameter может встретиться при анализе принятого documentElement несколько раз
            //для возможности сохранения значений таких condition/parameter будем дописывать к имени condition/parameter его номер встречи, который будем считать начиная с единицы
            //для удобства пользователя будем дописывать к имени condition/parameter его номер встречи только в случае оно больше единицы
            switch (namesDict.TryGetValue(name, out int refNumber))
            {
                case true:
                    refNumber++;
                    namesDict[name] = refNumber;
                    break;

                default:
                    namesDict.Add(name, 1);
                    break;
            }
        }

        public static void SaveValue(bool storageExists, DynamicObj item, string name, object value)
        {
            switch (storageExists)
            {
                case true:
                    item.SetMember(name, value);
                    break;

                default:
                    item.TrySetMember(new SetPropertyBinder(name), value);
                    break;
            }
        }

        public delegate void BuildColumnInDataGrid(string header, string bindPath);
        public static void ValuesToRow(System.Data.SqlClient.SqlDataReader reader, DynamicObj item, bool isReload, BuildColumnInDataGrid buildColumnInDataGridHandler)
        {
            if ((reader != null) && (item != null))
            {
                //переписываем постоянные данные, которые есть всегда
                //уникальность перечисленных значений DEV_ID обеспечена на уровне хранимой процедуры dbViewerPrepareSortedGroups
                string name = Common.Constants.AssemblyProtocolID;
                int index = reader.GetOrdinal(name);
                object value = reader[index];
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.AssemblyProtocolDescr;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                /*
                name = "SORTINGVALUE";
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));
                */

                name = Common.Constants.DevID;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.TDevID;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.GroupName;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.GroupID;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Code;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.MmeCode;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index])); //RemoveDuplicates()
                index = reader.GetOrdinal(Common.Constants.ProfileName);
                name = NameOfHiddenColumn(name);
                SaveValue(isReload, item, name, ChangeString_AggDelimeterToDelimeter(Convert.ToString(reader[index]), Constants.StringDelimeter));

                //дата возвращается всегда одно значение - max значение из списка дат изделий входящих в группу
                name = Common.Constants.Ts;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Usr;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index])); //RemoveDuplicates()

                name = "DEVICETYPEID";
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.DeviceTypeRU;
                index = reader.GetOrdinal(name);
                string deviceTypeRu = Convert.ToString(reader[index]);
                SaveValue(isReload, item, name, deviceTypeRu);

                name = Common.Constants.AverageCurrent;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Constructive;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Item;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));
                /*
                index = reader.GetOrdinal("TEMPERATUREMODE");
                string temperatureModes = Convert.ToString(reader[index]);
                */

                name = "SIOMNITY";
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                //от хранимой процедуры можем получить только 0 или 1
                name = Common.Constants.Choice;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToBoolean(reader[index]));

                /*
                name = "SAPID";
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index])); //RemoveDuplicates(Convert.ToString(reader[index]))

                name = Common.Constants.SapDescr;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));
                */

                name = Common.Constants.AssemblyStatusID;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.AssemblyStatusDescr;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));

                //вычисляем статус изделия
                byte statusColdSum = Convert.ToByte(reader[reader.GetOrdinal("STATUSCOLDSUM")]);
                byte statusColdCount = Convert.ToByte(reader[reader.GetOrdinal("STATUSCOLDCOUNT")]);
                byte statusHotSum = Convert.ToByte(reader[reader.GetOrdinal("STATUSHOTSUM")]);
                byte statusHotCount = Convert.ToByte(reader[reader.GetOrdinal("STATUSHOTCOUNT")]);
                name = Common.Constants.Status;
                SaveValue(isReload, item, name, CalcDeviceStatus(statusColdSum, statusColdCount, statusHotSum, statusHotCount));
                name = NameOfHiddenColumn(name);
                //запоминаем как был вычислен итоговый статус группы
                SaveValue(isReload, item, name, CalcDeviceStatusHelp(statusColdSum, statusColdCount, statusHotSum, statusHotCount));

                //вычисляем значение класса группы
                //итоговое значение класса сформированной группы есть минимум из максимумов по холодному и горячему классам
                object deviceClassColdMax = reader[reader.GetOrdinal("DEVICECLASSCOLDMAX")];
                object deviceClassHotMax = reader[reader.GetOrdinal("DEVICECLASSHOTMAX")];
                name = Common.Constants.DeviceClass;
                SaveValue(isReload, item, name, CalcDeviceClass(deviceClassColdMax, deviceClassHotMax));
                //запоминаем как был вычислен итоговый класс группы
                name = NameOfHiddenColumn(name);
                SaveValue(isReload, item, name, CalcDeviceClassHelp(deviceClassColdMax, deviceClassHotMax));

                name = Common.Constants.Reason;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, RemoveDuplicates(Convert.ToString(reader[index])));

                name = Common.Constants.CodeOfNonMatch;
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, RemoveDuplicates(Convert.ToString(reader[index])));

                name = "PROF_ID";
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, ChangeString_AggDelimeterToDelimeter(Convert.ToString(reader[index]), Constants.DelimeterForStringConcatenate));

                name = "PROFILEBODY";
                index = reader.GetOrdinal(name);
                SaveValue(isReload, item, name, Convert.ToString(reader[index]));
                /*
                //в интерфейсе показываем пользователю тело профиля, в hint к этому полю показываем сгруппированное обозначение профилей                
                index = reader.GetOrdinal(Common.Constants.ProfileName);
                name = NameOfHiddenColumn(name);
                SaveValue(isReload, item, name, ChangeString_AggDelimeterToDelimeter(Convert.ToString(reader[index]), Constants.StringDelimeter));
                */

                //переписываем переменные данные - комментарии к изделию, описание условий испытаний и измеряемых параметров
                name = Common.Constants.DeviceComments;
                index = reader.GetOrdinal(name);
                string deviceComments = Convert.ToString(reader[index]);

                if ((deviceComments != null) && (deviceComments.Trim() != string.Empty))
                {
                    //обрабатываем результат группировки - строки комментариев к группе измерений перечислены через разделитель - результат функции STRING_AGG
                    List<string> commentsList = deviceComments.Split(SCME.Common.Constants.cString_AggDelimeter).ToList();

                    //получаем не повторяющиеся комментарии (список в строку, каждый уникальный комментарий с новой строки) к группе измерений и имя автора с последней (максимальной) датой комментария
                    string unicComments = UnicComments(commentsList, out string lastDateAndCreator);
                    SaveValue(isReload, item, name, unicComments);

                    //имя комментатора и дату пишем в hint
                    name = NameOfHiddenColumn(name);
                    SaveValue(isReload, item, name, lastDateAndCreator);
                }

                //запоминаем начальный индекс условий изделия в item, чтобы при отрисовке их значений цветом не просматривать лишние столбцы
                int conditionsInDataSourceFirstIndex = item.GetDynamicMemberNames().ToList().Count() + 1;
                SaveValue(isReload, item, Constants.ConditionsInDataSourceFirstIndex, conditionsInDataSourceFirstIndex);

                index = reader.GetOrdinal("PROFCONDITIONS");
                string profConditionsXML = Convert.ToString(reader[index]);

                //создаём словарь имён для вычисления добавочного числа к имени conditions/parameters
                Dictionary<string, int> namesDict = new Dictionary<string, int>();

                //будем здесь формировать через разделитель значения температур при которых выполняются тесты, описания которых указаны в группированном значении PROFCONDITIONS
                //эти температуры понадобятся при обработке значений DEVICEPARAMETERS
                List<double> temperatureValuesList = new List<double>();

                if ((profConditionsXML != null) && (profConditionsXML.Trim() != string.Empty))
                {
                    //обрабатываем результат группировки - описания условий тестов перечислены через разделитель (получено как результат функции STRING_AGG)
                    XmlDocument xmlDoc = null;

                    List<string> profConditionsXMLList = profConditionsXML.Split(SCME.Common.Constants.cString_AggDelimeter).ToList<string>();
                    foreach (string xmlProfConditions in profConditionsXMLList)
                    {
                        xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xmlProfConditions);

                        //считываем значение температуры из текущего описания условий теста
                        double temperatureValue = TemperatureValueFromXML(xmlDoc);

                        temperatureValuesList.Add(temperatureValue);

                        ProcessingXmlData(namesDict, buildColumnInDataGridHandler, xmlDoc.DocumentElement, Common.Routines.XMLValues.Conditions, temperatureValue, deviceTypeRu, item);
                    }
                }

                //запоминаем начальный индекс параметров изделия в item, чтобы при отрисовке цветом попадания их значений в нормы не просматривать лишние столбцы
                int parametersInDataSourceFirstIndex = item.GetDynamicMemberNames().ToList().Count() + 1;
                SaveValue(isReload, item, Constants.ParametersInDataSourceFirstIndex, parametersInDataSourceFirstIndex);

                index = reader.GetOrdinal("DEVICEPARAMETERS");
                string deviceParametersXML = Convert.ToString(reader[index]);

                if ((deviceParametersXML != null) && (deviceParametersXML.Trim() != string.Empty))
                {
                    //обрабатываем результат группировки - описания параметров перечислены через разделитель (получено как результат функции STRING_AGG)
                    XmlDocument xmlDoc = null;

                    //очищаем содержимое namesDict от хранящихся в нем conditions
                    namesDict.Clear();

                    index = 0;
                    List<string> deviceParametersXMLList = deviceParametersXML.Split(SCME.Common.Constants.cString_AggDelimeter).ToList<string>();
                    foreach (string xmlDeviceParameters in deviceParametersXMLList)
                    {
                        //если при чтении условий температура не была считана (нет описания условий) - считаем, что параметры получены при комнатной темепературе
                        double temperature = (temperatureValuesList.Count - 1 >= index) ? temperatureValuesList[index] : 25;

                        //хранимая процедура чтения порции данных может прочитать из базы данных пустое множество параметров изделия - в этом случае xmlDeviceParameters будет иметь значение "EMPTY"
                        //так сделано чтобы не потерять соответствие между температурными режимами и считанными наборами параметров
                        if (xmlDeviceParameters != "EMPTY")
                        {
                            xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(xmlDeviceParameters);

                            ProcessingXmlData(namesDict, buildColumnInDataGridHandler, xmlDoc.DocumentElement, Common.Routines.XMLValues.Parameters, temperature, deviceTypeRu, item);
                        }

                        index++;
                    }
                }
            }
        }

        public static void ProcessingXmlData(Dictionary<string, int> namesDict, BuildColumnInDataGrid buildColumnInDataGridHandler, XmlElement documentElement, Common.Routines.XMLValues subject, double temperatureValue, string deviceTypeRu, DynamicObj item)
        {
            //builderColumnInDataGrid - механизм обратного вызова построения столбца в DataGrid

            TemperatureCondition tc = TemperatureConditionByTemperature(temperatureValue);

            foreach (XmlNode child in documentElement.ChildNodes)
            {
                XmlAttributeCollection attributes = child.Attributes;

                if (attributes != null)
                {
                    if (attributes["Test"] != null)
                    {
                        //атрибут "Test" точно существует, имеем дело с conditions/parameters
                        string test = attributes["Test"].Value;

                        //поправляем значение test из-за не соответствия в базе данных и в коде
                        test = CorrectTestTypeName(test);

                        //перечисление TestParametersType никогда ни при каких условиях не может содержать тип теста Manually (вручную созданные параметры)
                        if (
                            Enum.TryParse(test, true, out TestParametersType testType) ||
                            ((test == DbRoutines.cManually) && (subject == Common.Routines.XMLValues.Parameters))
                           )
                        {
                            bool need = false;

                            string name = null;
                            string value = null;
                            string unitMeasure = null;

                            string columnNameInDataGrid = string.Empty;
                            string columnNameInDataSource = string.Empty;

                            /*
                            Type factColumnDataType = null;
                            Type newColumnDataType = null;
                            */

                            double? dNrmMin = null;
                            double? dNrmMax = null;

                            if (attributes["Name"] != null)
                            {
                                name = attributes["Name"].Value;

                                if (name != string.Empty)
                                {
                                    switch (subject)
                                    {
                                        case Common.Routines.XMLValues.Conditions:
                                            //читаем список условий, которые надо показать
                                            List<string> conditions = ConditionNamesByDeviceTypeRu(testType, deviceTypeRu, tc);

                                            //если conditions=null - показываем любые условия - т.е. возможно не известен тип изделия
                                            //если же conditions не null, то в нём должно присутствовать условие с именем name
                                            need = (conditions == null) || ((conditions != null) && (conditions.IndexOf(name) != -1) && (attributes["Value"] != null));

                                            if (need)
                                            {
                                                //описания условий хранятся как строки, в них может быть всё что угодно                                    
                                                value = attributes["Value"].Value;

                                                //newColumnDataType = typeof(string);
                                                TypeOfValue(value, out value); //factColumnDataType = 

                                                //строим имя условия для отображения в DataGrid
                                                test = TestNameInDataGridColumn(test);
                                                string conditionName = Dictionaries.ConditionName(tc, name);

                                                //строим имя условия для отображения в DataGrid
                                                columnNameInDataGrid = ColumnNameInDataGrid(tc.ToString(), test, conditionName);

                                                //строим имя условия для использования в DataSource
                                                columnNameInDataSource = ColumnNameInDataSource(tc.ToString(), test, subject, name);
                                            }

                                            break;

                                        case Common.Routines.XMLValues.Parameters:
                                            //строим список измеренных параметров, которые надо показать
                                            List<string> measuredParameters = MeasuredParametersByTestType(testType);

                                            //проверяем вхождение имени текущего измеренного параметра в список измеренных параметров, которые требуется показать для текущего типа теста. если measuredParameters=null - значит надо показать их все
                                            need = ((measuredParameters == null) || (measuredParameters.IndexOf(name) != -1) && (attributes["Value"] != null));

                                            if (need)
                                            {
                                                XmlAttribute attributeValue = attributes["Value"];
                                                if (attributeValue != null)
                                                {
                                                    value = attributeValue.Value;

                                                    //значения измеренных параметров - всегда должны преобразовываться к числу с плавающей запятой. если это не так - ругаемся
                                                    if (!ValueAsDouble(value, out double dValue))
                                                        throw new Exception(string.Format("При чтении значения измеренного параметра '{0}' из XML описания оказалось, что его значение '{1}' не преобразуется к типу Double.", name, value));

                                                    //округляем до второго знака значения параметров (в том числе и созданных вручную)
                                                    //при этом если значение параметра записано целым числом - оно таковым и останется
                                                    value = Math.Round(dValue, 2).ToString();
                                                }

                                                /*
                                                newColumnDataType = typeof(double);
                                                factColumnDataType = newColumnDataType;
                                                */

                                                //атрибут "TemperatureCondition" может быть только у параметров, которые созданы пользователем
                                                string tC = (test == DbRoutines.cManually) ? attributes["TemperatureCondition"].Value : tc.ToString();

                                                //строим имя параметра для отображения в DataGrid
                                                test = TestNameInDataGridColumn(test);
                                                string paramName = (test == DbRoutines.cManually) ? name : Dictionaries.ParameterName(name);
                                                columnNameInDataGrid = ColumnNameInDataGrid(tC, test, paramName);

                                                //строим имя параметра для использования в DataSource
                                                columnNameInDataSource = ColumnNameInDataSource(tC, test, subject, name);

                                                //считываем единицу измерения
                                                unitMeasure = (attributes["Um"] == null) ? string.Empty : attributes["Um"].Value;

                                                //считываем значения норм
                                                XmlAttribute attributeNrm = attributes["NrmMin"];
                                                if (attributeNrm != null)
                                                {
                                                    string nrmMin = attributeNrm.Value;
                                                    //значения норм - всегда числа с плавающей запятой. если это не так - ругаемся
                                                    if (!ValueAsDouble(nrmMin, out double dValue))
                                                        throw new Exception(string.Format("При чтении значения нормы (min) измеренного параметра '{0}' из XML описания оказалось, что оно '{1}' не преобразуется к типу Double.", name, nrmMin));

                                                    dNrmMin = Math.Round(dValue, 2);
                                                }

                                                attributeNrm = attributes["NrmMax"];
                                                if (attributeNrm != null)
                                                {
                                                    string nrmMax = attributeNrm.Value;
                                                    //значения норм - всегда числа с плавающей запятой. если это не так - ругаемся
                                                    if (!ValueAsDouble(nrmMax, out double dValue))
                                                        throw new Exception(string.Format("При чтении значения нормы (max) измеренного параметра '{0}' из XML описания оказалось, что оно '{1}' не преобразуется к типу Double.", name, nrmMax));

                                                    dNrmMax = Math.Round(dValue, 2);
                                                }
                                            }

                                            break;

                                        default:
                                            throw new Exception(string.Format("Для принятого значения subject={0} обработка не предусмотрена.", subject.ToString()));
                                    }
                                }
                            }

                            if (need)
                            {
                                //актуализируем статистику по использованию имён conditions/parameters
                                CalcNameStatistics(namesDict, columnNameInDataSource);

                                //запоминаем значение condition/parameter
                                int addToName = namesDict[columnNameInDataSource];
                                string sAddToName = (addToName == 1) ? string.Empty : addToName.ToString();

                                //меняем имя столбца в item и в dataGrid дописывая в конце его имени sAddToName
                                columnNameInDataSource = string.Concat(columnNameInDataSource, sAddToName);
                                columnNameInDataGrid = string.Concat(columnNameInDataGrid, sAddToName);

                                //различаем ситуации когда выполняется создание нового места хранения и когда запись выполняется в уже существующее место хранения
                                bool storageExists = item.GetMember(columnNameInDataSource, out object storedValue);
                                SaveValue(storageExists, item, columnNameInDataSource, value);

                                //если столбца с таким Header не существует - создаём его в dataGrid 
                                buildColumnInDataGridHandler?.Invoke(columnNameInDataGrid, columnNameInDataSource);

                                //значение condition/parameter сохранено, запоминаем значение температуры и описание норм
                                string hintString = string.Format("{0}{1}", temperatureValue, Constants.Celsius);
                                string nrmDescr = ((dNrmMin == null) && (dNrmMax == null)) ? null : NrmDescr(dNrmMin, dNrmMax);
                                if (nrmDescr != null)
                                    hintString = string.Concat(hintString, Constants.StringDelimeter, nrmDescr);
                                name = NameOfHiddenColumn(columnNameInDataSource);
                                SaveValue(storageExists, item, name, hintString);

                                //запоминаем значение единицы измерения
                                name = NameOfUnitMeasure(columnNameInDataSource);
                                SaveValue(storageExists, item, name, unitMeasure ?? string.Empty);

                                //запоминаем значения норм min и max
                                if (dNrmMin != null)
                                {
                                    name = NameOfNrmMinParametersColumn(columnNameInDataSource);
                                    SaveValue(storageExists, item, name, (double)dNrmMin);
                                }

                                if (dNrmMax != null)
                                {
                                    name = NameOfNrmMaxParametersColumn(columnNameInDataSource);
                                    SaveValue(storageExists, item, name, (double)dNrmMax);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static DataGridBoundColumn CreateColumnInDataGrid(DataGridSqlResultBigData dataGrid, int index, string header, string bindPath)
        {
            DataGridTextColumn column = null;

            FrameworkElementFactory Factory = new FrameworkElementFactory(typeof(TextBlock));
            Factory.SetValue(TextBlock.TextProperty, header.Split('\n')[1]);

            if ((dataGrid != null) && (index != -1))
            {
                column = new DataGridTextColumn()
                {
                    Header = header,

                    HeaderTemplate = new DataTemplate
                    {
                        VisualTree = Factory
                    },

                    IsReadOnly = true,
                    Binding = new Binding(bindPath),

                    //данное поле пришлось применить взамен поля Binding из-за столбца комментариев класса DataGridTemplateColumn, у которого поле Binding отсутствует
                    //это поле (SortMemberPath) начал использовать только для единообразия чтения имени поля базы данных связанного с создаваемым столбцом dataGrid
                    SortMemberPath = bindPath
                };

                dataGrid.Columns.Insert(index, column);
            }

            return column;
        }

        private static void FillData(System.Collections.IList listOfItems, System.Data.SqlClient.SqlDataReader reader)
        {
            //формируем набор данных без построения столбцов в DataGrid
            DynamicObj item = new DynamicObj();
            ValuesToRow(reader, item, false, null);
            listOfItems.Add(item);
        }

        public static void GetCacheData(List<DynamicObj> data, int cacheSize)
        {
            //получение содержимого кеша
            if (data != null)
                SCME.Types.DbRoutines.CacheReadData(FillData, null, data, 0, cacheSize, Common.Constants.cString_AggDelimeter);
        }

        public static void GetAssemblyProtocolData(int assemblyProtocolID, List<DynamicObj> data)
        {
            if (data != null)
                SCME.Types.DbRoutines.ReadDataByAssemblyProtocolID(assemblyProtocolID, Common.Constants.cString_AggDelimeter, FillData, data);
        }

        public static void SetToQueueCreateColumnInDataGrid(DataGridSqlResultBigData dataGrid, ComboBox cmbDeviceType, ConcurrentQueue<Action> queueManager, object columnsLocker, bool assemblyProtocolMode, string header, string bindPath)
        {
            queueManager.Enqueue(
                                  delegate
                                  {
                                      lock (columnsLocker)
                                      {
                                          if ((!string.IsNullOrEmpty(header)) && (!string.IsNullOrEmpty(bindPath)))
                                          {
                                              //убеждаемся, что столбца c Header, равным принятому header в dataGrid не существует
                                              if (dataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == header) == null)
                                              {
                                                  string[] template = null;

                                                  switch (assemblyProtocolMode)
                                                  {
                                                      case true:
                                                          //если имеем дело с режимом просмотра протокола сборки, то список отображаемых столбцов зависит от выбраного пользователем типа изделия
                                                          object selectedItem = cmbDeviceType.SelectedItem;
                                                          string deviceTypeRu = (selectedItem == null) ? null : ((string[])selectedItem)[1];
                                                          template = AssemblyProtocolParametersByDeviceTypeRu(deviceTypeRu);

                                                          break;

                                                      default:
                                                          //просмотр исходных данных групп
                                                          template = Constants.OrderedColumnNames;

                                                          break;
                                                  }

                                                  //требуется создание столбца                                                  
                                                  int index = -1;

                                                  switch (header.Contains(DbRoutines.cManually))
                                                  {
                                                      //это вручную созданный параметр - всегда будем создавать его в самом конце списка столбцов - после условий и параметров померенных КИПП-ом
                                                      case true:
                                                          //DataGridColumn lastXMLColumn = dataGrid.Columns.LastOrDefault(c => ((System.Windows.Data.Binding)((DataGridBoundColumn)c).Binding).Path.Path.Contains(SCME.Common.Constants.FromXMLNameSeparator));

                                                          DataGridColumn lastXMLColumn = dataGrid.Columns.LastOrDefault(c => c.SortMemberPath.Contains(SCME.Common.Constants.FromXMLNameSeparator));
                                                          index = (lastXMLColumn == null) ? Constants.StartConditionsParamersInDataGridIndex : dataGrid.Columns.IndexOf(lastXMLColumn) + 1;

                                                          break;

                                                      default:
                                                          //имеем дело с condition/parameter - вычисляем индекс столбца который будет создан в списке уже имеющихся столбцов this.dgDevices.Columns
                                                          //сразу ставим столбец в соответствии с расположением имён в вычисленном template
                                                          //извлекаем имя condition/parameter, оно будет в верхнем регистре
                                                          string columnName = ParseColumnName(header, Constants.StringDelimeter, out string temperatureCondition);
                                                          if (!string.IsNullOrEmpty(columnName))
                                                          {
                                                              columnName = SCME.Common.Routines.RemoveEndingNumber(columnName);

                                                              if (!string.IsNullOrEmpty(columnName))
                                                              {
                                                                  string foundedName = template.FirstOrDefault(p => p == columnName);
                                                                  int indexInTemplate = string.IsNullOrEmpty(foundedName) ? -1 : Array.IndexOf(template, foundedName);

                                                                  if (indexInTemplate != -1)
                                                                  {
                                                                      int count = 0;

                                                                      //вычисляем сколько столбцов в dataGrid уже создано по шаблону template
                                                                      for (int i = 0; i <= indexInTemplate; i++)
                                                                      {
                                                                          count += (from column in dataGrid.Columns
                                                                                    where !SCME.Common.Routines.RemoveEndingNumber(column.Header.ToString()).Contains(DbRoutines.cManually) &&
                                                                                          SCME.Common.Routines.RemoveEndingNumber(column.Header.ToString()).ToUpper().EndsWith(template[i])
                                                                                    select column
                                                                                   ).Count();
                                                                      }

                                                                      index = Constants.StartConditionsParamersInDataGridIndex + count;
                                                                  }
                                                              }
                                                          }

                                                          break;
                                                  }

                                                  if (index != -1)
                                                      CreateColumnInDataGrid(dataGrid, index, header, bindPath);
                                              }
                                          }
                                      }
                                  }
                                );
        }

        public static bool IsCPColumn(DataGridColumn column)
        {
            //возвращает:
            // true - принятый column отображает данные conditions/parameters;
            // false - принятый column отображает данные не conditions/parameters
            if (column != null)
            {
                string sourceFieldName = Common.Routines.SourceFieldNameByColumn(column);

                return (!string.IsNullOrEmpty(sourceFieldName)) && sourceFieldName.Contains(SCME.Common.Constants.FromXMLNameSeparator);
            }

            return false;
        }

        public delegate void RemoveXMLDataGridColumns();
        public static void DeleteAllColumnsFromXML(DataGridSqlResultBigData dataGrid, ConcurrentQueue<Action> queueManager, object columnsLocker)
        {
            //удаляет из dataGrid все столбцы которые отображают значения conditions/parameters
            //эти столбцы отличаются от всех остальных столбцов наличием разделителя Constants.FromXMLNameSeparator в bind.Path.Path

            queueManager.Enqueue(
                                  delegate
                                  {
                                      lock (columnsLocker)
                                      {
                                          for (int i = dataGrid.Columns.Count - 1; i >= Constants.StartConditionsParamersInDataGridIndex; i--)
                                          {
                                              DataGridColumn column = dataGrid.Columns[i];

                                              if (IsCPColumn(column))
                                                  dataGrid.Columns.Remove(column);
                                          }
                                      }
                                  }
                                );
        }

        public static void ValuesToRowAssemblyProtocols(System.Data.SqlClient.SqlDataReader reader, DynamicObj item)
        {
            if ((reader != null) && (item != null))
            {
                string name = Common.Constants.AssemblyProtocolID;
                int index = reader.GetOrdinal(name);
                int? assemblyProtocolID = int.TryParse(Convert.ToString(reader[index]), out int intValue) ? (int?)intValue : null;
                SaveValue(false, item, name, assemblyProtocolID);

                name = Common.Constants.Descr;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Ts;
                index = reader.GetOrdinal(name);
                DateTime? ts = DateTime.TryParse(Convert.ToString(reader[index]), out DateTime dateTimeValue) ? (DateTime?)dateTimeValue : null;
                SaveValue(false, item, name, ts);

                name = Common.Constants.RecordCount;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Usr;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.DeviceModeView;
                index = reader.GetOrdinal(name);
                bool? deviceModeView = bool.TryParse(Convert.ToString(reader[index]), out bool boolValue) ? (bool?)boolValue : null;
                SaveValue(false, item, name, deviceModeView);

                name = Common.Constants.AssemblyJob;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Export;
                index = reader.GetOrdinal(name);
                bool? export = bool.TryParse(Convert.ToString(reader[index]), out boolValue) ? (bool?)boolValue : null;
                SaveValue(false, item, name, export);

                name = Common.Constants.DeviceTypeRU;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.DeviceTypeEN;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.AverageCurrent;
                index = reader.GetOrdinal(name);
                int? averageCurrent = int.TryParse(Convert.ToString(reader[index]), out intValue) ? (int?)intValue : null;
                SaveValue(false, item, name, averageCurrent);

                name = Common.Constants.Constructive;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.DeviceClass;
                index = reader.GetOrdinal(name);
                int? deviceClass = int.TryParse(Convert.ToString(reader[index]), out intValue) ? (int?)intValue : null;
                SaveValue(false, item, name, deviceClass);

                name = Common.Constants.SqlDUdt;
                index = reader.GetOrdinal(name);
                int? dUdt = int.TryParse(Convert.ToString(reader[index]), out intValue) ? (int?)intValue : null;
                SaveValue(false, item, name, dUdt);

                name = Common.Constants.Trr;
                index = reader.GetOrdinal(name);
                double? trr = double.TryParse(Convert.ToString(reader[index]), out double doubleValue) ? (double?)doubleValue : null;
                SaveValue(false, item, name, trr);

                name = Common.Constants.Tq;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Tgt;
                index = reader.GetOrdinal(name);
                double? tgt = double.TryParse(Convert.ToString(reader[index]), out doubleValue) ? (double?)doubleValue : null;
                SaveValue(false, item, name, tgt);

                name = Common.Constants.Qrr;
                index = reader.GetOrdinal(name);
                int? qrr = int.TryParse(Convert.ToString(reader[index]), out intValue) ? (int?)intValue : null;
                SaveValue(false, item, name, qrr);

                name = Common.Constants.Climatic;
                index = reader.GetOrdinal(name);
                SaveValue(false, item, name, Convert.ToString(reader[index]));

                name = Common.Constants.Omnity;
                index = reader.GetOrdinal(name);
                int? omnity = int.TryParse(Convert.ToString(reader[index]), out intValue) ? (int?)intValue : null;
                SaveValue(false, item, name, omnity);
            }
        }

        private static string ValueByFieldName(DynamicObj item, string fieldName)
        {
            return item.GetMember(fieldName, out object value) ? value?.ToString() : null;
        }

        private static string OmnityFromItem(string item)
        {
            //извлекаем из обозначения вида 0.2A.1.080.070.0460.TG803.A7 значение омности
            return StringByIndex(item, ".", 4);
        }

        public static string MostPopularValue(List<DynamicObj> data, string fieldName)
        {
            //вычисляет наиболее часто используемое значение хранящееся в поле fieldName списка data
            switch (fieldName)
            {
                //требуется вычислить наиболее часто используемое значение омности - т.е. части обозначения ITEM
                case Common.Constants.Omnity:
                    return data.GroupBy(i => OmnityFromItem(ValueByFieldName(i, Common.Constants.Item))).OrderByDescending(g => g.Count()).Select(g => g.Key).First();

                default:
                    return data.GroupBy(i => ValueByFieldName(i, fieldName)).OrderByDescending(g => g.Count()).Select(g => g.Key).First();
            }
        }

        public static string MinValue(List<DynamicObj> data, string fieldName)
        {
            //вычисляет минимальное значение хранящееся в поле fieldName списка data
            double StrToDouble(string str)
            {
                return double.TryParse(str, out double result) ? result : 0;
            }

            return data.Min(i => StrToDouble(ValueByFieldName(i, fieldName))).ToString();
        }

        private static string RemoveNonNumericChars(string text)
        {
            char[] numericChars = "0123456789,.".ToCharArray();

            return new String(text.Where(c => numericChars.Any(n => n == c)).ToArray());
        }

        public static string MinCPValue(List<DynamicObj> data, string fieldName)
        {
            //данная реализация предназначена для CP (condition/parameter)
            //вычисляет минимальное значение хранящееся в поле fieldName списка data
            //при этом проверяет каждую запись на возможность хранить несколько значений в поле имя которого содержит fieldName
            double? MinValueByItem(DynamicObj item)
            {
                double? minValue = null;

                //из принятого item получаем список полей, имена которых содержат fieldName
                IEnumerable<string> names = item.GetDynamicMemberNames().Where(n => n.Contains(fieldName.ToLower()) && !IsColumnHidden(n));

                if (names != null)
                {
                    //ищем в item минимальное значение в полях из списка names                
                    foreach (string name in names)
                    {
                        string sValue = ValueByFieldName(item, name);

                        //выбрасываем из sValue всё, что не является числом
                        sValue = RemoveNonNumericChars(sValue);

                        if (double.TryParse(sValue, out double dValue))
                        {
                            if (minValue == null)
                            {
                                minValue = dValue;
                            }
                            else
                            {
                                if (dValue < (double)minValue)
                                    minValue = dValue;
                            }
                        }
                    }
                }

                return minValue;
            }

            return data.Min(i => MinValueByItem(i)).ToString();
        }

        public static string MaxCPValue(List<DynamicObj> data, string fieldName)
        {
            //данная реализация предназначена для CP (condition/parameter)
            //вычисляет максимальное значение хранящееся в поле fieldName списка data
            //при этом проверяет каждую запись на возможность хранить несколько значений в поле имя которого содержит fieldName
            double? MaxValueByItem(DynamicObj item)
            {
                double? maxValue = null;

                //из принятого item получаем список полей, имена которых содержат fieldName
                IEnumerable<string> names = item.GetDynamicMemberNames().Where(n => n.Contains(fieldName.ToLower()) && !IsColumnHidden(n));

                if (names != null)
                {
                    //ищем в item максимальное значение в полях из списка names                
                    foreach (string name in names)
                    {
                        string sValue = ValueByFieldName(item, name);

                        if (!string.IsNullOrEmpty(sValue))
                        {
                            //выбрасываем из sValue всё, что не является числом
                            sValue = RemoveNonNumericChars(sValue);

                            if (double.TryParse(sValue, out double dValue))
                            {
                                if (maxValue == null)
                                {
                                    maxValue = dValue;
                                }
                                else
                                {
                                    if (dValue > (double)maxValue)
                                        maxValue = dValue;
                                }
                            }
                        }
                    }
                }

                return maxValue;
            }

            return data.Max(i => MaxValueByItem(i))?.ToString();
        }

        public static int? CalcQrr(string deviceTypeRu, string constructive, int? itav)
        {
            //табличное вычисление значения Qrr по типу, конструктиву и среднему току
            QrrGroupDescr qrr = (string.IsNullOrEmpty(deviceTypeRu) || string.IsNullOrEmpty(constructive) || (itav == null)) ? null : QrrGroups.Where(n => (n.DeviceTypeRu == deviceTypeRu) && (n.Constructive == constructive) && (n.Itav == itav)).FirstOrDefault();

            return (qrr == null) ? null : (int?)qrr.Value;
        }

        public static NrmStatus IsInNrm(DynamicObj item, string sourceFieldName)
        {
            //sourceFieldName - имя столбца в item
            //возвращает:
            //CheckNrmStatus.UnCheckable - проверка норм не имеет смысла
            //CheckNrmStatus.Good - значение в пределах нормы
            //CheckNrmStatus.Defective - значение вне нормы
            //CheckNrmStatus.NotSetted - нормы не установлены

            NrmStatus result = NrmStatus.UnCheckable;
            if (item != null)
            {
                //считываем индекс текущего столбца 
                List<string> columnsList = item.GetDynamicMemberNames().ToList();
                string columnName = sourceFieldName.ToLower();
                int columnIndex = columnsList.IndexOf(columnName);

                //считываем индекс самого первого параметра в данном item. в каждом item своё значение начального индекса параметров изделия
                if (item.GetMember(Constants.ParametersInDataSourceFirstIndex, out object parametersInDataSourceFirstIndex))
                {
                    //считываем индекс самого первого условия в данном item
                    if (item.GetMember(Constants.ConditionsInDataSourceFirstIndex, out object conditionsInDataSourceFirstIndex))
                    {
                        //мы имеем дело с условиями профиля - норм для них не существуют в принципе
                        if ((columnIndex >= (int)conditionsInDataSourceFirstIndex) && (columnIndex < (int)parametersInDataSourceFirstIndex))
                            return NrmStatus.LegallyAbsent;
                    }

                    //мы имеем дело с параметрами изделия
                    //нормы могут иметь данные с индексами начиная с paramersInDataSourceFirstIndex, максимальное значение индекса не ограничено, т.к. параметры изделия это последнее, что может хранится в item
                    if (columnIndex >= (int)parametersInDataSourceFirstIndex)
                    {
                        //читаем значение параметра изделия
                        if (item.GetMember(columnName, out object value))
                        {
                            if (value != DBNull.Value)
                            {
                                //считываем значение нормы min
                                string nameOfNrmMinParametersColumn = NameOfNrmMinParametersColumn(columnName).ToLower();

                                if (item.GetMember(nameOfNrmMinParametersColumn, out object nrmMin))
                                {
                                    if (nrmMin == DBNull.Value)
                                    {
                                        result = NrmStatus.NotSetted;
                                    }
                                    else
                                    {
                                        double dValue = double.Parse(value.ToString());
                                        double dNrmMin = double.Parse(nrmMin.ToString());

                                        result = (dNrmMin <= dValue) ? NrmStatus.Good : NrmStatus.Defective;

                                        if (result == NrmStatus.Defective)
                                            return result;
                                    }
                                }
                                else
                                    result = NrmStatus.NotSetted;

                                //считываем значения нормы max
                                string nameOfNrmMaxParametersColumn = NameOfNrmMaxParametersColumn(columnName).ToLower();

                                if (item.GetMember(nameOfNrmMaxParametersColumn, out object nrmMax))
                                {
                                    if (nrmMax == DBNull.Value)
                                    {
                                        if (result == NrmStatus.NotSetted)
                                            return NrmStatus.NotSetted;
                                    }
                                    else
                                    {
                                        //проверяем не является ли описание нормы Max описанием для проверки Boolean значения
                                        if ((nrmMin == DBNull.Value) && (IsBoolean(nrmMax.ToString())))
                                        {
                                            //имеем дело с описанием норм на Boolean значение
                                            bool bValue = Convert.ToBoolean(value);
                                            bool bNrmMax = Convert.ToBoolean(nrmMax);

                                            result = (bValue == bNrmMax) ? NrmStatus.Good : NrmStatus.Defective;

                                            if (result == NrmStatus.Defective)
                                                return result;
                                        }
                                        else
                                        {
                                            //имеем дело с описанием норм на float параметры
                                            double dValue = double.Parse(value.ToString());
                                            double dNrmMax = double.Parse(nrmMax.ToString());

                                            result = (dValue <= dNrmMax) ? NrmStatus.Good : NrmStatus.Defective;

                                            if (result == NrmStatus.Defective)
                                                return result;
                                        }
                                    }
                                }
                                else
                                {
                                    if (result == NrmStatus.NotSetted)
                                        return NrmStatus.NotSetted;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static bool? CheckValuesFromRegularFields(DynamicObj item, string sourceFieldName)
        {
            //проверка значений простых (не являющихся conditions и parameters) реквизитов с целью раскраски для пользователя
            //возвращает:
            // null - значение не подлежит оценке;
            // true - хорошее значение;
            // false - плохое значение
            bool? result = null;
            string columnName = sourceFieldName.ToLower();

            switch (columnName)
            {
                case "status":
                    if (item.GetMember(columnName, out object value))
                    {
                        if (value != DBNull.Value)
                            result = value.ToString() == Constants.GoodSatatus;
                    }

                    break;
            }

            return result;
        }

        public static string[] AssemblyProtocolParametersByDeviceTypeRu(string deviceTypeRu)
        {
            //для принятого типа изделия deviceTypeRu возвращает список имён параметров, которые пользователь хочет видеть в протоколе сборки
            string[] result = null;

            if (!string.IsNullOrEmpty(deviceTypeRu))
            {
                switch (deviceTypeRu)
                {
                    case "Д":
                        result = new string[] { "UBR", "ITM", "UTM", "URRM", "IRRM" };
                        break;

                    case "ДЛ":
                        result = new string[] { "UBR", "ITM", "UTM", "URRM", "IRRM", "PRSM" };
                        break;

                    case "ДЧ":
                        result = new string[] { "UBR", "ITM", "UTM", "URRM", "IRRM", Common.Constants.Trr, "URSM", "IRSM" };
                        break;

                    case "ДЧЛ":
                        result = new string[] { "UBR", "ITM", "UTM", "URRM", "IRRM", "PRSM", Common.Constants.Trr, Common.Constants.Qrr, "URSM", "IRSM" };
                        break;

                    case "МД":
                    case "МДТ":
                        result = new string[] { "UBR", "ITM", "UTM", "URSM", "URRM", "IRRM", "IRSM" };
                        break;

                    case "МДЧ":
                        result = new string[] { Common.Constants.Trr, Common.Constants.Qrr, "IRRM", "UBR", "ITM", "UTM", "URSM", "URRM", "IRRM", "IRSM" };
                        break;

                    case "МДЧЛ":
                        result = new string[] { "UBR", "ITM", "UTM", "URRM", "URSM", "IRRM", "PRSM", Common.Constants.Trr, Common.Constants.Qrr, "IRRM", "IRSM" };
                        break;

                    case "МТ":
                    case "МТД":
                        result = new string[] { "UBO", "UBR", "UDSM", "URSM", "ITM", "UTM", "UDRM", "URRM", "IDRM", "IRRM", Common.Constants.Tq, "IGT", "UGT", Common.Constants.DUdt.ToUpper(), "IDSM", "IRSM" };
                        break;

                    case "МТБ":
                    case "МТБЧ":
                    case "МТБИ":
                        result = new string[] { "UBO", "UBR", "UDSM", "URSM", "ITM", "UTM", "UDRM", "URRM", "IDRM", "IRRM", "IGT", "UGT", Common.Constants.Tq, Common.Constants.Trr, Common.Constants.Qrr, "IRR", Common.Constants.DUdt.ToUpper(), "IDSM", "IRSM" };
                        break;

                    case "Т":
                        result = new string[] { "UBO", "UBR", "ITM", "UTM", "UDRM", "URRM", "IDRM", "IRRM", Common.Constants.Tq, "IGT", "UGT", Common.Constants.DUdt.ToUpper() };
                        break;

                    case "ТБ":
                    case "ТБИ":
                    case "ТБЧ":
                        result = new string[] { "UBO", "UBR", "ITM", "UTM", "UDRM", "URRM", "IDRM", "IRRM", Common.Constants.Tq, Common.Constants.Trr, Common.Constants.Qrr, "IRR", Common.Constants.Tgt, "IGT", "UGT", Common.Constants.DUdt.ToUpper() };
                        break;

                    case "ТЛ":
                        result = new string[] { "UBO", "UBR", "ITM", "UTM", "UDRM", "URRM", "IDRM", "IRRM", "PRSM", Common.Constants.Tq, "IGT", "UGT", Common.Constants.DUdt.ToUpper() };
                        break;
                }
            }

            return result;
        }

        public class ParamConditionDescr
        {
            public ParamConditionDescr(string temperatureMode, string descr, string unitMeasure)
            {
                this.TemperatureMode = temperatureMode;
                this.Descr = descr;
                this.UnitMeasure = unitMeasure;
            }

            public string TemperatureMode { get; }
            public string Descr { get; }
            public string UnitMeasure { get; }
        }

        public class ParamConditionDescrFirstLine : ParamConditionDescr
        {
            public ParamConditionDescrFirstLine(string temperatureMode, string descr, string unitMeasure) : base(temperatureMode, descr, unitMeasure)
            {
            }
        }

        public class ParamConditionDescrSecondLine : ParamConditionDescr
        {
            public ParamConditionDescrSecondLine(string temperatureMode, string descr, string unitMeasure) : base(temperatureMode, descr, unitMeasure)
            {
            }
        }

        public static IEnumerable<ParamConditionDescr> AssemblyProtocolEmptyNamesByDeviceTypeRu(string deviceTypeRu)
        {
            //для принятого типа изделия deviceTypeRu возвращает список имён параметров/условий, которые пользователь хочет видеть в отчёте протокола сборки с пустыми значениями, которые заполнит пользователь (шапка таблицы)
            switch (deviceTypeRu)
            {
                case "МТ":
                case "МТБ":
                case "МТБЧ":
                case "МТД":
                case "Т":
                case "ТБ":
                case "ТБИ":
                case "ТБЧ":
                case "ТЛ":
                    //ITM, UDRM, URRM создаём как ParamConditionDescrSecondLine, остальные как ParamConditionDescrFirstLine с целью различения при выводе в отчёт
                    return new ParamConditionDescr[] { new ParamConditionDescrFirstLine("RT", "UBO", "В"), new ParamConditionDescrFirstLine("RT", "UBR", "В"), new ParamConditionDescrSecondLine("RT", "ITM", "А"), new ParamConditionDescrFirstLine("RT", "UTM", "В"), new ParamConditionDescrFirstLine("RT", "IGT", "мА"), new ParamConditionDescrFirstLine("RT", "UGT", "В"), new ParamConditionDescrSecondLine("TM", "UDRM", "В"), new ParamConditionDescrSecondLine("TM", "URRM", "В"), new ParamConditionDescrFirstLine("TM", "IDRM", "мА"), new ParamConditionDescrFirstLine("TM", "IRRM", "мА"), new ParamConditionDescrFirstLine("TM", Common.Constants.DUdt, "В/мкс"), new ParamConditionDescrFirstLine("TM", "UDSM", "В"), new ParamConditionDescrFirstLine("TM", "URSM", "В") };

                case "Д":
                case "ДЛ":
                case "ДЧ":
                case "ДЧЛ":
                case "МД":
                case "МДТ":
                case "МДЧ":
                case "МДЧЛ":
                    //ITM, URRM создаём как ParamConditionDescrSecondLine, остальные как ParamConditionDescrFirstLine с целью различения при выводе в отчёт
                    return new ParamConditionDescr[] { new ParamConditionDescrFirstLine("RT", "UBR", "В"), new ParamConditionDescrSecondLine("RT", "ITM", "А"), new ParamConditionDescrFirstLine("RT", "UFM", "В"), new ParamConditionDescrSecondLine("RT", "IFM", "А"), new ParamConditionDescrSecondLine("TM", "URRM", "В"), new ParamConditionDescrFirstLine("TM", "IRRM", "мА"), new ParamConditionDescrFirstLine("TM", "URSM", "В") };
            }

            return null;
        }

        public class GroupDescr
        {
            //вариант конструктора для Tq
            public GroupDescr(string trueValue, string descr, string num)
            {
                this.TrueValue = trueValue;
                this.Descr = descr;
                this.Num = num;
            }

            //вариант конструктора для DUDt, Trr, TqOffTrTg, TgtOn
            public GroupDescr(string descr, string num)
            {
                this.TrueValue = null;
                this.Descr = descr;
                this.Num = num;
            }

            public string TrueValue { get; }
            public string Descr { get; }
            public string Num { get; }
        }

        public static Dictionary<int, GroupDescr> DUDtGroups => new Dictionary<int, GroupDescr>
        {
                   { 20,   new GroupDescr("Р3", "1") },
                   { 50,   new GroupDescr("Е3", "2") },
                   { 100,  new GroupDescr("А3", "3") },
                   { 200,  new GroupDescr("Р2", "4") },
                   { 320,  new GroupDescr("К2", "5") },
                   { 500,  new GroupDescr("Е2", "6") },
                   { 1000, new GroupDescr("А2", "7") },
                   { 1600, new GroupDescr("Т1", "8") },
                   { 2000, new GroupDescr("Р1", "" ) },
                   { 2500, new GroupDescr("М1", "9") },
                   { 3200, new GroupDescr("К1", "" ) },
                   { 4000, new GroupDescr("Н1", "" ) },
                   { 5000, new GroupDescr("Е1", "" ) },
                   { 6300, new GroupDescr("С1", "" ) },
                   { 8000, new GroupDescr("В1", "" ) }
        };

        public static Dictionary<double, GroupDescr> TrrGroups => new Dictionary<double, GroupDescr>
        {
                   {0.2,  new GroupDescr("Р5", "" ) },
                   {0.25, new GroupDescr("М5", "" ) },
                   {0.32, new GroupDescr("К5", "" ) },
                   {0.4,  new GroupDescr("Н5", "9") },
                   {0.5,  new GroupDescr("Е5", "" ) },
                   {0.63, new GroupDescr("С5", "8") },
                   {0.8,  new GroupDescr("В5", "" ) },
                   {1,    new GroupDescr("А5", "7") },
                   {1.25, new GroupDescr("Х4", "" ) },
                   {1.6,  new GroupDescr("Т4", "6") },
                   {2,    new GroupDescr("Р4", "5") },
                   {2.5,  new GroupDescr("М4", "4") },
                   {3.2,  new GroupDescr("К4", "3") },
                   {4,    new GroupDescr("Н4", "2") },
                   {5,    new GroupDescr("Е4", "1") },
                   {6.3,  new GroupDescr("С4", "" ) },
                   {8,    new GroupDescr("В4", "" ) },
                   {10,   new GroupDescr("А4", "" ) }
        };

        public static Dictionary<string, GroupDescr> TqGroups => new Dictionary<string, GroupDescr>
        {
                   {"TI,TS:   0.5", new GroupDescr("0.5",  "Е5", "" ) },
                   {"TI,TS:   0.8", new GroupDescr("0.8",  "В5", "" ) },
                   {"TI,TS:   1.25",new GroupDescr("1.25", "Х4", "" ) },
                   {"TI,TS:   2",   new GroupDescr("2",    "Р4", "" ) },
                   {"TI,TS:   3.2", new GroupDescr("3.2",  "К4", "" ) },
                   {"TI,TS:   5",   new GroupDescr("5",    "Е4", "" ) },
                   {"TI,TS:   6.3", new GroupDescr("6.3",  "С4", "" ) },
                   {"TI,TS:   8",   new GroupDescr("8",    "В4", "9") },
                   {"TI,TS:  10",   new GroupDescr("10",   "А4", "" ) },
                   {"TI,TS:  12.5", new GroupDescr("12.5", "Х3", "8") },
                   {"TI,TS:  16",   new GroupDescr("16",   "Т3", "7") },
                   {"TI,TS:  20",   new GroupDescr("20",   "Р3", "6") },
                   {"TI,TS:  25",   new GroupDescr("25",   "М3", "5") },
                   {"TI,TS:  32",   new GroupDescr("32",   "К3", "4") },
                   {"TI,TS:  40",   new GroupDescr("40",   "Н3", "3") },
                   {"TI,TS:  50",   new GroupDescr("50",   "Е3", "2") },
                   {"TI,TS:  63",   new GroupDescr("63",   "С3", "1") },
                   {"TI,TS:   -",   new GroupDescr("",     "",   "0") },

                   {"TR,TG:   40",  new GroupDescr("40",   "Н3", "" ) },
                   {"TR,TG:   50",  new GroupDescr("50",   "Е3", "" ) },
                   {"TR,TG:   63",  new GroupDescr("63",   "С3", "5") },
                   {"TR,TG:   80",  new GroupDescr("80",   "В3", "" ) },
                   {"TR,TG:  100",  new GroupDescr("100",  "А3", "4") },
                   {"TR,TG:  125",  new GroupDescr("125",  "Х2", "" ) },
                   {"TR,TG:  160",  new GroupDescr("160",  "Т2", "3") },
                   {"TR,TG:  200",  new GroupDescr("200",  "Р2", "" ) },
                   {"TR,TG:  250",  new GroupDescr("250",  "М2", "2") },
                   {"TR,TG:  320",  new GroupDescr("320",  "К2", "" ) },
                   {"TR,TG:  400",  new GroupDescr("400",  "Н2", "" ) },
                   {"TR,TG:  500",  new GroupDescr("500",  "Е2", "1") },
                   {"TR,TG:  630",  new GroupDescr("630",  "С2", "" ) },
                   {"TR,TG:  800",  new GroupDescr("800",  "В2", "" ) },
                   {"TR,TG:    -",  new GroupDescr("",     "",   "0") }
        };

        /*
        public static Dictionary<double, GroupDescr> TqOffTrTgGroups => new Dictionary<double, GroupDescr>
        {
                   {40,  new GroupDescr("Н3", "-") },
                   {50,  new GroupDescr("Е3", "-") },
                   {63,  new GroupDescr("С3", "5") },
                   {80,  new GroupDescr("В3", "-") },
                   {100, new GroupDescr("А3", "4") },
                   {125, new GroupDescr("Х2", "-") },
                   {160, new GroupDescr("Т2", "3") },
                   {200, new GroupDescr("Р2", "-") },
                   {250, new GroupDescr("М2", "2") },
                   {320, new GroupDescr("К2", "-") },
                   {400, new GroupDescr("Н2", "-") },
                   {500, new GroupDescr("Е2", "1") },
                   {630, new GroupDescr("С2", "-") },
                   {800, new GroupDescr("В2", "-") }
        };
        */

        public static Dictionary<double, GroupDescr> TgtOnGroups => new Dictionary<double, GroupDescr>
        {
                   {0.08, new GroupDescr("В6", "" ) },
                   {0.1,  new GroupDescr("А6", "" ) },
                   {0.16, new GroupDescr("Т5", "" ) },
                   {0.25, new GroupDescr("М5", "" ) },
                   {0.4,  new GroupDescr("Н5", "9") },
                   {0.63, new GroupDescr("С5", "8") },
                   {1,    new GroupDescr("А5", "7") },
                   {1.25, new GroupDescr("Х4", "6") },
                   {1.6,  new GroupDescr("Т4", "5") },
                   {2,    new GroupDescr("Р4", "4") },
                   {2.5,  new GroupDescr("М4", "3") },
                   {3.2,  new GroupDescr("К4", "2") },
                   {4,    new GroupDescr("Н4", "1") },
                   {6.3,  new GroupDescr("С4", "" ) },
                   {8,    new GroupDescr("В4", "" ) },
                   {10,   new GroupDescr("А4", "" ) },
                   {16,   new GroupDescr("Т3", "" ) }
        };

        public class QrrGroupDescr
        {
            public QrrGroupDescr(int value, string deviceTypeRu, string constructive, int itav)
            {
                this.Value = value;
                this.DeviceTypeRu = deviceTypeRu;
                this.Constructive = constructive;
                this.Itav = itav;
            }

            public int Value { get; }
            public string DeviceTypeRu { get; }
            public string Constructive { get; }
            public int Itav { get; }
        }

        public static List<QrrGroupDescr> QrrGroups => new List<QrrGroupDescr>
        {
                   { new QrrGroupDescr(60,   "ТБЧ", "133", 400 ) },
                   { new QrrGroupDescr(65,   "ДЧЛ", "233", 200 ) },
                   { new QrrGroupDescr(80,   "ТБЧ", "123", 200 ) },
                   { new QrrGroupDescr(80,   "ТБЧ", "143", 400 ) },
                   { new QrrGroupDescr(80,   "ТБЧ", "143", 500 ) },
                   { new QrrGroupDescr(80,   "ТБЧ", "343", 500 ) },
                   { new QrrGroupDescr(80,   "ТБЧ", "123", 200 ) },
                   { new QrrGroupDescr(100,  "ТБИ", "133", 400 ) },
                   { new QrrGroupDescr(100,  "ТБИ", "143", 400 ) },
                   { new QrrGroupDescr(100,  "ТБИ", "343", 400 ) },
                   { new QrrGroupDescr(100,  "ТБЧ", "153", 800 ) },
                   { new QrrGroupDescr(100,  "ТБИ", "433", 400 ) },
                   { new QrrGroupDescr(100,  "ТБИ", "543", 400 ) },
                   { new QrrGroupDescr(110,  "ДЧ",  "143", 500 ) },
                   { new QrrGroupDescr(120,  "ДЧ",  "233", 100 ) },
                   { new QrrGroupDescr(120,  "ДЧЛ", "133", 200 ) },
                   { new QrrGroupDescr(125,  "ТБИ", "261", 125 ) },
                   { new QrrGroupDescr(140,  "ДЧЛ", "333", 250 ) },
                   { new QrrGroupDescr(150,  "ТБИ", "175", 200 ) },
                   { new QrrGroupDescr(150,  "ТБИ", "261", 160 ) },
                   { new QrrGroupDescr(150,  "ТБИ", "271", 200 ) },
                   { new QrrGroupDescr(150,  "ТБИ", "333", 400 ) },
                   { new QrrGroupDescr(150,  "ТБЧ", "153", 1000) },
                   { new QrrGroupDescr(150,  "ТБИ", "371", 200 ) },
                   { new QrrGroupDescr(150,  "ТБИ", "533", 400 ) },
                   { new QrrGroupDescr(150,  "ТБЧ", "153", 1000) },
                   { new QrrGroupDescr(150,  "ДЧЛ", "133", 320 ) },
                   { new QrrGroupDescr(190,  "ДЧ",  "223", 320 ) },
                   { new QrrGroupDescr(190,  "ДЧ",  "133", 500 ) },
                   { new QrrGroupDescr(200,  "ТБИ", "143", 500 ) },
                   { new QrrGroupDescr(200,  "ТБИ", "153", 800 ) },
                   { new QrrGroupDescr(200,  "ТБИ", "175", 250 ) },
                   { new QrrGroupDescr(200,  "ТБИ", "271", 250 ) },
                   { new QrrGroupDescr(200,  "ТБИ", "343", 500 ) },
                   { new QrrGroupDescr(200,  "ТБИ", "371", 250 ) },
                   { new QrrGroupDescr(200,  "ТБИ", "543", 500 ) },
                   { new QrrGroupDescr(200,  "ДЧЛ", "133", 320 ) },
                   { new QrrGroupDescr(200,  "ДЧЛ", "333", 320 ) },
                   { new QrrGroupDescr(220,  "ТБИ", "173", 2000) },
                   { new QrrGroupDescr(220,  "TБИ", "573", 2000) },
                   { new QrrGroupDescr(250,  "ТБИ", "143", 630 ) },
                   { new QrrGroupDescr(250,  "ТБИ", "233", 320 ) },
                   { new QrrGroupDescr(250,  "ТБИ", "271", 320 ) },
                   { new QrrGroupDescr(250,  "ТБИ", "333", 320 ) },
                   { new QrrGroupDescr(250,  "ТБИ", "343", 630 ) },
                   { new QrrGroupDescr(250,  "ТБИ", "543", 630 ) },
                   { new QrrGroupDescr(250,  "ДЧ",  "261", 250 ) },
                   { new QrrGroupDescr(250,  "ДЧ",  "261", 250 ) },
                   { new QrrGroupDescr(250,  "ДЧ",  "153", 1000) },
                   { new QrrGroupDescr(275,  "ДЧ",  "253", 630 ) },
                   { new QrrGroupDescr(300,  "ТБИ", "153", 1000) },
                   { new QrrGroupDescr(300,  "ТБИ", "243", 400 ) },
                   { new QrrGroupDescr(300,  "ТБИ", "243", 500 ) },
                   { new QrrGroupDescr(300,  "ТБИ", "443", 400 ) },
                   { new QrrGroupDescr(300,  "ТБИ", "443", 500 ) },
                   { new QrrGroupDescr(300,  "ТБИ", "643", 400 ) },
                   { new QrrGroupDescr(300,  "ТБИ", "643", 500 ) },
                   { new QrrGroupDescr(300,  "ДЧ",  "261", 320 ) },
                   { new QrrGroupDescr(300,  "ДЧ",  "261", 320 ) },
                   { new QrrGroupDescr(300,  "ДЧ",  "123", 320 ) },
                   { new QrrGroupDescr(300,  "ДЧ",  "243", 630 ) },
                   { new QrrGroupDescr(300,  "ДЧ",  "443", 630 ) },
                   { new QrrGroupDescr(310,  "ДЧ",  "153", 630 ) },
                   { new QrrGroupDescr(350,  "ТБИ", "153", 1250) },
                   { new QrrGroupDescr(350,  "ТБИ", "243", 630 ) },
                   { new QrrGroupDescr(350,  "ТБИ", "443", 630 ) },
                   { new QrrGroupDescr(350,  "ТБИ", "453", 1250) },
                   { new QrrGroupDescr(350,  "ТБИ", "643", 630 ) },
                   { new QrrGroupDescr(350,  "ДЧ",  "271", 400 ) },
                   { new QrrGroupDescr(350,  "ДЧ",  "271", 400 ) },
                   { new QrrGroupDescr(375,  "ДЧ",  "133", 320 ) },
                   { new QrrGroupDescr(400,  "ТБИ", "233", 400 ) },
                   { new QrrGroupDescr(400,  "ТБИ", "253", 800 ) },
                   { new QrrGroupDescr(420,  "ДЧ",  "353", 1000) },
                   { new QrrGroupDescr(440,  "ДЧ",  "233", 320 ) },
                   { new QrrGroupDescr(440,  "ДЧ",  "443", 320 ) },
                   { new QrrGroupDescr(450,  "ТБИ", "253", 1000) },
                   { new QrrGroupDescr(500,  "ТБИ", "933", 250 ) },
                   { new QrrGroupDescr(500,  "ДЧ",  "271", 500 ) },
                   { new QrrGroupDescr(500,  "ДЧ",  "271", 500 ) },
                   { new QrrGroupDescr(500,  "ДЧ",  "253", 1000) },
                   { new QrrGroupDescr(770,  "ТБИ", "353", 700 ) },
                   { new QrrGroupDescr(800,  "TБИ", "273", 2000) },
                   { new QrrGroupDescr(800,  "TБИ", "673", 2000) },
                   { new QrrGroupDescr(800,  "ДЧ",  "243", 1000) },
                   { new QrrGroupDescr(800,  "ДЧ",  "443", 800 ) },
                   { new QrrGroupDescr(800,  "ДЧ",  "453", 800 ) },
                   { new QrrGroupDescr(900,  "ТБИ", "253", 1250) },
                   { new QrrGroupDescr(1000, "ТБИ", "353", 1000) },
                   { new QrrGroupDescr(1000, "ТБИ", "353", 800 ) },
                   { new QrrGroupDescr(1200, "ТБИ", "193", 2500) },
                   { new QrrGroupDescr(1200, "ТБИ", "393", 2500) },
                   { new QrrGroupDescr(1250, "TБИ", "373", 1600) },
                   { new QrrGroupDescr(1250, "TБИ", "373", 2000) },
                   { new QrrGroupDescr(1250, "TБИ", "773", 2000) },
                   { new QrrGroupDescr(1250, "ТБИ", "773", 1600) },
                   { new QrrGroupDescr(3000, "TБИ", "473", 1600) },
                   { new QrrGroupDescr(3000, "ТБИ", "873", 1600) },
                   { new QrrGroupDescr(5000, "ДЧ",  "373", 2000) }
        };

        public class DeviceTypes : List<string[]>
        {
            public DeviceTypes()
            {
                DbRoutines.LoadDeviceTypes(this);
            }
        }

        public class ModificationGroups : List<string>
        {
            public ModificationGroups()
            {
                DbRoutines.LoadModificationsFromDeviceReferences(this);
            }
        }

        public class DataGridColumnDescr
        {
            private readonly string FNameInDB;
            public string NameInDB
            {
                get { return FNameInDB; }
            }

            private readonly string FNameInDataSource;
            public string NameInDataSource
            {
                get { return FNameInDataSource; }
            }

            private readonly string FNameInDataGrid;
            public string NameInDataGrid
            {
                get { return FNameInDataGrid; }
            }

            private readonly Common.Routines.XMLValues FSubject;
            public Common.Routines.XMLValues Subject
            {
                get { return FSubject; }
            }

            public DataGridColumnDescr(string nameInDB, string nameInDataSource, string nameInDataGrid, Common.Routines.XMLValues subject)
            {
                this.FNameInDB = nameInDB;
                this.FNameInDataSource = nameInDataSource;
                this.FNameInDataGrid = nameInDataGrid;
                this.FSubject = subject;
            }
        }



        /*
        public class ListOfDataGridColumnDescrs : List<DataGridColumnDescr>
        {
            private DataGridColumnDescr ColumnDescrByNameInDataSource(string name)
            {
                return this.Where(x => x.NameInDataSource == name).FirstOrDefault();
            }

            private DataGridColumnDescr ColumnDescrByNameInDataGrid(string name)
            {
                return this.Where(x => x.NameInDataGrid == name).FirstOrDefault();
            }

            public int IndexOfColumnDescrByNameInDataGrid(string name)
            {
                //ищет в себе такой элемент, в котором NameInDataGrid равно принятому name
                DataGridColumnDescr founded = this.ColumnDescrByNameInDataGrid(name);

                return this.IndexOf(founded);
            }

            public string NameInDBByNameInDataSource(string name)
            {
                DataGridColumnDescr founded = this.ColumnDescrByNameInDataSource(name);

                return founded?.NameInDB;
            }

            public XMLValues SubjectByNameInDataGrid(string name)
            {
                //читаем принадлежность столбца к condition/parameters по его имени в DataGrid
                DataGridColumnDescr founded = this.ColumnDescrByNameInDataGrid(name);

                return (founded == null) ? XMLValues.UnAssigned : founded.Subject;
            }

            public XMLValues SubjectByNameInDataSource(string name)
            {
                //читаем принадлежность столбца к condition/parameters по его имени в DataSource
                DataGridColumnDescr founded = this.ColumnDescrByNameInDataSource(name);

                return (founded == null) ? XMLValues.UnAssigned : founded.Subject;
            }
        }
        */



    }
}
