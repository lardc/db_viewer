using SCME.Types.Profiles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SCME.Common
{
    public static class Routines
    {
        public delegate void LoadProfileParameters(IEnumerable<string> profileParameters);

        public static TemperatureCondition TemperatureConditionByTemperature(double temperatureValue)
        {
            return (temperatureValue > 25) ? TemperatureCondition.TM : TemperatureCondition.RT;
        }

        public static string SourceFieldNameByColumn(DataGridColumn column)
        {
            if (column != null)
            {
                /*
                if (column is DataGridBoundColumn boundColumn)
                {
                    if (boundColumn.Binding is Binding bind)
                        return bind.Path.Path;
                }
                */

                return column.SortMemberPath;
            }

            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    System.Windows.DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
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

        public static bool IsKeyCtrlPressed()
        {
            return System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);
        }

        public static T FindParent<T>(System.Windows.DependencyObject item) where T : class
        {
            if (item is T)
            {
                return item as T;
            }
            else
            {
                System.Windows.DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(item);

                if (parent == null)
                {
                    return default(T);
                }
                else
                {
                    Type type = parent.GetType();

                    if ((type == typeof(T)) || type.IsSubclassOf(typeof(T)))
                    {
                        return parent as T;
                    }
                    else
                    {
                        return FindParent<T>(parent);
                    }
                }
            }
        }

        public static string ProcessName()
        {
            //возвращает имя процесса внутри которого работает данный вызов
            return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        }

        public static bool IsDouble(object value)
        {
            //return double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out double num);

            string checkedValue = SimpleFloatingValueToFloatingValue(Convert.ToString(value));

            return double.TryParse(checkedValue, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out double d);
        }

        /*
        public static double? StrToDouble(string value)
        {
            //преобразование принятой строки value в тип double? для любого системного разделителя целой от дробной части
            return double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out double d) ? (double?)d : null;
        }
        */

        public static string DoubleToStr(double value)
        {
            //преобразование принятого value в строку в которой разделитель целой от дробной части будет системным (как установлено в настройках Windows)
            NumberFormatInfo nfi = new NumberFormatInfo
            {
                NumberDecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator
            };

            string result = value.ToString(nfi);

            return result;
        }

        public static bool TryStringToDouble(string value, out double dValue)
        {
            if (value == null)
            {
                dValue = 0;

                return false;
            }

            return double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dValue);
        }

        public static char SystemDecimalSeparator()
        {
            return Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }

        public static bool IsInteger(string value, out int iValue, out bool isDouble, out double dValue)
        {
            //если value есть целое число - вернёт true, иначе false
            //в isDouble вернёт признак того, что принятый value успешно преобразуется к типу double
            bool result = int.TryParse(value, out iValue);

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

        public static int CountByChar(string value, char forSearch)
        {
            //вычисляет сколько раз в value содержится forSearch
            return value.Count(f => f == forSearch);
        }

        public static bool Like(string value, string forSearching)
        {
            //аналог SQL Like
            //выполяет поиск forSearching в value
            //смотрим как составлено поисковое выражение forSearching
            if (CountByChar(value, '%') == 1)
            {
                if (forSearching.StartsWith("%"))
                {
                    return value.EndsWith(forSearching.Substring(1));
                }
                else
                {
                    if (forSearching.EndsWith("%"))
                    {
                        return value.StartsWith(forSearching.Substring(0, forSearching.Length - 1));
                    }
                }
            }

            //превращаем строку toFind в массив строк, разделителем является символ "%"
            string[] subStrings = forSearching.Split(new[] { "%" }, StringSplitOptions.None);

            //если хотя бы одна из полученных подсток не содержится в value - возвращаем False
            foreach (string sub in subStrings)
            {
                if (!value.Contains(sub))
                    return false;
            }

            return true;
        }

        public static string DateByDateTime(DateTime value)
        {
            return value.ToString("dd.MM.yyyy");
        }

        public static bool IsTimeExistInDateTime(string value)
        {
            //проверяет наличие времени в принятом value
            //возвращает:
            //true - в принятом value есть информация о времени;
            //false - в принятом value информация о времени отсутствует

            if (DateTime.TryParse(value, out DateTime dt))
                return ((dt.Hour == 0) && (dt.Minute == 0) && (dt.Second == 0) && (dt.Millisecond == 0)) ? false : true;

            return false;
        }

        public static DateTime? StringToDateZeroTime(string value)
        {
            //преобразует принятое value в DateTime с обнулённым значением времени           

            if (DateTime.TryParse(value, out DateTime dt))
                return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);

            return null;
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

        public static bool CheckGroupNameByMask(string groupName)
        {
            //проверяет принятый groupName на соответствие маске 8-XXXXXXXX-XXXX
            //возвращает true - принятый groupName соответствует маске;
            //           false - принятый groupName не соответствует маске
            Regex r = new Regex("^[8][-]\\d\\d\\d\\d\\d\\d\\d\\d[-]\\d\\d\\d\\d$");

            return r.IsMatch(groupName);
        }

        public static bool CheckAssemblyJobByMask(string assemblyjob, out string mask)
        {
            //проверяет принятый assemblyjob на соответствие маске 8-XXXXXXXX-XXXX
            //возвращает true - принятый assemblyjob соответствует маске;
            //           false - принятый assemblyjob не соответствует маске
            //в mask возвращается маска, соответствие которой проверяется
            mask = "8-XXXXXXXX-XXXX";
            Regex r = new Regex("^[8][-]\\d\\d\\d\\d\\d\\d\\d\\d[-]\\d\\d\\d\\d$");

            return r.IsMatch(assemblyjob);
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
            //вырезает все цифры начиная с конца принятого value и возвращает value без цифр
            char[] trimChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            return value.TrimEnd(trimChars);
        }

        private static bool IsModuleClampingPrefix(string item)
        //вычисляет префикс модуля прижимного по коду изделия Item
        //возвращает True если вычисленный префикс соответствует префиксу модуля прижимного
        {
            //вычисляем префикс модуля прижимного
            string moduleClampingPrefix = item.Substring(2, 2).ToUpper(); //UpperCase(Copy(Item, 3, 2));

            return (
                     (moduleClampingPrefix == "MA") ||
                       (moduleClampingPrefix == "MC") ||
                         (moduleClampingPrefix == "MD") ||
                           (moduleClampingPrefix == "ME") ||
                             (moduleClampingPrefix == "MF") ||
                             (moduleClampingPrefix == "MB")
                   );
        }

        private static bool IsModuleSolderingPrefix(string item)
        //вычисляет префикс модуля паяного по коду изделия item
        //возвращает True если вычисленный префикс соответствует префиксу модуля паяного
        {
            //вычисляем префикс модуля паяного
            string moduleSolderingPrefix = item.Substring(2, 2).ToUpper(); //  UpperCase(Copy(Item, 3, 2));

            return (moduleSolderingPrefix == "MI");
        }

        private static int ParametersCount(string parameters, string delimeter)
        {
            string[] arr = parameters.Split(new[] { delimeter }, StringSplitOptions.None);

            return arr.Count();
        }

        private static string ParameterByIndex(string parameters, string delimeter, int parameterIndex)
        //возвращает значение параметра по его индексу в списке parameters
        //нумерация индексов начинается с 0
        {
            string[] arr = parameters.Split(new[] { delimeter }, StringSplitOptions.None);
            string result = arr[parameterIndex];

            return result;
        }

        public static string PackageTypeByItem(string item)
        //вычисление значения 'тип корпуса' по коду изделия
        {
            const string noData = "н/д";
            string result = null;

            try
            {
                if (string.IsNullOrEmpty(item))
                    return null;

                string productType = item.Substring(0, 3); //Copy(Item, 1, 3);

                if (productType == "0.2")
                {
                    return "ОХЛ";
                }
                else
                {
                    productType = item.Substring(0, 2); //Copy(Item, 1, 2);

                    if (productType == "1.")
                        productType = "-1";

                    //первый символ 'n' для возможности вычисления типа корпуса подменяем на 1
                    if (char.ToLower(productType[0]) == 'n') //LowerCase(ProductType[1])='n' then ProductType[1]:='1';
                        productType = string.Concat("1", productType.Substring(1));

                    //убедимся, что ProductType преобразуется в тип Integer без исключит. ситуации
                    if (int.TryParse(productType, out int iProductType))
                    {
                        switch (iProductType)
                        {
                            case 1:
                            case 11:
                                result = string.Concat(item.Substring(2, 1), ".", ParameterByIndex(item, ".", 4), ".", item.Substring(13, 1));  //Copy(Item, 3, 1)+'.'+ParametersByIndex(Item, '.', 4)+'.'+Copy(Item, 14, 1);
                                break;

                            case 2:
                            case 12:
                                result = string.Concat(item.Substring(2, 1), ".", item.Substring(18, 2)); // 3, 1)+'.'+Copy(Item, 19, 2);
                                break;

                            case 3:
                            case 13:
                                if (IsModuleSolderingPrefix(item))
                                {
                                    result = item.Substring(2, 4); // 3, 4);
                                }
                                else
                                {
                                    if (IsModuleClampingPrefix(item))
                                    {
                                        result = string.Concat(item.Substring(2, 3), ".", item.Substring(6, 1), item.Substring(8, 1), item.Substring(11, 1));  //Copy(Item, 3, 3)+'.'+Copy(Item, 7, 1)+Copy(Item, 9, 1)+Copy(Item, 12, 1);
                                    }
                                    else
                                        return noData;
                                }
                                break;


                            case 14:
                                return "ППЭ";

                            case 5:
                            case 15:
                                result = string.Concat("D.", item.Substring(13, 2)); //'D.'+Copy(Item, 14, 2);
                                break;

                            case 6:
                            case 16:
                                result = string.Concat(item.Substring(2, 1), ".", item.Substring(14, 2)); //Copy(Item, 3, 1)+'.'+Copy(Item, 15, 2);
                                break;

                            case 7:
                            case 17:
                            case -1:
                                return "ОХЛ";

                            case 8:
                            case 18:
                                //в этом случае могут быть как модули, так и сборки силовые. будем различать эти случаи по количеству подстрок в коде изделия
                                int subStringsCount = ParametersCount(item, ".");

                                if (subStringsCount >= 5)
                                {
                                    if (IsModuleClampingPrefix(item))
                                    {
                                        result = string.Concat(item.Substring(2, 3), ".", item.Substring(6, 1), item.Substring(8, 1), item.Substring(11, 1));    //Copy(Item, 3, 3)+'.'+Copy(Item, 7, 1)+Copy(Item, 9, 1)+Copy(Item, 12, 1);
                                    }
                                    else
                                    {
                                        if (item.Substring(18, 1) == "S") //(Copy(Item, 19, 1)='S') 
                                        {
                                            result = string.Concat(item.Substring(2, 1), ".", item.Substring(18, 3));  //Copy(Item, 3, 1)+'.'+Copy(Item, 19, 3);
                                        }
                                        else
                                        {
                                            if (item.ToUpper().Substring(2, 2) == "DS") //(UpperCase(Copy(Item, 3, 2))='DS') of
                                            {
                                                //для всех *DS* (пример 18DS32.18.200.B2.N.004) тип корпуса должен вычисляться так. третий символ кода + точка + 15 и 16 символы кода
                                                result = string.Concat(item.Substring(2, 1), ".", item.Substring(14, 2)); //Copy(Item, 3, 1)+'.'+Copy(Item, 15, 2);
                                            }
                                            else
                                                result = string.Concat(item.Substring(2, 1), ".", item.Substring(18, 2)); //Copy(Item, 3, 1)+'.'+Copy(Item, 19, 2);
                                        }
                                    }
                                }
                                else
                                    return "СБ СИЛ";

                                break;

                            default:
                                return noData;
                        }
                    }
                    else
                        return noData;
                }
            }
            catch (Exception)
            {
                return noData;
            }

            return result;
        }

        public static string TestNameInDataGridColumn(string test)
        {
            return (test == "StaticLoses") ? "SL" : test;
        }

        public enum XMLValues
        {
            //не определённое значение описания в формате XML
            UnAssigned = 0x00,

            //описание в XML формате условий измерений
            Conditions = 0x01,

            //описание в XML формате измеренных значений параметров
            Parameters = 0x02,

            //описание в XML формате параметров созданных вручную
            ManuallyParameters = 0x03,

            //описание в XML формате комментариев к изделию
            DeviceComments = 0x04
        }

        public static XMLValues SubjectByPath(string path)
        {
            //вычисление по приниятому path того с чем мы имеем дело: conditions или parameters
            //если по приниятому path не удаётся установить принадлежность к conditions или parameters - реализация возвращает UnAssigned
            return path.Contains(XMLValues.Conditions.ToString()) ? XMLValues.Conditions :
                    path.Contains(XMLValues.ManuallyParameters.ToString()) ? XMLValues.ManuallyParameters :
                     path.Contains(XMLValues.Parameters.ToString()) ? XMLValues.Parameters : XMLValues.UnAssigned;
        }

        public static string ExtractXMLTestFromPath(string path)
        {
            //извлекает имя теста из принятого path
            //пример: RTSLConditions©SL_ITM
            string result = path;

            switch (SubjectByPath(path))
            {
                case XMLValues.Conditions:
                    result = result.Remove(result.IndexOf(XMLValues.Conditions.ToString()));
                    break;

                case XMLValues.Parameters:
                    result = result.Remove(result.IndexOf(XMLValues.Parameters.ToString()));
                    break;
            }

            //первые два символа в принятом path содержат описание температурного режима
            result = result.Substring(2);

            return result;
        }

        public static string ExtractXMLNameFromPath(string path)
        {
            //извлекает имя Condition/Parameter/ManuallyParameter из принятого path
            //извлекаемое имя начинается сразу за разделителем и до конца принятого path
            string result = null;

            int index = path.IndexOf(Constants.FromXMLNameSeparator);

            if (index != -1)
            {
                result = path.Substring(index + 1);

                //избавляемся от числа в конце (это номер использования)
                result = RemoveEndingNumber(result);
            }

            return result;
        }

        public static string RemoveEndingNumber(string value)
        {
            //удаляет из принятого value конечное число
            char[] digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            return value.TrimEnd(digits);
        }

        public static string TemperatureDescription(bool isModeRT)
        {
            string result = isModeRT ? string.Concat("<=", Constants.MaxTemperatureRT.ToString()) : string.Concat(">", Constants.MaxTemperatureRT.ToString());

            return result;
        }

        /*
        public static string GetTemperatureSQLCondition(int conditionIDTemperature, bool isModeRT)
        {
            //чтение части SQL запроса для ограничения множества conditions/parameters по температурному режиму
            //не требуется использования данной реализации при построении множества значений простых реквизитов (не conditions/parameters)
            // conditionIDTemperature - идентификатор условия тмпературы в БД
            // isModeRT - входной параметр, определяющий температурный режим: true - RT, false - TM 
            return string.Format(@"LEFT JOIN PROF_COND PCT WITH(NOLOCK) ON (P.PROF_ID=PCT.PROF_ID) AND
                                                                           (PCT.COND_ID={0}) AND
                                                                           (PCT.VALUE{1})", conditionIDTemperature, TemperatureDescription(isModeRT));
        }
        */

        public static string GetSQLCondition(string partName, int conditionIDTemperature, bool isModeRT, int testTypeID, int condID)
        {
            //обернуть вызовом string.Format()
            //входной параметр isLimitedByTemperatureMode - есть ограничение по температурному режиму:
            //partName нужен для возможности различать в предложении HAVING случаи использования фильтров по разным параметрам;
            //{0} - идентификатор теста;
            //{1}- идентификатор условия

            return string.Format(@"LEFT JOIN PROF_COND PCTC{0} WITH(NOLOCK) ON (P.PROF_ID=PCTC{0}.PROF_ID) AND
                                                                               (PCTC{0}.COND_ID={1}) AND
                                                                               (PCTC{0}.VALUE{2})", partName, conditionIDTemperature, TemperatureDescription(isModeRT)) + "\r\n" +
                   string.Format(@"LEFT JOIN PROF_TEST_TYPE PTT{0} WITH(NOLOCK) ON (PCTC{0}.PROF_ID=PTT{0}.PROF_ID) AND
                                                                                   (PTT{0}.TEST_TYPE_ID={1})
                                   LEFT JOIN PROF_COND PC{0} WITH(NOLOCK) ON (PTT{0}.PTT_ID=PC{0}.PROF_TESTTYPE_ID) AND
                                                                             (PC{0}.COND_ID={2})", partName, testTypeID, condID);
        }

        public static string GetSQLParameter(string partName, int conditionIDTemperature, bool isModeRT, int testTypeID, int paramID)
        {
            //обернуть вызовом string.Format()
            //partName нужен для возможности различать в предложении HAVING случаи использования фильтров по разным параметрам;
            //{0} - идентификатор теста;
            //{1}- идентификатор параметра

            return string.Format(@"LEFT JOIN PROF_COND PCTP{0} WITH(NOLOCK) ON (P.PROF_ID=PCTP{0}.PROF_ID) AND
                                                                               (PCTP{0}.COND_ID={1}) AND
                                                                               (PCTP{0}.VALUE{2})", partName, conditionIDTemperature, TemperatureDescription(isModeRT)) + "\r\n" +
                   string.Format(@"LEFT JOIN PROF_TEST_TYPE PTT{0} WITH(NOLOCK) ON (PCTP{0}.PROF_ID=PTT{0}.PROF_ID) AND
                                                                                   (PTT{0}.TEST_TYPE_ID={1})
                                   LEFT JOIN DEV_PARAM DP{0} WITH(NOLOCK) ON (PTT{0}.PTT_ID=DP{0}.TEST_TYPE_ID) AND
                                                                             (D.DEV_ID=DP{0}.DEV_ID) AND
                                                                             (DP{0}.PARAM_ID={2})", partName, testTypeID, paramID);
        }

        public static string GetJoinSectionForSort(SCME.Common.Routines.XMLValues subject, string columnName, string partName, int? conditionIDTemperature, bool? isModeRT, int? testTypeID, int? conditionOrParameterID)
        {
            //возвращает текст части JOIN SQL запроса для вызова хранимой процедуры dbViewerGetData
            //данная реализация используется только для сортировки данных
            string result = null;

            switch (subject)
            {
                case Common.Routines.XMLValues.UnAssigned:
                    //имеем дело с постоянными данными - они есть в любой записи и хранятся в таблице Devices
                    switch (columnName)
                    {
                        case Common.Constants.AssemblyProtocolDescr:
                            result = string.Format("LEFT JOIN ASSEMBLYPROTOCOLS AP{0} WITH(NOLOCK) ON (D.ASSEMBLYPROTOCOLID=AP{0}.ASSEMBLYPROTOCOLID)", partName);
                            break;

                        case Common.Constants.GroupName:
                            result = string.Format("LEFT JOIN GROUPS G{0} WITH(NOLOCK) ON (D.GROUP_ID=G{0}.GROUP_ID)", partName);
                            break;
                    }
                    break;

                case SCME.Common.Routines.XMLValues.Conditions:
                    result = GetSQLCondition(partName, (int)conditionIDTemperature, (bool)isModeRT, (int)testTypeID, (int)conditionOrParameterID);
                    break;

                case SCME.Common.Routines.XMLValues.Parameters:
                    result = GetSQLParameter(partName, (int)conditionIDTemperature, (bool)isModeRT, (int)testTypeID, (int)conditionOrParameterID);
                    break;
            }

            return result;
        }

        #region WaitVisualizer System
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, Int32 wParam, Int32 lParam);

        public static IntPtr StartProcessWaitVisualizer()
        {
            //запуск процесса визуализации ожидания
            //возвращает десктриптор окна визуализации
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            System.Diagnostics.Process process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = string.Concat(AppDomain.CurrentDomain.BaseDirectory, "SCME.WaitVisualizer.exe"),
                    Arguments = currentProcess.Id.ToString()
                }
            };

            process.Start();

            //дожидаемся пока процесс создаст своё главное окно
            while (process.MainWindowHandle == IntPtr.Zero)
                process.WaitForInputIdle();

            IntPtr result = process.MainWindowHandle;

            //прячем окно визуализации ожидания
            SendMessage(result, Constants.WM_HIDE, 0, 0);

            return result;
        }

        public static void HideProcessWaitVisualizer(IntPtr waitVisualizerWindowHandle)
        {
            //прячет окно визуализации простого процесса ожидания
            SendMessage(waitVisualizerWindowHandle, Constants.WM_HIDE, 0, 0);
        }

        public static void HideProcessWaitVisualizerSortingFiltering(IntPtr waitVisualizerWindowHandle)
        {
            //прячет окно визуализации процесса ожидания сортировки/фильтрации
            SendMessage(waitVisualizerWindowHandle, Constants.WM_HIDESortingFiltering, 0, 0);
        }

        private static void ShowProcessWaitVisualizer(System.Windows.Window parentWindow, IntPtr waitVisualizerWindowHandle, bool visualizationSortingFiltering)
        {
            //показывает окно визуализации
            //входной параметр visualizationSortingFiltering:
            // true  - визуализация процесса ожидания сортировки/фильтрации
            // false - визуализация процесса ожидания простой длительной операции (отчёт в Excel)

            //формируем данные о положении родительского окна
            double wDParam = parentWindow.ActualWidth / 2;
            double lDParam = parentWindow.ActualHeight / 2;

            int wParam;
            int lParam;

            if (parentWindow.WindowState == System.Windows.WindowState.Maximized)
            {
                wParam = Convert.ToInt32(Math.Ceiling(wDParam));
                lParam = Convert.ToInt32(Math.Ceiling(lDParam));
            }
            else
            {
                wParam = Convert.ToInt32(Math.Ceiling(parentWindow.Left + wDParam));
                lParam = Convert.ToInt32(Math.Ceiling(parentWindow.Top + lDParam));
            }

            switch (visualizationSortingFiltering)
            {
                case true:
                    //визуализация процесса ожидания сортировки/фильтрации
                    SendMessage(waitVisualizerWindowHandle, Constants.WM_SHOWSortingFiltering, wParam, lParam);
                    break;

                default:
                    //визуализация процесса ожидания простых длительных операций, не являющимися ни сортировкой ни фильтрацией
                    SendMessage(waitVisualizerWindowHandle, Constants.WM_SHOW, wParam, lParam);
                    break;
            }
        }

        public static void ShowProcessWaitVisualizerSortingFiltering(System.Windows.Window parentWindow, IntPtr waitVisualizerWindowHandle)
        {
            ShowProcessWaitVisualizer(parentWindow, waitVisualizerWindowHandle, true);
        }

        public static void ShowProcessWaitVisualizer(System.Windows.Window parentWindow, IntPtr waitVisualizerWindowHandle)
        {
            //визуализация простых длительных операций
            ShowProcessWaitVisualizer(parentWindow, waitVisualizerWindowHandle, false);
        }

        public static void KillProcessWaitVisualizer(IntPtr waitVisualizerWindowHandle)
        {
            //закрывает окно визуализации, что приводит к уничтожению процесса вызуализации
            SendMessage(waitVisualizerWindowHandle, Constants.WM_CLOSE, 0, 0);
        }
        #endregion

        #region Permissions bit operations
        public static bool CheckBit(ulong value, byte bitNumber)
        {
            //проверка состояния бита с номером bitNumber в value
            ulong bitMask = (ulong)(1 << bitNumber);

            return (value & bitMask) != 0;
        }

        public static ulong SetBit(ulong value, byte bitNumber)
        {
            //установка бита с номером bitNumber в value
            ulong bitMask = (ulong)(1 << bitNumber);

            return (value | bitMask);
        }

        public static ulong DropBit(ulong value, byte bitNumber)
        {
            //сброс бита с номером bitNumber в value
            ulong bitMask = ~(ulong)(1 << bitNumber);

            return (value & bitMask);
        }

        public static bool IsUserAdmin(ulong permissionsLo)
        {
            //отвечает на вопрос является ли обладатель битовой маски permissionsLo правами администратора в данной системе. за это отвечает бит Constants.cIsUserAdmin
            return CheckBit(permissionsLo, Constants.cIsUserAdmin);
        }

        public static bool IsUserCanReadCreateComments(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo читать/создавать комментарии. за это отвечает бит Constants.cIsUserCanReadCreateComments
            return CheckBit(permissionsLo, Constants.cIsUserCanReadCreateComments);
        }

        public static bool IsUserCanReadComments(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo читать комментарии. за это отвечает бит Constants.cIsUserCanReadComments
            return CheckBit(permissionsLo, Constants.cIsUserCanReadComments);
        }

        public static bool IsUserCanCreateParameter(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo создавать параметры в ManualInputParams
            return CheckBit(permissionsLo, Constants.cIsUserCanCreateParameter);
        }

        public static bool IsUserCanEditParameter(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo редактировать значения параметров созданных вручную в ManualInputParams
            return CheckBit(permissionsLo, Constants.cIsUserCanEditParameter);
        }

        public static bool IsUserCanDeleteParameter(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo удалять параметры созданные вручную из ManualInputParams
            return CheckBit(permissionsLo, Constants.cIsUserCanDeleteParameter);
        }

        public static bool IsUserCanCreateValueOfManuallyEnteredParameter(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo создавать места хранения значения параметра созданного вручную в ManualInputDevParam
            return CheckBit(permissionsLo, Constants.cIsUserCanCreateValueOfManuallyEnteredParameter);
        }

        public static bool IsUserCanEditValueOfManuallyEnteredParameter(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo редактировать значения параметров созданных вручную в ManualInputDevParam
            return CheckBit(permissionsLo, Constants.cIsUserCanEditValueOfManuallyEnteredParameter);
        }

        public static bool IsUserCanDeleteValueOfManuallyEnteredParameter(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo удалять значения параметров созданных вручную в ManualInputDevParam
            return CheckBit(permissionsLo, Constants.cIsUserCanDeleteValueOfManuallyEnteredParameter);
        }

        public static bool IsUserCanCreateDevices(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo создавать изделия (ручная регистрация изделий)
            return CheckBit(permissionsLo, Constants.cIsUserCanCreateDevices);
        }

        public static bool IsUserCanWorkWithAssemblyProtocol(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo работать с протоколом сборки
            return CheckBit(permissionsLo, Constants.cWorkWithAssemblyProtocol);
        }

        public static bool IsUserCanManageDeviceReferences(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo работать со справочником норм на изделия 
            return CheckBit(permissionsLo, Constants.cIsUserCanManageDeviceReferences);
        }

        public static bool IsUserCanReadReason(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo читать поле 'DEVICE.REASON'. за это отвечает бит Constants.cIsUserCanReadReason
            return CheckBit(permissionsLo, Constants.cIsUserCanReadReason);
        }

        public static bool IsUserCanEditAssembly(ulong permissionsLo)
        {
            //отвечает на вопрос может ли обладатель битовой маски permissionsLo редактировать сборку
            return CheckBit(permissionsLo, Constants.cEditAssembly);
        }
        #endregion

        public static bool TextIsNumeric(string text)
        {
            return text.All(c => Char.IsDigit(c) || Char.IsControl(c));
        }

        public static string SimpleFloatingValueToFloatingValue(string simpleFloatingValue)
        {
            //если число simpleFloatingValue записано в виде ,25 или .25 возвратим его в полноценном виде с корректным системным разделителем дробной части от целой части
            if (simpleFloatingValue.StartsWith(SystemDecimalSeparator().ToString()))
            {
                string result = string.Concat("0", simpleFloatingValue);

                return result;
            }
            else
                return simpleFloatingValue;
        }

        public static void TextBoxOnlyNumeric_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            //запрещаем ввод не цифр, но пробел этот обработчик даст ввести
            e.Handled = !TextIsNumeric(e.Text);
        }

        public static void TextBoxOnlyNumericPaste(object sender, System.Windows.DataObjectPastingEventArgs e)
        {
            //запрещаем вставку из буфера обмена не цифр
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));

                if (!TextIsNumeric(text))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        public static void TextBoxDisableSpace_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //запрещаем использование пробела в поле ввода значения класса
            if (e.Key == System.Windows.Input.Key.Space)
                e.Handled = true;
        }

        public static void TextBoxOnlyDouble_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            //на пробел данное событие не реагирует - поэтому пользователь может ввести пробелы
            if (e.OriginalSource is TextBox tb)
            {
                string selectedText = tb.SelectedText;
                string tbText;

                if (string.IsNullOrEmpty(selectedText))
                {
                    tbText = tb.Text;
                }
                else
                {
                    int startSelectedIndex = tb.Text.IndexOf(selectedText);
                    tbText = tb.Text.Remove(startSelectedIndex, selectedText.Length);
                }

                string allEntered = tbText.Insert(tb.CaretIndex, e.Text);
                e.Handled = !Common.Routines.IsDouble(allEntered);
            }
        }

        public static void TextBoxOnlyDoublePaste(object sender, System.Windows.DataObjectPastingEventArgs e)
        {
            //значение параметра может быть только типа double
            if (!e.SourceDataObject.GetDataPresent(System.Windows.DataFormats.UnicodeText, true))
                return;

            bool parced = IsDouble(e.SourceDataObject.GetData(System.Windows.DataFormats.UnicodeText).ToString());

            if (!parced)
                e.CancelCommand();
        }
    }
}
