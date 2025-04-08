using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Excel;

namespace SCME.dbViewer.ForParameters
{
    public class Parameter
    {
        public Temperature temperature { get; }
        public string tittleName { get; set; }
        public string dbName { get; set; }
        public string um { get; set; }
        public string value { get; set; }

        public Parameter()
        {
            this.temperature = new Temperature();
            this.tittleName = string.Empty;
            this.dbName = string.Empty;
            this.um = string.Empty;
            this.value = null;
        }
    }

    public enum TemperatureCondition
    {
        None = 0,
        RT = 1,
        TM = 2
    }

    public class Temperature
    {
        public TemperatureCondition condition { get; set; }
        public int value { get; set; }
        public string asString
        {
            get
            {
                switch (this.condition)
                {
                    case TemperatureCondition.RT:
                        return this.condition.ToString();

                    case TemperatureCondition.TM:
                        return string.Format("{0}°C", this.value.ToString());

                    default:
                        return string.Empty;
                }
            }
        }

        public Temperature()
        {
            this.condition = TemperatureCondition.None;
            this.value = 0;
        }
    }

    public class Parameters : ObservableCollection<Parameter>
    {
        public Parameters(int devID, string profileID, string groupName, string code, string silN1, string silN2, string equipment, string user) : base()
        {
            this._DevID = devID;
            this._ProfileID = profileID;
            this._GroupName = groupName;
            this._Code = code;
            this._SilN1 = silN1;
            this._SilN2 = silN2;
            this._equipment = equipment;
            this._user = user;
        }

        private int _DevID;
        private string _ProfileID;
        private string _GroupName;
        private string _Code;
        private string _SilN1;
        private string _SilN2;
        private string _equipment;
        private string _user;

        public int DevID { get { return _DevID; } }
        public string ProfileID { get { return _ProfileID; } }
        public string GroupName { get { return _GroupName; } }
        public string Code { get { return _Code; } }
        public string SilN1 { get { return _SilN1; } }
        public string SilN2 { get { return _SilN2; } }

        public ObservableCollection<Parameter> list
        {
            get
            {
                return this;
            }
        }

        public void Load(SqlConnection connection, int devID)
        {
            this.LoadConditions(connection, devID);
            this.LoadMeasuredParameters(connection, devID, );
        }

        private void LoadConditions(SqlConnection connection, int devID)
        {
            //загрузка значений conditions для принятого devID
            string SQLText = "SELECT C.COND_NAME, PC.VALUE" +
                             " FROM PROF_COND AS PC" +
                             "  INNER JOIN PROFILES AS PR ON PC.PROF_ID=PR.PROF_ID" +
                             "  INNER JOIN DEVICES AS D ON D.PROFILE_ID=PR.PROF_GUID AND" +
                             "                                          D.DEV_ID=@DevID" +
                             "  INNER JOIN CONDITIONS C ON (PC.COND_ID=C.COND_ID AND" +
                             "                              C.COND_NAME IN(@Conditions))" +
                             " ORDER BY PC.COND_ID";

            SqlCommand command = new SqlCommand(SQLText, connection);
            SqlParameter DevID = new SqlParameter("DevID", SqlDbType.Int);
            DevID.Value = devID;
            command.Parameters.Add(DevID);

            SqlParameter Conditions = new SqlParameter("Conditions", SqlDbType.NVarChar);
            Conditions.Value = "BVT_VD', 'BVT_VR";
            command.Parameters.Add(Conditions);

            if (connection.State != ConnectionState.Open)
                connection.Open();

            try
            {
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    //запоминаем имя условия
                    int index = reader.GetOrdinal("COND_NAME");
                    string conditionName = reader.GetValue(index).ToString();

                    //запоминаем значение условия
                    index = reader.GetOrdinal("VALUE");
                    string conditionValue = reader.GetValue(index).ToString();

                    //ед. измерения у условия нет
                    Parameter p = new Parameter { dbName = conditionName, value = conditionValue };
                    this.Add(p);
                }
            }
            finally
            {
                connection.Close();
            }

        }

