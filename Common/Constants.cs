using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCME.Common
{
    public static class Constants
    {
        public const string cStringDelimeter = "\r\n";
        public const string cNameSeparator = "©";
        public const string FromXMLNameSeparator = "©";
        public const char cString_AggDelimeter = '©';

        public const string Temperature = "CLAMP_Temperature";

        //максимальное значение температуры, которая соответствует температурному режиму RT
        public const int MaxTemperatureRT = 25;

        public const string AssemblyProtocolDescr = "ASSEMBLYPROTOCOLDESCR";
        public const string Code = "CODE";
        public const string Item = "ITEM";
        public const string Status = "STATUS";
        public const string Choice = "CHOICE";
        /*
        public const string SapID = "SAPID";
        public const string SapDescr = "SAPDESCR";
        */
        public const string AssemblyStatusID = "ASSEMBLYSTATUSID";
        public const string AssemblyStatusDescr = "ASSEMBLYSTATUSDESCR";
        public const string DeviceClass = "DEVICECLASS";
        public const string CodeOfNonMatch = "CODEOFNONMATCH";
        public const string GroupName = "GROUP_NAME";
        public const string DeviceTypeRU="DEVICETYPERU";
        public const string Constructive = "CONSTRUCTIVE";
        public const string AverageCurrent = "AVERAGECURRENT";
        public const string Reason = "REASON";
        public const string Ts = "TS";
        public const string MmeCode = "MME_CODE";
        public const string Usr = "USR";

        public const string AssemblyProtocolID = "ASSEMBLYPROTOCOLID";
        public const string Descr = "DESCR";
        public const string RecordCount = "RECORDCOUNT";
        public const string DeviceModeView = "DEVICEMODEVIEW";
        public const string AssemblyJob = "ASSEMBLYJOB";
        public const string Export = "EXPORT";
        public const string DeviceTypeEN = "DEVICETYPEEN";

        public const string SqlDUdt = "dVdt";
        public const string DUdt = "dU/dt";
        public const string Trr = "TRR";
        public const string Tq = "TQ";
        public const string Tgt = "TGT";
        public const string Qrr = "QRR";
        public const string Climatic = "CLIMATIC";
        public const string Omnity = "OMNITY";

        public const string DevID = "DEV_ID";
        public const string TDevID = "TDEV_ID";
        public const string GroupID = "GROUP_ID";
        public const string ProfileID = "PROFILE_ID";
        public const string ProfileName = "PROF_NAME";

        public const string SiType = "SITYPE";
        public const string SiOmnity = "SIOMNITY";

        public const string ProfileBody = "PROFILEBODY";
        public const string DeviceType = "DEVICETYPE";

        public const string DeviceComments = "DEVICECOMMENTS";
        public const string XMLConditions = "PROFCONDITIONS";

        public const string HiddenMarker = "<!--this is hidden storage-->";
        public const string RT = "RT";
        public const string TM = "TM";

        public const string GoodSatatus = "OK";
        public const string FaultSatatus = "Fault";

        public const string IsPairCreated = "IsPairCreated";
        public const string RecordIsStorage = "RecordIsStorage";

        public const string noData = "Нет данных";

        //номера битов разрешений на действия в системе
        public const byte cIsUserAdmin = 0;
        public const byte cIsUserCanReadCreateComments = 1;
        public const byte cIsUserCanReadComments = 2;
        public const byte cIsUserCanCreateParameter = 3;
        public const byte cIsUserCanEditParameter = 4;
        public const byte cIsUserCanDeleteParameter = 5;
        public const byte cIsUserCanCreateValueOfManuallyEnteredParameter = 6;
        public const byte cIsUserCanEditValueOfManuallyEnteredParameter = 7;
        public const byte cIsUserCanDeleteValueOfManuallyEnteredParameter = 8;
        public const byte cIsUserCanCreateDevices = 9;
        public const byte cWorkWithAssemblyProtocol = 10;
        public const byte cIsUserCanManageDeviceReferences = 11;
        public const byte cIsUserCanReadReason = 12;

        //сообщения windows для системы визуализации ожидания отклика приложения
        public const int WM_CLOSE = 0x0010;
        public const int WM_USER = 0x0400;
        public const int WM_HIDE = WM_USER + 1;
        public const int WM_HIDESortingFiltering = WM_USER + 2;
        public const int WM_SHOW = WM_USER + 3;
        public const int WM_SHOWSortingFiltering = WM_USER + 4;

        //Linker
        public const byte cEditAssembly = 14;
    }
}
