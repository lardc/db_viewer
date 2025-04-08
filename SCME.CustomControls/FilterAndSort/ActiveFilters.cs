using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCME.CustomControls
{
    public class FilterValue
    {
        public FilterValues Owner { get; } = null;

        public FilterValue(FilterValues owner)
        {
            this.Owner = owner;
        }

        public object Value { get; set; }

        public FilterDescription FilterDescription
        {
            //выход на уровень описания фильтра
            //надо для работы механизма binding чтобы показывать список значений для выбора в comboBox
            get
            {
                return this.Owner.Owner;
            }
        }
    }

    public class FilterValues : ObservableCollection<FilterValue>
    {
        public FilterDescription Owner { get; } = null;

        public FilterValues(FilterDescription owner)
        {
            this.Owner = owner;
        }

        public FilterValue NewFilterValue()
        {
            FilterValue result = new FilterValue(this);
            this.Add(result);

            return result;
        }

        public IEnumerable<string> Values()
        {
            //возвращает список значений которые пользователь ввёл для отбора
            IEnumerable<string> result = this.Select(x => x.Value?.ToString());

            return result;
        }

        public void Correct()
        {
            foreach (FilterValue filterValue in this)
            {
                if (filterValue.Value != null)
                {
                    string sValue = filterValue.Value.ToString();

                    //выбрасываем все встречающиеся пробелы
                    sValue = Regex.Replace(sValue, @"\s+", "");

                    if (sValue == string.Empty)
                        filterValue.Value = null;
                }

                if (filterValue.Owner.Owner.Type.ToString() == "System.Boolean")
                {
                    //для пользователя хранящиеся в базе данных значения типа bool равные null или false равнозначны
                    if (filterValue.Value != null)
                    {
                        if (!(bool)filterValue.Value)
                            filterValue.Value = null;
                    }
                }
            }
        }
    }

    public class ListOfValues : List<string>
    {
        public ListOfValues(string fieldName)
        {
            //грузим множество допустимых значений
            Types.DbRoutines.LoadListOfValues(this, fieldName);
        }
    }

    public class FilterDescription
    {
        public ActiveFilters Owner { get; set; } = null;

        //properties для возможности работы механизма binding в ListView, binding не будет работать по простым полям
        public string TittlefieldName { get; set; }
        public string Comparison { get; set; }

        //множество значений фильтра (пользователь формирует список значений по которому система выполняет фильтрацию записей)
        public FilterValues Values { get; set; }

        public object Value
        {
            get
            {
                //сравнение '=' предполагает список возможных значений для выбранного поля фильтра (нет места хранения одного значения)
                //все остальные варианты сравнения предполагают использование только одного значения фильтра установленного пользователем (нет списка значений)
                return (this.Values.Count == 1) ? this.Values[0].Value : null;
            }

            set
            {
                //если выполняется установка значения фильтра (всегда только одно значение, ни какого списка в этом случае быть не может) то возможны следующие случаи:
                //место хранения одного единственного значения не существует (this.Values.Count=0);
                //место хранения одного единственного значения существует - пользователь редактирует значение фильтра

                FilterValue filterValue = (this.Values.Count == 0) ? this.Values.NewFilterValue() : (this.Values.Count == 1) ? this.Values[0] : null;

                if (filterValue != null)
                    filterValue.Value = value;
            }
        }

        public string StringValueCorrected { get; set; }

        //список возможных значений из которых пользователь выбирает нужное ему (только ComboBox)
        public List<string> ListOfValues { get; }

        public string Type;
        public string FieldName { get; }
        public string ComparisonCorrected;

        //флаг об актуальности данного фильтра, т.е. данный фильтр надо показывать пользователю и использовать для превращения в текст SQL запроса
        //нужен для удаления не подтверждённых пользователем фильтров
        //по умолчанию фильтр создаётся не нужным для использования, нужным он может стать если пользователь нажмёт кнопку 'OK' для применения фильтров или для формирования фильтров в форме SCME.CustomControls.FiltersInput
        bool FAcceptedToUse = false;
        public bool AcceptedToUse
        {
            get
            {
                return this.FAcceptedToUse;
            }

            set
            {
                this.FAcceptedToUse = value;

                if (value)
                {
                    Owner.Accept(this);
                }
            }
        }

        public FilterDescription(ActiveFilters owner, string fieldName)
        {
            //запоминаем ссылку на коллекцию, в которой будет жить данный фильтр
            this.Owner = owner;

            this.FieldName = fieldName;

            //создаём место хранения списка значений для выбора одного из них с помощью ComboBox
            //список может быть пустым - поле фильтра не предполагает использование списка значений для выбора пользователем
            this.ListOfValues = new ListOfValues(fieldName);

            //создаём место хранения списка значений для данного фильтра - пользователь формирует этот списка чтобы получить такие записи, которые имеют значения поля только из данного списка
            this.Values = new FilterValues(this);
        }

        public void SetFilterValue(object value)
        {
            //создание места хранения для фильтра
            switch (this.Values.Count)
            {
                case 0:
                    this.Values.NewFilterValue().Value = value;
                    break;

                default:
                    this.Values[0].Value = value;
                    break;
            }
        }

        public void Correct()
        {
            //корректировка описания фильтра
            //subject использует dbViewer
            if (this.Value == null)
            {
                this.Comparison = "=";
                this.ComparisonCorrected = " IS ";
            }
            else
            {
                //по значению фильтра нельзя решать как выполнять сравнение значения - для этого надо смотреть на его Type
                bool isNumeric = (this.Type == "System.Double") || (this.Type == "System.Int32");

                if (isNumeric)
                {
                    if (this.Type == "System.Double")
                    {
                        Common.Routines.TryStringToDouble(this.Value.ToString(), out double d);
                        this.Value = d.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                switch (isNumeric)
                {
                    case true:
                        this.ComparisonCorrected = this.Comparison;
                        break;

                    default:
                        switch (this.Type)
                        {
                            case "System.DateTime":
                            case "System.DateOnly":
                                //имеем дело с типом данных DateTime
                                this.ComparisonCorrected = this.Comparison;
                                this.StringValueCorrected = this.Value.ToString();
                                break;

                            case "System.String":
                                if (this.Values.Count == 1)
                                {
                                    this.StringValueCorrected = this.Values[0].Value.ToString().Replace("*", "%");
                                    bool valueIsMask = (this.Values[0].Value.ToString() != this.StringValueCorrected);

                                    switch (valueIsMask)
                                    {
                                        case true:
                                            //имеем дело с маской для поиска
                                            this.Comparison = "=";
                                            this.ComparisonCorrected = " LIKE ";
                                            break;

                                        default:
                                            //строка не является маской
                                            this.ComparisonCorrected = this.Comparison; // "=";
                                            break;
                                    }
                                }
                                break;

                        }
                        break;
                }
            }
        }

        public string WhereSection()
        {
            string result = null;

            if (this.Value == null)
                result = string.Format("{0}{1}{2}", this.FieldName, this.ComparisonCorrected, this.Value);
            else
            {
                switch (this.Type.ToString())
                {
                    case "System.String":
                        result = string.Format("{0}{1}'{2}'", this.FieldName, this.ComparisonCorrected, this.StringValueCorrected);
                        break;

                    case "System.DateTime":
                        //извлекаем значение даты из DataTime - отбрасываем значение времени
                        if (DateTime.TryParse(this.Value.ToString(), out DateTime dtValue))
                            //выполняем сравнение DateTime без времени - только даты
                            result = string.Format("Convert(Substring(Convert({0}, 'System.String'), 1, 10), 'System.DateTime'){1}'{2}'", this.FieldName, this.Comparison, dtValue);

                        break;

                    case "System.Int32":
                        if (int.TryParse(this.Value.ToString(), out int iValue))
                            result = string.Format("{0}{1}{2}", this.FieldName, this.Comparison, iValue);

                        break;

                    case "System.Double":
                        if (SCME.Common.Routines.TryStringToDouble(this.Value.ToString(), out double dValue))
                            result += string.Format("{0}{1}{2}", this.FieldName, this.Comparison, dValue.ToString().Replace(',', '.'));

                        break;

                    case "System.Boolean":
                        if (bool.TryParse(this.Value.ToString(), out bool bValue))
                            result += string.Format("{0}{1}{2}", this.FieldName, this.Comparison, bValue ? 1 : 0);

                        break;
                }
            }

            return result;
        }

        public string TemperatureMode()
        {
            //чтение температурного режима из имени поля по которому работает данный фильтр (только для случаев conditions/parameters)
            return this.FieldName.Substring(0, 2).ToUpper();
        }
    }

    public class ActiveFilters : ObservableCollection<FilterDescription>
    {
        //механизм обратного вызова для выполнения действий которые необходимо выполнить при изменении списка фильтров
        //в activeFilters передаётся список активных фильтров, которые существуют уже после произошедшего изменения списка фильтров
        public delegate void OnChangedListOfFilters(IEnumerable<FilterDescription> activeFilters);
        public OnChangedListOfFilters OnChangedListOfFiltersHandler { get; set; }

        public FilterDescription FindFilterByFieldNameAndComparisonEquality(string fieldName, FilterDescription excludeFilterDescription)
        {
            //ищет в списке фильтров с типом сравнения '=' фильтр по полю fieldName, не являющийся фильтром excludeFilterDescription
            //такого фильтра либо не будет найдено, либо он будет найден в единственном числе
            FilterDescription result = this.Where(f => f.FieldName == fieldName && f.Comparison == "=" && f.AcceptedToUse && f != excludeFilterDescription).SingleOrDefault();

            return result;
        }

        public void Accept(FilterDescription filterDescription)
        {
            //для принятого на вход фильтра filterDescription выполняется установка filterDescription.AcceptedToUse=true - пользователь подтверждает фильтр filterDescription
            //если данный фильтр с Comparison="=" - проверяем нет ли фильтра по тому же FieldName с Comparison="=" и если таковой есть - то:
            // 1. добавляем в Values найденного фильтра значение filterDescription.Value;
            // 2. уничтожаем фильтр filterDescription
            if (filterDescription.AcceptedToUse && (Common.Routines.ProcessName() == "SCME.dbViewer"))
            {
                if (filterDescription.Comparison == "=")
                {
                    //только dbViewer умеет выполнять фильтрацию по множеству значений фильтруемого поля - Linker так не умеет
                    FilterDescription oldFilterDescription = this.FindFilterByFieldNameAndComparisonEquality(filterDescription.FieldName, filterDescription);

                    if (oldFilterDescription != null)
                    {
                        oldFilterDescription.Values.NewFilterValue().Value = filterDescription.Value;
                        this.Remove(filterDescription);
                    }
                }

                //filterDescription может быть отредактирован пользователем, поэтому проверяем возможность отображения данных в режиме 'Протокол сборки'
                this.ExecOnChangedListOfFilters();
            }
        }

        public new void Add(FilterDescription filterDescription)
        {
            base.Add(filterDescription);

            this.ExecOnChangedListOfFilters();
        }

        public new void Clear()
        {
            //удаляются все фильтры
            base.Clear();

            this.ExecOnChangedListOfFilters();

            /*
            //формируем в deletedFilters список имён полей удаляемых фильтров
            //тип List<string> для deletedFilters выбран не просто так - будет сделана копия сформированного списка имён, после выполнения очистки this эта копия никак не изменится
            //если использовать IEnumerable<string> вместо List<string>, то сформированный список будет пустым сразу после выполнения base.Clear(); 
            List<string> activeFilters = this.Where(f => f.AcceptedToUse).Select(f => f.FieldName).ToList();

            base.Clear();

            if (deletedFilters.Count() != 0)
                this.OnDeleteFilterHandler?.Invoke(deletedFilters);
            */
        }

        public new bool Remove(FilterDescription item)
        {
            //удаляется один фильтр
            bool result = base.Remove(item);

            if (item.AcceptedToUse)
            {
                this.ExecOnChangedListOfFilters();

                /*
                //формируем в deletedFilters имя поля этого фильтра
                IEnumerable<string> deletedFilters = new string[] { item.FieldName };

                //вызываем OnDeleteFilterHandler только если среди оставшихся фильтров не осталось фильтров по тому же FieldName, что и удалённого фильтра - удаляется последний фильтр по полю FieldName
                IEnumerable<FilterDescription> otherFilters = this.Where(f => f.AcceptedToUse && (f.FieldName == item.FieldName));

                if (otherFilters.Count() == 0)
                    this.OnDeleteFilterHandler?.Invoke(deletedFilters);
                */
            }

            return result;
        }

        private void ExecOnChangedListOfFilters()
        {
            if (this.OnChangedListOfFiltersHandler != null)
            {
                IEnumerable<FilterDescription> activeFilters = this.Where(f => f.AcceptedToUse);
                this.OnChangedListOfFiltersHandler(activeFilters);
            }
        }

        public new void RemoveAt(int index)
        {
            //удаляется один фильтр           
            string fieldName = this.Items[index].FieldName;
            bool acceptedToUse = this.Items[index].AcceptedToUse;

            base.RemoveAt(index);

            if (acceptedToUse)
            {
                this.ExecOnChangedListOfFilters();

                /*
                //формируем в deletedFilters имя поля этого фильтра
                IEnumerable<string> deletedFilters = new string[]
                {
                   fieldName
                };

                //вызываем OnDeleteFilterHandler только если среди оставшихся фильтров не осталось фильтров по тому же FieldName, что и удалённого фильтра
                var otherFilters = this.Where(f => f.AcceptedToUse && (f.FieldName == fieldName));
                if (otherFilters.Count() == 0)
                    this.OnDeleteFilterHandler?.Invoke(deletedFilters);
                */
            }
        }

        public int FirstIndexByFieldName(int index, string fieldName)
        {
            //вычисляет индекс первого найденного фильтра у которого FieldName равно принятому fieldName
            //поиск начинается с нулевого индекса до принятого индекса = index-1 включительно
            //если искомый фильтр не найден - возвращает -1;
            //если искомый фильтр найден - возвращает положительное значение индекса
            var filter = this.Where(f => (IndexOf(f) < index) && (f.FieldName == fieldName)).SingleOrDefault();

            return (filter == null) ? -1 : this.IndexOf(filter);
        }

        public void DeleteEmptyFilters()
        {
            //удаляет фильтры, значения которых не определены - равны null
            this.Where(l => l.Value == null).ToList().All(i => this.Remove(i));
        }

        public void Correct()
        {
            foreach (FilterDescription f in this.Items)
            {
                f.Values.Correct();
                f.Correct();
            }
        }

        public string WhereSection(bool isCalledBydbViewer, out string joinSection, out string havingSection)
        {
            //получение whereSection, joinSection и havingSection частей SQL запроса сформированного по всему множеству хранящихся в this фильтров
            //данную реализацию вызывают две системы: Linker и dbViewer
            //входной параметр subject=null для случая, если данная реализация вызвана Linker
            //subject нужен только для dbViewer
            string whereSection = null;
            joinSection = null;
            havingSection = null;

            if (this.Count > 0)
            {
                this.Correct();
                int condTemperatureID = -1;

                //если данную реализацию вызвал dbViewer - считываем идентификатор условия температуры
                if (isCalledBydbViewer)
                    condTemperatureID = Types.DbRoutines.ConditionIDByName(Common.Constants.Temperature);

                for (int index = 0; index < this.Count; index++)
                {
                    FilterDescription f = this[index];

                    if (isCalledBydbViewer)
                    {
                        /*
                        //обработка вызова от системы dbViewer                       
                        SCME.Common.Routines.XMLValues subject = SCME.Common.Routines.SubjectByPath(f.FieldName);

                        switch (subject)
                        {
                            case Common.Routines.XMLValues.UnAssigned:
                                //фильтр по простым реквизитам
                                if (havingSection != null)
                                    havingSection = string.Concat(havingSection, " AND ");

                                switch (f.FieldName)
                                {
                                    case Common.Constants.DeviceClass:
                                        string classValue = string.Format("dbo.DeviceClassForSort(STRING_AGG(SUBSTRING(D.PARTPROFILENAME, 3, 2), '{0}') WITHIN GROUP (ORDER BY D.DEV_ID), STRING_AGG(D.{1}, '{0}') WITHIN GROUP (ORDER BY D.DEV_ID), '{0}')", SCME.Common.Constants.cString_AggDelimeter, f.FieldName);
                                        havingSection = string.Concat(havingSection, string.Format("(dbo.NumericCompare({0}, '{1}', '{2}', '{3}') = 1)", classValue, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected, f.Value));
                                        break;

                                    case Common.Constants.Status:
                                        string statusValue = string.Format("dbo.DeviceStatusForSort(STRING_AGG(SUBSTRING(D.PARTPROFILENAME, 3, 2), '{0}') WITHIN GROUP (ORDER BY D.DEV_ID), STRING_AGG(D.{1}, '{0}') WITHIN GROUP (ORDER BY D.DEV_ID), '{0}')", SCME.Common.Constants.cString_AggDelimeter, f.FieldName);
                                        havingSection = string.Concat(havingSection, string.Format("(dbo.StringCompare({0}, '{1}', '{2}', '{3}') = 1)", statusValue, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected.Trim(), f.StringValueCorrected));
                                        break;

                                    case Common.Constants.Code:
                                        //данное поле использовано в выражении GROUP BY, используем его как есть
                                        string codeValue = string.Format("D.{0}", f.FieldName);
                                        havingSection = string.Concat(havingSection, string.Format("{0}{1}'{2}'", codeValue, f.ComparisonCorrected, f.StringValueCorrected));
                                        break;

                                    case "PROFILEBODY":
                                        //данное поле использовано в выражении GROUP BY, используем его как есть
                                        string profileBodyValue = string.Format("D.{0}", f.FieldName);
                                        havingSection = string.Concat(havingSection, string.Format("{0}{1}'{2}'", profileBodyValue, f.ComparisonCorrected, f.StringValueCorrected));
                                        break;

                                    case Common.Constants.SapDescr:
                                        //отображается строковое значение, но поиск выполняем только по идентификатору, сравнивать можно только на равенство
                                        bool? sapDescrBooleanValue = SCME.Types.DbRoutines.SapIDBySapDescr(f.StringValueCorrected);

                                        if (sapDescrBooleanValue != null)
                                        {
                                            string sapID = string.Format("STRING_AGG(D.SAPID, '{0}')", SCME.Common.Constants.cString_AggDelimeter);
                                            string sapIDValue = (bool)sapDescrBooleanValue ? "1" : "0";
                                            havingSection = string.Concat(havingSection, string.Format("(dbo.NumericCompare({0}, '{1}', '{2}', '{3}') = 1)", sapID, SCME.Common.Constants.cString_AggDelimeter, "=", sapIDValue));
                                        }
                                        break;

                                    case Common.Constants.AssemblyProtocolDescr:
                                        if (joinSection != null)
                                            joinSection = string.Concat(joinSection, "\r\n");

                                        joinSection = string.Concat(joinSection, "LEFT JOIN ASSEMBLYPROTOCOLS AP WITH(NOLOCK) ON (D.ASSEMBLYPROTOCOLID=AP.ASSEMBLYPROTOCOLID)");
                                        string assemblyProtocolDescrValue = string.Format("STRING_AGG(RTRIM(AP.DESCR), '{0}')", SCME.Common.Constants.cString_AggDelimeter);
                                        havingSection = string.Concat(havingSection, string.Format("(dbo.NumericCompare({0}, '{1}', '{2}', '{3}') = 1)", assemblyProtocolDescrValue, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected, f.Value));
                                        break;

                                    case Common.Constants.GroupName:
                                        if (joinSection != null)
                                            joinSection = string.Concat(joinSection, "\r\n");

                                        joinSection = string.Concat(joinSection, "LEFT JOIN GROUPS G WITH(NOLOCK) ON (D.GROUP_ID=G.GROUP_ID)");
                                        string group_nameValue = string.Format("STRING_AGG(RTRIM(G.{0}), '{1}')", f.FieldName, SCME.Common.Constants.cString_AggDelimeter);
                                        havingSection = string.Concat(havingSection, string.Format("(dbo.StringCompare({0}, '{1}', '{2}', '{3}') = 1)", group_nameValue, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected.Trim(), f.StringValueCorrected));
                                        break;

                                    case Common.Constants.Ts:
                                        //выполняем сравнение даты-времени
                                        string TSValue = string.Format("STRING_AGG(D.{0}, '{1}')", f.FieldName, SCME.Common.Constants.cString_AggDelimeter);
                                        havingSection = string.Concat(havingSection, string.Format("(dbo.DateTimeCompare({0}, '{1}', '{2}', '{3}') = 1)", TSValue, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected, f.StringValueCorrected));
                                        break;

                                    case Common.Constants.Choice:
                                    case Common.Constants.AverageCurrent:
                                        //выполняем сравнение чисел, а не строк
                                        string averageCurrentValue = string.Format("STRING_AGG(D.{0}, '{1}')", f.FieldName, SCME.Common.Constants.cString_AggDelimeter);
                                        havingSection = string.Concat(havingSection, string.Format("(dbo.NumericCompare({0}, '{1}', '{2}', '{3}') = 1)", averageCurrentValue, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected, f.Value));
                                        break;

                                    default:
                                        //сравнение строковых значений
                                        string value = string.Format("STRING_AGG(D.{0}, '{1}')", f.FieldName, SCME.Common.Constants.cString_AggDelimeter);
                                        havingSection = string.Concat(havingSection, string.Format("(dbo.StringCompare({0}, '{1}', '{2}', '{3}') = 1)", value, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected.Trim(), f.StringValueCorrected));
                                        break;
                                }

                                break;

                            case Common.Routines.XMLValues.Conditions:
                                //фильтр по условию профиля
                                //если фильтр с именем f.FieldName уже был использован для получения текста SQL запроса - вычислим его индекс чтобы не формировать лишние JOIN SQL запросы
                                indexSQL = this.FirstIndexByFieldName(index, f.FieldName);

                                if (indexSQL == -1)
                                {
                                    //текущий фильтр не был ранее превращён в текст части JOIN SQL запроса
                                    int condID = Types.DbRoutines.ConditionIDByName(SCME.Common.Routines.ExtractXMLNameFromPath(f.FieldName));

                                    if (condID != -1)
                                    {
                                        //узнаём идентификатор теста
                                        string testTypeName = SCME.Common.Routines.ExtractXMLTestFromPath(f.FieldName);
                                        int testTypeID = Types.DbRoutines.TestTypeIDByTestTypeName(testTypeName);

                                        if (testTypeID != -1)
                                        {
                                            if (joinSection != null)
                                                joinSection = string.Concat(joinSection, "\r\n");

                                            indexSQL = index;
                                            joinSection = string.Concat(joinSection, SCME.Common.Routines.GetSQLCondition(indexSQL.ToString(), condTemperatureID, (f.TemperatureMode() == "RT"), testTypeID, condID));
                                        }
                                    }
                                }

                                if (havingSection != null)
                                    havingSection = string.Concat(havingSection, " AND ");

                                havingSection = string.Concat(havingSection, string.Format("(dbo.NumericCompare(STRING_AGG(PC{0}.VALUE, '{1}'), '{1}', '{2}', '{3}') = 1)", indexSQL, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected, f.Value));

                                break;

                            case Common.Routines.XMLValues.Parameters:
                                //фильтр по параметру изделия
                                //если фильтр с именем f.FieldName уже был использован для получения текста SQL запроса - вычислим его индекс чтобы не формировать лишние JOIN SQL запросы
                                indexSQL = this.FirstIndexByFieldName(index, f.FieldName);

                                if (indexSQL == -1)
                                {
                                    //текущий фильтр не был ранее превращён в текст части JOIN SQL запроса
                                    int paramID = Types.DbRoutines.ParamIDByName(SCME.Common.Routines.ExtractXMLNameFromPath(f.FieldName));

                                    if (paramID != -1)
                                    {
                                        //узнаём идентификатор теста
                                        string testTypeName = SCME.Common.Routines.ExtractXMLTestFromPath(f.FieldName);
                                        int testTypeID = Types.DbRoutines.TestTypeIDByTestTypeName(testTypeName);

                                        if (testTypeID != -1)
                                        {
                                            if (joinSection != null)
                                                joinSection = string.Concat(joinSection, "\r\n");

                                            indexSQL = index;
                                            joinSection = string.Concat(joinSection, SCME.Common.Routines.GetSQLParameter(indexSQL.ToString(), condTemperatureID, (f.TemperatureMode() == "RT"), testTypeID, paramID));
                                        }
                                    }
                                }

                                if (havingSection != null)
                                    havingSection = string.Concat(havingSection, " AND ");

                                havingSection = string.Concat(havingSection, string.Format("(dbo.NumericCompare(STRING_AGG(DP{0}.VALUE, '{1}'), '{1}', '{2}', '{3}') = 1)", indexSQL, SCME.Common.Constants.cString_AggDelimeter, f.ComparisonCorrected, f.Value));

                                break;
                        }
                        */
                    }
                    else
                    {
                        //случай вызова от системы Linker
                        string forAddWhereSection = f.WhereSection();
                        if (forAddWhereSection != null)
                        {
                            if (whereSection != null)
                                whereSection = string.Concat(whereSection, " AND ");

                            whereSection = string.Concat(whereSection, forAddWhereSection);
                        }
                    }
                }
            }

            return whereSection;
        }
    }
}