        private void LoadMeasuredParameters(SqlConnection connection, int devID)
        {           
            //чтение значения данного параметра с его единицей измерения и температурой при которой было выполнено данное измерение
            string SQLText = "SELECT PC.VALUE AS TEMPERATURE, P.PARAM_NAME, P.PARAMUM, DP.VALUE" +
                             " FROM DEV_PARAM DP" +
                             "  INNER JOIN PARAMS AS P ON P.PARAM_ID=DP.PARAM_ID" +
                             "  INNER JOIN DEVICES AS D ON DP.DEV_ID=D.DEV_ID" +
                             "  INNER JOIN PROFILES AS PR ON D.PROFILE_ID=PR.PROF_GUID" +
                             "  LEFT JOIN PROF_COND AS PC ON PR.PROF_ID=PC.PROF_ID" +
                             "  INNER JOIN CONDITIONS C ON(PC.COND_ID=C.COND_ID AND" +
                             "                             C.COND_NAME='CLAMP_Temperature')" +
                             " WHERE DP.DEV_ID=@DevID" +
                             " ORDER BY DP.DEV_PARAM_ID";

            SqlCommand command = new SqlCommand(SQLText, connection);

            SqlParameter DevID = new SqlParameter("DevID", SqlDbType.Int);
            DevID.Value = devID;
            command.Parameters.Add(DevID);

            if (connection.State != ConnectionState.Open)
                connection.Open();

            try
            {
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    //запоминаем при какой температуре производилось измерение
                    int index = reader.GetOrdinal("TEMPERATURE");
                    var z = reader.GetValue(index);
                    int t = int.Parse(z.ToString());
                    TemperatureCondition tc = (t > 25) ? TemperatureCondition.TM : TemperatureCondition.RT;

                    //запоминаем наименование параметра
                    index = reader.GetOrdinal("PARAM_NAME");
                    z = reader.GetValue(index);
                    string paramName = z.ToString();

                    //запоминаем единицу измерения параметра
                    index = reader.GetOrdinal("PARAMUM");
                    z = reader.GetValue(index);
                    string u = z.ToString();

                    //запоминаем значение параметра
                    index = reader.GetOrdinal("VALUE");
                    z = reader.GetValue(index);
                    string v = z.ToString();

                    Parameter p = new Parameter { dbName = paramName, um = u, value = v };
                    p.temperature.value = t;
                    p.temperature.condition = tc;
                    this.Add(p);
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public Parameter Combine(Parameter param1, Parameter param2)
        {
            //объединение данных двух параметров для вывода в отчёт - предполагается, что в принятых param1 param2 value установлено только в одном из параметров, либо ни в одном из параметров
            Parameter Result = null;

            if ((param1 != null) && (param2 != null))
            {
                if ((param1.value == null) || (param2.value == null))
                {
                    Result = new Parameter();

                    Result.tittleName = param1.tittleName + string.Format("({0})", param2.tittleName);

                    if ((param1.value != null) || (param2.value != null))
                    {
                        //хотя бы в одном из параметров есть значение
                        if (param1.value == null)
                        {
                            Result.temperature.condition = param2.temperature.condition;
                            Result.temperature.value = param2.temperature.value;
                            Result.um = param2.um;
                            Result.value = param2.value;
                        }
                        else
                        {
                            Result.temperature.condition = param1.temperature.condition;
                            Result.temperature.value = param1.temperature.value;
                            Result.um = param1.um;
                            Result.value = param1.value;
                        }
                    }
                }
            }

            return Result;
        }

        private string RCColumnNumToA1ColumnNum(int columnNum)
        {
            //преобразование номера столбца ColumnNum из цифровой идентификации в строковую идентификацию
            int A1 = Convert.ToByte('A') - 1;  //номер "A" минус 1 (65 - 1 = 64)

            int AZ = Convert.ToByte('Z') - A1; //кол-во букв в англ. алфавите (90 - 64 = 26)
            int t, m;

            //номер колонки
            t = (int)(columnNum / AZ); //целая часть
            m = (columnNum % AZ);      //остаток

            if (m == 0)
                t--;

            char result = '\0';

            switch (t > 0)
            {
                case (true):
                    result = Convert.ToChar(A1 + t);
                    break;
            }

            switch (m)
            {
                case (0):
                    t = AZ;
                    break;

                default:
                    t = m;
                    break;
            }

            string Result = "";
            if (result != '\0')
                Result = Convert.ToString(result);

            return Result + Convert.ToString(Convert.ToChar(A1 + t));
        }

        public string xlRCtoA1(int rowNum, int columnNum)
        {
            //преобразование цифровой идентификации ячейки (по номеру столбца, номеру строки) в строковую идентификацию
            return "$" + RCColumnNumToA1ColumnNum(columnNum) + rowNum.ToString();
        }

        public Range range(Excel.Application exelApp, int rowNum, int columnNum)
        {
            switch (exelApp != null)
            {
                case true:
                    string addr = xlRCtoA1(rowNum, columnNum);

                    return exelApp.get_Range(addr, addr);

                default:
                    return null;
            }
        }

        public void SetBorders(Excel.Application exelApp, Excel.Worksheet sheet, int rowNumBeg, int rowNumEnd)
        {
            if ((exelApp != null) || (sheet != null))
            {
                const int xlContinuous = 1;
                const int xlAutomatic = -4105;
                const int xlThin = 2;
                const int xlMedium = -4138;

                //вычисляем адреса начала и конца выведенного блока данных по изделию
                string addrBeg = xlRCtoA1(rowNumBeg, 1);
                string addrEnd = xlRCtoA1(rowNumEnd, sheet.UsedRange.Columns.Count);

                Range range = exelApp.get_Range(addrBeg, addrEnd);
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlInsideVertical].LineStyle = xlContinuous;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlInsideVertical].ColorIndex = xlAutomatic;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlInsideVertical].Weight = xlThin;

                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlInsideHorizontal].LineStyle = xlContinuous;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlInsideHorizontal].ColorIndex = xlAutomatic;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlInsideHorizontal].Weight = xlThin;

                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeLeft].LineStyle = xlContinuous;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeLeft].ColorIndex = xlAutomatic;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeLeft].Weight = xlMedium;

                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeRight].LineStyle = xlContinuous;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeRight].ColorIndex = xlAutomatic;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeRight].Weight = xlMedium;

                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeTop].LineStyle = xlContinuous;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeTop].ColorIndex = xlAutomatic;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeTop].Weight = xlMedium;

                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeBottom].LineStyle = xlContinuous;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeBottom].ColorIndex = xlAutomatic;
                range.Borders[Microsoft.Office.Interop.Excel.XlBordersIndex.xlEdgeBottom].Weight = xlMedium;
            }
        }

        public void Paint(Excel.Application exelApp, Excel.Worksheet sheet, int rowNumBeg, int rowNumEnd)
        {
            if ((exelApp != null) || (sheet != null))
            {
                const int xlSolid = 1;
                const int xlAutomatic = -4105;
                const int xlThemeColorDark1 = 1;

                //вычисляем адреса начала и конца выведенного блока данных по изделию
                string addrBeg = xlRCtoA1(rowNumBeg, 7);
                string addrEnd = xlRCtoA1(rowNumEnd, sheet.UsedRange.Columns.Count);

                Range range = exelApp.get_Range(addrBeg, addrEnd);

                range.Interior.Pattern = xlSolid;
                range.Interior.PatternColorIndex = xlAutomatic;
                range.Interior.ThemeColor = xlThemeColorDark1;
                range.Interior.TintAndShade = -0.14996795556505;
                range.Interior.PatternTintAndShade = 0;
            }
        }
    }

    public class TParameters : Parameters
    //параметры прибора типа Т
    {
        public Parameter UDRM { get; }
        public Parameter URRM { get; }
        public Parameter UBO { get; }
        public Parameter UBR { get; }
        public Parameter UTM { get; }
        public Parameter ITM { get; }
        public Parameter dUDdt { get; }
        public Parameter IH { get; }
        public Parameter UGT { get; }
        public Parameter IGT { get; }
        public Parameter Rg { get; }
        public Parameter IDRM { get; }
        public Parameter IRRM { get; }
        public Parameter tq { get; }
        public Parameter Rac { get; }
        public Parameter Rca { get; }

        public TParameters() : base()
        {
            //0
            UDRM = new Parameter { tittleName = "UDRM", dbName = "VDRM" };
            this.list.Add(UDRM);

            //1
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //2
            UBO = new Parameter { tittleName = "UBO", dbName = "" };
            this.list.Add(UBO);

            //3
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //4
            UTM = new Parameter { tittleName = "UTM", dbName = "VTM" };
            this.list.Add(UTM);

            //5
            ITM = new Parameter { tittleName = "ITM", dbName = "" };
            this.list.Add(ITM);

            //6
            dUDdt = new Parameter { tittleName = "dUD/dt", dbName = "duD/dtcrit" };
            this.list.Add(dUDdt);

            //7
            IH = new Parameter { tittleName = "IH", dbName = "IH" };
            this.list.Add(IH);

            //8
            UGT = new Parameter { tittleName = "UGT", dbName = "VGT" };
            this.list.Add(UGT);

            //9
            IGT = new Parameter { tittleName = "IGT", dbName = "IGT" };
            this.list.Add(IGT);

            //10
            Rg = new Parameter { tittleName = "Rg", dbName = "RG" };
            this.list.Add(Rg);

            //11
            IDRM = new Parameter { tittleName = "IDRM", dbName = "IDRM" };
            this.list.Add(IDRM);

            //12
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //13
            tq = new Parameter { tittleName = "tq", dbName = "TQ" };
            this.list.Add(tq);

            //14
            Rac = new Parameter { tittleName = "Rac", dbName = "" };
            this.list.Add(Rac);

            //15
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (4):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (8):
                            offset++;
                            break;

                        case (14):
                            offset += 3;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 4) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }

    public class TLParameters : Parameters
    //параметры прибора типа ТЛ
    {
        public Parameter UDRM { get; }
        public Parameter URRM { get; }
        public Parameter UBO { get; }
        public Parameter UBR { get; }
        public Parameter UTM { get; }
        public Parameter ITM { get; }
        public Parameter dUDdt { get; }
        public Parameter IH { get; }
        public Parameter UGT { get; }
        public Parameter IGT { get; }
        public Parameter Rg { get; }
        public Parameter IDRM { get; }
        public Parameter IRRM { get; }
        public Parameter tq { get; }
        public Parameter PRSM { get; }
        public Parameter Rac { get; }
        public Parameter Rca { get; }

        public TLParameters() : base()
        {
            //0
            UDRM = new Parameter { tittleName = "UDRM", dbName = "VDRM" };
            this.list.Add(UDRM);

            //1
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //2
            UBO = new Parameter { tittleName = "UBO", dbName = "" };
            this.list.Add(UBO);

            //3
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //4
            UTM = new Parameter { tittleName = "UTM", dbName = "VTM" };
            this.list.Add(UTM);

            //5
            ITM = new Parameter { tittleName = "ITM", dbName = "" };
            this.list.Add(ITM);

            //6
            dUDdt = new Parameter { tittleName = "dUD/dt", dbName = "duD/dtcrit" };
            this.list.Add(dUDdt);

            //7
            IH = new Parameter { tittleName = "IH", dbName = "IH" };
            this.list.Add(IH);

            //8
            UGT = new Parameter { tittleName = "UGT", dbName = "VGT" };
            this.list.Add(UGT);

            //9
            IGT = new Parameter { tittleName = "IGT", dbName = "IGT" };
            this.list.Add(IGT);

            //10
            Rg = new Parameter { tittleName = "Rg", dbName = "RG" };
            this.list.Add(Rg);

            //11
            IDRM = new Parameter { tittleName = "IDRM", dbName = "IDRM" };
            this.list.Add(IDRM);

            //12
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //13
            tq = new Parameter { tittleName = "tq", dbName = "TQ" };
            this.list.Add(tq);

            //14
            PRSM = new Parameter { tittleName = "PRSM", dbName = "PRSM" };
            this.list.Add(PRSM);

            //15
            Rac = new Parameter { tittleName = "Rac", dbName = "" };
            this.list.Add(Rac);

            //16
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (4):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (8):
                            offset++;
                            break;

                        case (15):
                            offset += 2;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 4) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }

    public class TBParameters : Parameters
    //параметры прибора типа ТБ
    {
        public Parameter UDRM { get; }
        public Parameter URRM { get; }
        public Parameter UBO { get; }
        public Parameter UBR { get; }
        public Parameter UTM { get; }
        public Parameter ITM { get; }
        public Parameter dUDdt { get; }
        public Parameter IH { get; }
        public Parameter tgt { get; }
        public Parameter UGT { get; }
        public Parameter IGT { get; }
        public Parameter Rg { get; }
        public Parameter IDRM { get; }
        public Parameter IRRM { get; }
        public Parameter tq { get; }
        public Parameter trr { get; }
        public Parameter Qrr { get; }
        public Parameter Rac { get; }
        public Parameter Rca { get; }

        public TBParameters() : base()
        {
            //0
            UDRM = new Parameter { tittleName = "UDRM", dbName = "VDRM" };
            this.list.Add(UDRM);

            //1
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //2
            UBO = new Parameter { tittleName = "UBO", dbName = "" };
            this.list.Add(UBO);

            //3
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //4
            UTM = new Parameter { tittleName = "UTM", dbName = "VTM" };
            this.list.Add(UTM);

            //5
            ITM = new Parameter { tittleName = "ITM", dbName = "" };
            this.list.Add(ITM);

            //6
            dUDdt = new Parameter { tittleName = "dUD/dt", dbName = "duD/dtcrit" };
            this.list.Add(dUDdt);

            //7
            IH = new Parameter { tittleName = "IH", dbName = "IH" };
            this.list.Add(IH);

            //8
            tgt = new Parameter { tittleName = "tgt", dbName = "" };
            this.list.Add(tgt);

            //9
            UGT = new Parameter { tittleName = "UGT", dbName = "VGT" };
            this.list.Add(UGT);

            //10
            IGT = new Parameter { tittleName = "IGT", dbName = "IGT" };
            this.list.Add(IGT);

            //11
            Rg = new Parameter { tittleName = "Rg", dbName = "RG" };
            this.list.Add(Rg);

            //12
            IDRM = new Parameter { tittleName = "IDRM", dbName = "IDRM" };
            this.list.Add(IDRM);

            //13
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //14
            tq = new Parameter { tittleName = "tq", dbName = "TQ" };
            this.list.Add(tq);

            //15
            trr = new Parameter { tittleName = "trr", dbName = "TRR" };
            this.list.Add(trr);

            //16
            Qrr = new Parameter { tittleName = "Qrr", dbName = "QRR" };
            this.list.Add(Qrr);

            //17
            Rac = new Parameter { tittleName = "Rac", dbName = "" };
            this.list.Add(Rac);

            //18
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (4):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (15):
                            offset++;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 4) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }

    public class TBIParameters : Parameters
    //параметры прибора типа ТБИ
    {
        public Parameter UDRM { get; }
        public Parameter URRM { get; }
        public Parameter UBO { get; }
        public Parameter UBR { get; }
        public Parameter UTM { get; }
        public Parameter ITM { get; }
        public Parameter dUDdt { get; }
        public Parameter IH { get; }
        public Parameter tgt { get; }
        public Parameter UGT { get; }
        public Parameter IGT { get; }
        public Parameter Rg { get; }
        public Parameter IDRM { get; }
        public Parameter IRRM { get; }
        public Parameter tq { get; }
        public Parameter trr { get; }
        public Parameter Qrr { get; }
        public Parameter Rac { get; }
        public Parameter Rca { get; }

        public TBIParameters() : base()
        {
            //0
            UDRM = new Parameter { tittleName = "UDRM", dbName = "VDRM" };
            this.list.Add(UDRM);

            //1
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //2
            UBO = new Parameter { tittleName = "UBO", dbName = "" };
            this.list.Add(UBO);

            //3
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //4
            UTM = new Parameter { tittleName = "UTM", dbName = "VTM" };
            this.list.Add(UTM);

            //5
            ITM = new Parameter { tittleName = "ITM", dbName = "" };
            this.list.Add(ITM);

            //6
            dUDdt = new Parameter { tittleName = "dUD/dt", dbName = "duD/dtcrit" };
            this.list.Add(dUDdt);

            //7
            IH = new Parameter { tittleName = "IH", dbName = "IH" };
            this.list.Add(IH);

            //8
            tgt = new Parameter { tittleName = "tgt", dbName = "" };
            this.list.Add(tgt);

            //9
            UGT = new Parameter { tittleName = "UGT", dbName = "VGT" };
            this.list.Add(UGT);

            //10
            IGT = new Parameter { tittleName = "IGT", dbName = "IGT" };
            this.list.Add(UGT);

            //11
            Rg = new Parameter { tittleName = "Rg", dbName = "RG" };
            this.list.Add(Rg);

            //12
            IDRM = new Parameter { tittleName = "IDRM", dbName = "IDRM" };
            this.list.Add(IDRM);

            //13
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //14
            tq = new Parameter { tittleName = "tq", dbName = "TQ" };
            this.list.Add(tq);

            //15
            trr = new Parameter { tittleName = "trr", dbName = "TRR" };
            this.list.Add(trr);

            //16
            Qrr = new Parameter { tittleName = "Qrr", dbName = "QRR" };
            this.list.Add(Qrr);

            //17
            Rac = new Parameter { tittleName = "Rac", dbName = "" };
            this.list.Add(Rac);

            //18
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (4):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (15):
                            offset++;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 4) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }

    public class TBHParameters : Parameters
    //параметры прибора типа ТБЧ
    {
        public Parameter UDRM { get; }
        public Parameter URRM { get; }
        public Parameter UBO { get; }
        public Parameter UBR { get; }
        public Parameter UTM { get; }
        public Parameter ITM { get; }
        public Parameter dUDdt { get; }
        public Parameter IH { get; }
        public Parameter tgt { get; }
        public Parameter UGT { get; }
        public Parameter IGT { get; }
        public Parameter Rg { get; }
        public Parameter IDRM { get; }
        public Parameter IRRM { get; }
        public Parameter tq { get; }
        public Parameter trr { get; }
        public Parameter Qrr { get; }
        public Parameter Rac { get; }
        public Parameter Rca { get; }

        public TBHParameters() : base()
        {
            //0
            UDRM = new Parameter { tittleName = "UDRM", dbName = "VDRM" };
            this.list.Add(UDRM);

            //1
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //2
            UBO = new Parameter { tittleName = "UBO", dbName = "" };
            this.list.Add(UBO);

            //3
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //4
            UTM = new Parameter { tittleName = "UTM", dbName = "VTM" };
            this.list.Add(UTM);

            //5
            ITM = new Parameter { tittleName = "ITM", dbName = "" };
            this.list.Add(ITM);

            //6
            dUDdt = new Parameter { tittleName = "dUD/dt", dbName = "duD/dtcrit" };
            this.list.Add(dUDdt);

            //7
            IH = new Parameter { tittleName = "IH", dbName = "IH" };
            this.list.Add(IH);

            //8
            tgt = new Parameter { tittleName = "tgt", dbName = "" };
            this.list.Add(tgt);

            //9
            UGT = new Parameter { tittleName = "UGT", dbName = "VGT" };
            this.list.Add(UGT);

            //10
            IGT = new Parameter { tittleName = "IGT", dbName = "IGT" };
            this.list.Add(UGT);

            //11
            Rg = new Parameter { tittleName = "Rg", dbName = "RG" };
            this.list.Add(Rg);

            //12
            IDRM = new Parameter { tittleName = "IDRM", dbName = "IDRM" };
            this.list.Add(IDRM);

            //13
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //14
            tq = new Parameter { tittleName = "tq", dbName = "TQ" };
            this.list.Add(tq);

            //15
            trr = new Parameter { tittleName = "trr", dbName = "TRR" };
            this.list.Add(trr);

            //16
            Qrr = new Parameter { tittleName = "Qrr", dbName = "QRR" };
            this.list.Add(Qrr);

            //17
            Rac = new Parameter { tittleName = "Rac", dbName = "" };
            this.list.Add(Rac);

            //18
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (4):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (15):
                            offset++;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 4) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }

    public class DParameters : Parameters
    //параметры прибора типа Д
    {
        public Parameter URRM { get; }
        public Parameter UBR { get; }
        public Parameter UFM { get; }
        public Parameter IFM { get; }
        public Parameter IRRM { get; }
        public Parameter Rca { get; }

        public DParameters() : base()
        {
            //0
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //1
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //2
            UFM = new Parameter { tittleName = "UFM", dbName = "UFM" };
            this.list.Add(UFM);

            //3
            IFM = new Parameter { tittleName = "IFM", dbName = "" };
            this.list.Add(IFM);

            //4
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //5
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (0):
                        case (1):
                            offset++;
                            break;

                        case (2):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (4):
                            offset += 7;
                            break;

                        case (5):
                            offset += 5;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 2) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }

    public class DLParameters : Parameters
    //параметры прибора типа ДЛ
    {
        public Parameter URRM { get; }
        public Parameter UBR { get; }
        public Parameter UFM { get; }
        public Parameter IFM { get; }
        public Parameter IRRM { get; }
        public Parameter PRSM { get; }
        public Parameter Rca { get; }

        public DLParameters() : base()
        {
            //0
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //1
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //2
            UFM = new Parameter { tittleName = "UFM", dbName = "UFM" };
            this.list.Add(UFM);

            //3
            IFM = new Parameter { tittleName = "IFM", dbName = "" };
            this.list.Add(IFM);

            //4
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //5
            PRSM = new Parameter { tittleName = "PRSM", dbName = "PRSM" };
            this.list.Add(PRSM);

            //6
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (0):
                        case (1):
                        case (5):
                            offset++;
                            break;

                        case (2):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (4):
                            offset += 7;
                            break;

                        case (6):
                            offset += 3;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 2) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }

    public class DHParameters : Parameters
    //параметры прибора типа ДЧ
    {
        public Parameter URRM { get; }
        public Parameter UBR { get; }
        public Parameter UFM { get; }
        public Parameter IFM { get; }
        public Parameter IRRM { get; }
        public Parameter trr { get; }
        public Parameter Qrr { get; }
        public Parameter Rca { get; }

        public DHParameters() : base()
        {
            //0
            URRM = new Parameter { tittleName = "URRM", dbName = "VRRM" };
            this.list.Add(URRM);

            //1
            UBR = new Parameter { tittleName = "UBR", dbName = "Vbr" };
            this.list.Add(UBR);

            //2
            UFM = new Parameter { tittleName = "UFM", dbName = "UFM" };
            this.list.Add(UFM);

            //3
            IFM = new Parameter { tittleName = "IFM", dbName = "" };
            this.list.Add(IFM);

            //4
            IRRM = new Parameter { tittleName = "IRRM", dbName = "IRRM" };
            this.list.Add(IRRM);

            //5
            trr = new Parameter { tittleName = "trr", dbName = "TRR" };
            this.list.Add(trr);

            //6
            Qrr = new Parameter { tittleName = "Qrr", dbName = "QRR" };
            this.list.Add(Qrr);

            //7
            Rca = new Parameter { tittleName = "Rca", dbName = "" };
            this.list.Add(Rca);
        }

        public void ToExcel(Excel.Application exelApp, Excel.Worksheet sheet, int number, string LastUsedProfileID, ref int rowNum)
        {
            if ((exelApp != null) || (sheet != null))
            {
                Range rng = null;

                //выводим порядковый номер
                rng = this.range(exelApp, rowNum, 1);
                rng.Value2 = number.ToString();

                //определяем надо ли выводить температуру, имя параметра, ед. измерения 
                bool needHead = ((LastUsedProfileID == "") || (this.ProfileID != LastUsedProfileID));

                //выводим идентификационные данные                
                rng = this.range(exelApp, rowNum, 2);
                rng.NumberFormat = "@";
                rng.Value2 = this.Code;

                rng = this.range(exelApp, rowNum, 3);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN1;

                rng = this.range(exelApp, rowNum, 4);
                rng.NumberFormat = "@";
                rng.Value2 = this.SilN2;

                Parameter p, combinedp = null;
                int offset = 5;

                for (int i = 0; i <= this.Count - 1; i++)
                {
                    p = this.Items[i];

                    switch (i)
                    {
                        case (0):
                        case (1):
                        case (7):
                            offset++;
                            break;

                        case (2):
                            combinedp = Combine(this.Items[i], this.Items[i + 1]);

                            if (combinedp != null)
                                p = combinedp;

                            break;

                        case (4):
                            offset += 7;
                            break;

                        case (5):
                            offset += 2;
                            break;
                    }

                    int count = 0;

                    if (needHead)
                    {
                        //требуется вывести температуру, имя параметра, ед. измерения
                        rng = this.range(exelApp, rowNum, i + offset);
                        rng.Value2 = p.temperature.asString;
                        rng.HorizontalAlignment = -4108;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.tittleName;
                        rng.HorizontalAlignment = -4108;
                        rng.Font.Bold = true;
                        count++;

                        rng = this.range(exelApp, rowNum + count, i + offset);
                        rng.Value2 = p.um;
                        rng.HorizontalAlignment = -4108;
                        count++;
                    }

                    rng = this.range(exelApp, rowNum + count, i + offset);
                    rng.Value2 = p.value.ToString();
                    rng.HorizontalAlignment = -4108;

                    //если мы только что вывели объединённый столбец
                    if ((i == 2) && (p == combinedp))
                    {
                        i++;
                        offset--;
                    }
                }

                int rowNumEnd = needHead ? rowNum + 3 : rowNum;

                //очерчиваем выведенные данные
                SetBorders(exelApp, sheet, rowNum, rowNumEnd);

                //закрашиваем выведенные данные
                Paint(exelApp, sheet, rowNum, rowNumEnd);

                rowNum = rowNumEnd + 1;
            }
        }
    }
}
