using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCME.dbViewer
{
    static class Constants
    {
        public const string StringDelimeter = "\r\n";
        public const string Celsius = "°C";

        public const char DelimeterForStringConcatenate = ';';
        public const int StartConditionsParamersInDataGridIndex = 7;

        public const string ConditionsInDataSourceFirstIndex = "ConditionsInDataSourceFirstIndex";
        public const string ParametersInDataSourceFirstIndex = "ParametersInDataSourceFirstIndex";

        //желаемый пользователем порядок следования столбцов для отображения, если имени нет в данном списке - то нет и отображения в DataGrid
        //все имена пишем в верхнем регистре
        public static readonly string[] OrderedColumnNames = { "ITM", "UTM", "UBO", "UBR", Common.Constants.DUdt.ToUpper(), "IDRM", "IRRM", "IDSM", "IRSM", "PRSM", Common.Constants.Tq, Common.Constants.Trr, "IRR", Common.Constants.Qrr, "IGT", "UGT", "IH", "RG", "BVT_I", "UDRM", "URRM", "UBRMAX", "URSM", "UDSM", "DVDt_OK" };

        //желаемый пользователем порядок следования столбцов для вывода в Excel отчёт:
        //все обозначения в нижнем регистре, т.к. это содержимое сравнивается с содержимым от вызова Row.GetDynamicMemberNames();
        //именование содержимого - в терминах базы данных;
        //в этом списке есть как измеряемые параметры, так и условия измерений dvdt_voltagerate (dUdt)
        //имена не должны содержать цифры
        public static readonly string[] OrderedColumnNamesInReport = { "sl_itm", "vdrm", "vrrm", "idrm", "irrm", "dvdt_voltagerate", "vtm", "prsm", "tq", "trr", "irm", "irr", "qrr", "igt", "vgt", "ih", "rg" };

        public const string Prof_ID = "PROF_ID";

        /*
        public const string DevID = "DEV_ID";
        public const string ProfileID = "PROFILE_ID";
        public const string GroupName = "GROUP_NAME";
        public const string Item = "ITEM";
        public const string SiType = "SITYPE";
        public const string SiOmnity = "SIOMNITY";
        public const string Code = "CODE";
        public const string Ts = "TS";
        public const string ProfileName = "PROF_NAME";
        public const string ProfileBody = "PROFILEBODY";
        public const string DeviceType = "DEVICETYPE";
        public const string Constructive = "CONSTRUCTIVE";
        public const string AverageCurrent = "AVERAGECURRENT";
        public const string SapID = "SAPID";
        public const string DeviceClass = "DEVICECLASS";
        public const string MmeCode = "MME_CODE";
        public const string Usr = "USR";
        public const string Status = "STATUS";
        public const string CodeOfNonMatch = "CODEOFNONMATCH";
        public const string Reason = "REASON";
        */

        public const string XMLConditions = "PROFCONDITIONS";


        public const string HiddenMarker = "<!--hint-->";
        public const string RT = "RT";
        public const string TM = "TM";

        public const string GoodSatatus = "OK";
        public const string FaultSatatus = "Fault";

        public const string IsPairCreated = "IsPairCreated";
        public const string RecordIsStorage = "RecordIsStorage";

        public const string noData = "Нет данных";
        public const string Min = "Min";
        public const string Max = "Max";

        public const string AssemblyProtocolID = "AssemblyProtocolID";
        public const string AssemblyJob = "AssemblyJob";
        public const string PackageType = "PackageType";
        public const string Omnity = "Omnity";
        public const string Device = "Device";
        public const string DeviceTypeRu = "DeviceTypeRu";

        public const string dUdt = "dUdt";
        public const string Trr = "trr";
        public const string Tq = "tq";
        public const string Tgt = "tgt";
        public const string Prsm = "PRSM";
        public const string Qrr = "Qrr";
        

        public const string Igt = "Igt";
        public const string Ugt = "Ugt";

        public const string AssemblyReportRecordCount = "AssemblyReportRecordCount";
        public const string QtyReleasedByGroupName = "QtyReleasedByGroupName";
    }
}
