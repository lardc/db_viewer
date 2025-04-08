using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using SCME.Types.Profiles;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace SCME.Types
{
    public static class DBConnections
    {
        private static SqlConnection FConnection = null;
        private static SqlConnection FConnectionDC = null;
        private static SqlConnection FConnectionSL = null;
        private static SqlConnection FConnectionPRINET = null;

        public static SqlConnection Connection
        {
            //установка соединения с базой данных SCME_ResultsDB - центральная база данных хранения результатов измерений КИП СПП
            get
            {
                if (FConnection == null)
                {
                    //                                      192.168.2.154, 1433
                    //                                      192.168.0.120, 55387
                    FConnection = new SqlConnection("server=192.168.2.170, 1433; uid=sa; pwd=Hpl1520; database=SCME_ResultsDB; MultipleActiveResultSets=True");
                    //FConnection = new SqlConnection(@"Server =.\SQLExpress; AttachDbFilename = C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\DATA\SCME_ResultsDB.mdf; Database = SCME_ResultsDB; Trusted_Connection = Yes");
                }

                return FConnection;
            }
        }

        public static SqlConnection ConnectionDC
        {
            //установка соединения с базой данных DC
            get
            {
                if (FConnectionDC == null)
                    FConnectionDC = new SqlConnection("server=sa-011; uid=sa; pwd=Hpl1520; database=SL_PE_DC20002");

                return FConnectionDC;
            }
        }

        public static SqlConnection ConnectionSL
        {
            //установка соединения с базой данных SiteLine
            get
            {
                if (FConnectionSL == null)
                    FConnectionSL = new SqlConnection("server=sa-011; uid=sa; pwd=Hpl1520; database=SL_PE_App");

                return FConnectionSL;
            }
        }

        public static SqlConnection ConnectionPRINET
        {
            //установка соединения с базой данных печати этикеток
            get
            {
                if (FConnectionPRINET == null)
                    FConnectionPRINET = new SqlConnection("server=sa-011; uid=sa; pwd=Hpl1520; database=PrinEt");

                return FConnectionPRINET;
            }

        }
    }

    public class Interval<T> where T : struct, IComparable
    {
        public T? Start { get; set; }
        public T? End { get; set; }

        public Interval(T? start, T? end)
        {
            Start = start;
            End = end;
        }

        public bool InRange(T value)
        {
            return (
                    (!Start.HasValue || value.CompareTo(Start.Value) >= 0) &&
                    (!End.HasValue || End.Value.CompareTo(value) >= 0)
                   );
        }
    }

    public static class DbRoutines
    {
        const string cReadedRecordNotSingle = "Readed record not a single ({0}). One record was expected. {1}.";
        public const string cManually = "Manually";

        public static string AddTrailingZeros(string value, byte numberOfSigns)
        {
            //возвращает принятый value дополненный не значащами нулями (только слева) до общего количества знаков numberOfSigns
            int signsCount = value.Length;

            string trailingZeros = (numberOfSigns > signsCount) ? new string('0', numberOfSigns - signsCount) : string.Empty;

            return string.IsNullOrEmpty(value) ? null : string.Concat(trailingZeros, value);
        }

        public static object ChangeNullToDBNullValue(object value)
        {
            return value ?? DBNull.Value;
        }

        public static object ChangeDBNullToNullValue(object value)
        {
            return (value == DBNull.Value) ? null : value;
        }

        public static object ChoiceToBoolValue(string choice)
        {
            return string.IsNullOrEmpty(choice) ? DBNull.Value : (choice.ToLower() == "true") ? (object)true : (object)false;
        }

        public static object DeviceStatusToIntValue(string deviceStatus)
        {
            return string.IsNullOrEmpty(deviceStatus) ? DBNull.Value : (deviceStatus.ToUpper() == "OK") ? (object)1 : (object)0;
        }

        #region Permissions

        public static bool UserPermissions(long userID, out ulong permissionsLo)
        {
            //чтение битовой маски, хранящей права пользователя в данном приложении
            //возвращает:
            //true  - пользователь userID является пользователем приложения;
            //false - пользователь userID не является пользователем приложения (при этом он может быть пользователем системы DC)
            int count = 0;
            ulong readedPermissionsLo = 0;
            bool connectionOpened = false;

            SqlConnection connection = DBConnections.Connection;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = "SELECT PermissionsLo" +
                             " FROM Users WITH(NOLOCK)" +
                             string.Format(" WHERE (UserID='{0}')", userID);

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedPermissionsLo = Convert.ToUInt64(values[0]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //пользователь userID не является пользователем приложения
                    permissionsLo = 0;
                    return false;

                case 1:
                    //пользователь userID является пользователем приложения
                    permissionsLo = readedPermissionsLo;
                    return true;

                default:
                    //считано более одной записи для userID
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), userID.ToString()));
            }
        }

        #endregion

        #region Users

        public static bool UserLoginByUserID(long userID, out string userLogin)
        {
            //чтение имени пользователя UserLogin из системы DC по принятому идентификатору пользователя userID
            //возвращает:
            //true  - пользователь userID зарегистрирован в системе DC;
            //false - пользователь userID не является пользователем приложения (при этом он может быть пользователем системы DC)
            string readedUserLogin = null;
            int count = 0;

            SqlConnection connection = DBConnections.ConnectionDC;

            connection.Open();

            try
            {
                string sql = "SELECT USERLOGIN" +
                             " FROM RUSDC_Users WITH(NOLOCK)" +
                             string.Format(" WHERE (ID='{0}')", userID);

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedUserLogin = Convert.ToString(values[0]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //пользователь userID не имеет регистрации в системе DC
                    userLogin = null;
                    return false;

                case 1:
                    //пользователь userID имеет регистрацию в системе DC
                    userLogin = readedUserLogin;
                    return true;

                default:
                    //считано более одной записи для userID
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), "userID=" + userID.ToString()));
            }

        }

        public static bool FullUserNameByUserID(long userID, out string fullUserName)
        {
            //чтение полного имени пользователя из системы DC по принятому идентификатору пользователя userID
            //возвращает:
            //true  - пользователь userID зарегистрирован в системе DC;
            //false - пользователь userID не является пользователем приложения (при этом он может быть пользователем системы DC)
            string readedFullUserName = null;
            int count = 0;

            SqlConnection connection = DBConnections.ConnectionDC;

            connection.Open();

            try
            {
                string sql = "SELECT CONCAT(LASTNAME, ' ', CONCAT(SUBSTRING(FIRSTNAME, 1, 1), '.', SUBSTRING(MIDDLENAME, 1, 1), '.'))" +
                             " FROM RUSDC_USERS WITH(NOLOCK)" +
                             string.Format(" WHERE (ID='{0}')", userID);

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedFullUserName = Convert.ToString(values[0]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //пользователь userID не имеет регистрации в системе DC
                    fullUserName = null;
                    return false;

                case 1:
                    //пользователь userID имеет регистрацию в системе DC
                    fullUserName = readedFullUserName;
                    return true;

                default:
                    //считано более одной записи для userID
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), "userID=" + userID.ToString()));
            }
        }

        public static long CheckDCUserExist(string userName, string userPassword)
        {
            //проверяет наличие записи в талице RUSDC_USERS для сочетания User_Name - UserPassword
            //возвращает:
            // -1 - введённый пароль неверен, либо пользователя с именем userName не существует;
            // -2 - введённый пароль неверен;
            // больше, чем ноль - идентификатор пользователя, пользователь userName является пользователем DC
            string sql = "SELECT ID, USERPASSWORD" +
                         " FROM RUSDC_USERS WITH(NOLOCK)" +
                         " WHERE (" +
                         string.Format("USERLOGIN='{0}' AND ", userName) +
                         string.Format("USERPASSWORD='{0}'", userPassword) +
                         "       )";

            long userID = -1;
            string password = null;
            int count = 0;

            SqlConnection connection = DBConnections.ConnectionDC;
            connection.Open();

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        userID = Convert.ToInt64(values[0]);
                        password = (values[1] == DBNull.Value) ? null : values[1].ToString();

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //введённый пароль неверен, либо пользователя с именем userName не существует
                        return -1;

                    case 1:
                        //запись найдена, но её поиск выполнялся по значению поля 'userPassword' без учёта регистра написания, проверяем введённый пароль с учётом регистра
                        return (userPassword == password) ? userID : -2;

                    default:
                        //считано более одной записи для userName
                        throw new Exception(string.Format("Считано более одной записи ({0}) из DC для пользователя '{1}'. Ожидалось либо ноль, либо одна запись.", count.ToString(), userName));
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        private static void InsertToUsers(long userID, ulong permissionsLo)
        {
            //вставка новой записи в таблицу USERS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"INSERT INTO USERS(USERID, PERMISSIONSLO)
                           VALUES (@UserID, @PermissionsLo)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserID", SqlDbType.BigInt).Value = userID;
            command.Parameters.Add("@PermissionsLo", SqlDbType.BigInt).Value = permissionsLo;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        private static void UpdateUsers(long userID, ulong permissionsLo)
        {
            //изменение битовой маски прав permissionsLo пользователя userID в таблице USERS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE USERS
                           SET PERMISSIONSLO=@PermissionsLo
                           WHERE (USERID=@UserID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserID", SqlDbType.BigInt).Value = userID;
            command.Parameters.Add("@PermissionsLo", SqlDbType.BigInt).Value = permissionsLo;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteFromUsers(long userID)
        {
            //удаление записи о пользователе userID из таблицы USERS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM USERS
                           WHERE (USERID=@UserID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserID", SqlDbType.BigInt).Value = userID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void SaveToUsers(long userID, ulong permissionsLo)
        {
            //выполняет сохранение битовой маски разрешений permissionsLo для пользователя userID
            //проверяем наличие пользователя userID в таблице Users
            if (UserPermissions(userID, out ulong oldPermissionsLo))
            {
                //пользователь userID имеет описание своих прав в Users
                if (permissionsLo != oldPermissionsLo)
                {
                    //сохранённая битовая маска разрешений отличается от сохраняемой
                    UpdateUsers(userID, permissionsLo);
                }
            }
            else
            {
                //пользователь userID не имеет описания своих прав в Users
                InsertToUsers(userID, permissionsLo);
            }
        }

        #endregion

        #region ManualInputParams

        public static void GetManualInputParams(DataTable data, int? profID, List<TemperatureCondition> listTemperatureCondition)
        {
            //listTemperatureCondition - список температурных режимов параметров созданных вручную
            if ((data != null) && (listTemperatureCondition != null))
            {
                data.Clear();

                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();

                    //формируем часть where
                    IEnumerable<TemperatureCondition> list = listTemperatureCondition.Distinct();

                    string where = string.Empty;
                    int index = 0;
                    foreach (TemperatureCondition tc in list)
                    {
                        if (where != string.Empty)
                            where = string.Concat(where, " OR ");

                        where = string.Concat(where, "MP.TEMPERATURECONDITION=@TemperatureCondition", index);
                        index++;
                    }

                    string sqlText = @"SELECT MP.MANUALINPUTPARAMID, MP.NAME, MP.TEMPERATURECONDITION, MP.UM, MP.DESCREN, MP.DESCRRU";

                    if (profID == null)
                    {
                        sqlText = string.Concat(sqlText, ", NULL AS MINVAL, NULL AS MAXVAL");
                    }
                    else
                        sqlText = string.Concat(sqlText, ", MPN.MINVAL, MPN.MAXVAL");

                    sqlText = string.Concat(sqlText, "\r\n", "FROM MANUALINPUTPARAMS MP WITH(NOLOCK)");

                    //если значение profID не Null - значит надо прочитать значения норм
                    if (profID != null)
                    {
                        sqlText = string.Concat(sqlText, "\r\n");
                        sqlText = string.Concat(sqlText, @" LEFT JOIN MANUALINPUTPARAMNORMS MPN WITH(NOLOCK) ON (
                                                                                                                 (MP.MANUALINPUTPARAMID=MPN.MANUALINPUTPARAMID) AND
                                                                                                                 (MPN.Prof_ID=@Prof_ID)
                                                                                                                )");
                    }

                    sqlText = string.Concat(sqlText, "\r\n");
                    sqlText = string.Concat(sqlText, "WHERE ", where);

                    adapter.SelectCommand = new SqlCommand(sqlText, connection);

                    //формируем список параметров                   
                    index = 0;
                    foreach (TemperatureCondition tc in list)
                    {
                        adapter.SelectCommand.Parameters.Add(string.Concat("@TemperatureCondition", index), SqlDbType.NVarChar).Value = tc.ToString();
                        index++;
                    }

                    if (profID != null)
                        adapter.SelectCommand.Parameters.Add("@Prof_ID", SqlDbType.Int).Value = profID;

                    adapter.Fill(data);
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        private static bool CheckManualInputParamExist(string name, TemperatureCondition temperatureCondition, out int manualInputParamID)
        {
            //проверяет наличие записи в талице MANUALINPUTPARAMS
            //возвращает:
            // true - в таблице ManualInputParams обнаружена единственная запись с принятыми name и temperatureCondition. в manualInputParamID будет возвращён идентификатор найденного параметра;
            // false - в таблице ManualInputParams нет ни одной записи с принятым name;
            string sql = "SELECT MANUALINPUTPARAMID" +
                         " FROM MANUALINPUTPARAMS WITH(NOLOCK)" +
                         " WHERE (" +
                         "        (NAME=@Name) AND" +
                         "        (TEMPERATURECONDITION=@TemperatureCondition)" +
                         "       )";

            manualInputParamID = -1;
            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;
                command.Parameters.Add("@TemperatureCondition", SqlDbType.NVarChar).Value = temperatureCondition.ToString();

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        manualInputParamID = Convert.ToInt32(values[0]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //параметра с принятыми name и temperatureCondition не существует в таблице ManualInputParams
                    return false;

                case 1:
                    //запись найдена
                    return true;

                default:
                    //считано более одной записи для принятых name и temperatureCondition
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("name=", name, "temperatureCondition=", temperatureCondition.ToString())));
            }
        }

        public static bool CheckManualInputParamExist(string temperatureConditionAndName, out int manualInputParamID)
        {
            //проверяет наличие записи в талице MANUALINPUTPARAMS
            //во входном параметре temperatureConditionAndName принимает строку слепленную из temperatureCondition и Name
            //возвращает:
            // true - в таблице ManualInputParams обнаружена единственная запись с принятыми name и temperatureCondition. в manualInputParamID будет возвращён идентификатор найденного параметра;
            // false - в таблице ManualInputParams нет ни одной записи с принятым name;
            string sql = @"SELECT MANUALINPUTPARAMID
                           FROM MANUALINPUTPARAMS WITH(NOLOCK)
                           WHERE (
                                  CONCAT(TEMPERATURECONDITION, NAME)=@TemperatureConditionAndName
                                 )";

            manualInputParamID = -1;
            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@TemperatureConditionAndName", SqlDbType.NVarChar).Value = temperatureConditionAndName;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        manualInputParamID = Convert.ToInt32(values[0]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //искомого параметра не существует в таблице ManualInputParams
                    return false;

                case 1:
                    //запись найдена
                    return true;

                default:
                    //считано более одной записи для принятых name и temperatureCondition
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), temperatureConditionAndName));
            }
        }

        private static int InsertToManualInputParam(string name, TemperatureCondition temperatureCondition, string um, string descrEN, string descrRU)
        {
            //вставка новой записи в таблицу MANUALINPUTPARAMS
            int manualInputParamID = -1;

            SqlConnection connection = DBConnections.Connection;

            string sql = @"INSERT INTO MANUALINPUTPARAMS(NAME, TEMPERATURECONDITION, UM, DESCREN, DESCRRU)
                           OUTPUT INSERTED.MANUALINPUTPARAMID VALUES (@Name, @TemperatureCondition, @Um, @DescrEN, @DescrRU)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;
            command.Parameters.Add("@TemperatureCondition", SqlDbType.NVarChar).Value = temperatureCondition.ToString();
            command.Parameters.Add("@Um", SqlDbType.NVarChar).Value = um;
            command.Parameters.Add("@DescrEN", SqlDbType.NVarChar).Value = descrEN;
            command.Parameters.Add("@DescrRU", SqlDbType.NVarChar).Value = descrRU;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                //считываем идентификатор созданного параметра
                manualInputParamID = (int)command.ExecuteScalar();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return manualInputParamID;
        }

        private static void UpdateManualInputParam(int manualInputParamID, string name, TemperatureCondition temperatureCondition, string um, string descrEN, string descrRU)
        {
            //изменение реквизитов параметра в таблице MANUALINPUTPARAMS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE MANUALINPUTPARAMS
                           SET NAME=@Name,
                               TEMPERATURECONDITION=@TemperatureCondition,
                               UM=@Um,
                               DESCREN=@DescrEN,
                               DESCRRU=@DescrRU
                           WHERE (MANUALINPUTPARAMID=@ManualInputParamID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;
            command.Parameters.Add("@TemperatureCondition", SqlDbType.NVarChar).Value = temperatureCondition.ToString();
            command.Parameters.Add("@Um", SqlDbType.NVarChar).Value = um;
            command.Parameters.Add("@DescrEN", SqlDbType.NVarChar).Value = descrEN;
            command.Parameters.Add("@DescrRU", SqlDbType.NVarChar).Value = descrRU;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        private static void DeleteFromManualInputParams(int manualInputParamID)
        {
            //удаление записи о вручную созданном параметре из таблицы MANUALINPUTPARAMS с идентификатором manualInputParamID
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM MANUALINPUTPARAMS
                           WHERE (MANUALINPUTPARAMID=@ManualInputParamID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteAllByManualInputParamID(int manualInputParamID)
        {
            //сначала удаляет все определения параметра с принятым manualInputParamID в таблице ManualInputDevParam, после чего удаляет данный параметр из справочника MANUALINPUTDEVPARAM
            DeleteFromManualInputDevParam(manualInputParamID);
            DeleteFromManualInputParams(manualInputParamID);
        }

        public static int SaveToManualInputParams(int? manualInputParamID, string name, TemperatureCondition temperatureCondition, string um, string descrEN, string descrRU)
        {
            //выполняет сохранение параметра не предназначенного для измерения КИП СПП (его значение может быть введено пользователем вручную, оно никогда не будет измеряться средствами КИП СПП)
            //возвращает идентификатор созданного параметра
            //если принятый manualInputParamID=null - выполняется попытка создания нового параметра, иначе выполняется редактирование параметра
            int result;

            if (manualInputParamID == null)
            {
                //выполняется попытка создания параметра
                result = InsertToManualInputParam(name, temperatureCondition, um, descrEN, descrRU);
            }
            else
            {
                //выполняется попытка редактирования параметра
                result = (int)manualInputParamID;
                UpdateManualInputParam(result, name, temperatureCondition, um, descrEN, descrRU);
            }

            return result;
        }

        #endregion

        #region ManualInputDevParamNorms

        private static bool IsExistsManualInputParamNorms(int manualInputParamID, int profID)
        {
            //проверка наличия записи в MANUALINPUTPARAMNORMS для принятых manualInputParamID и prof_ID
            bool result = false;

            SqlConnection connection = DBConnections.Connection;

            string sql = @"SELECT COUNT(*) AS NUMROWS
                           FROM MANUALINPUTPARAMNORMS WITH(NOLOCK) 
                           WHERE (
                                  (MANUALINPUTPARAMID=@ManualInputParamID) AND
                                  (PROF_ID=@ProfID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@ProfID", SqlDbType.Int).Value = profID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        if (values[0] is int numrows)
                            result = (numrows > 0);
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        private static void InsertToManualInputParamNorms(int manualInputParamID, int profID, double? minVal, double? maxVal)
        {
            //вставка новой записи в таблицу MANUALINPUTPARAMNORMS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"INSERT INTO MANUALINPUTPARAMNORMS(MANUALINPUTPARAMID, PROF_ID, MINVAL, MAXVAL)
                           VALUES (@ManualInputParamID, @ProfID, @MinVal, @MaxVal)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@ProfID", SqlDbType.Int).Value = profID;

            if (minVal == null)
            {
                command.Parameters.Add("@MinVal", SqlDbType.Float).Value = DBNull.Value;
            }
            else
                command.Parameters.Add("@MinVal", SqlDbType.Float).Value = minVal;

            if (maxVal == null)
            {
                command.Parameters.Add("@MaxVal", SqlDbType.Float).Value = DBNull.Value;
            }
            else
                command.Parameters.Add("@MaxVal", SqlDbType.Float).Value = maxVal;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteScalar();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        private static void UpdateManualInputParamNorms(int manualInputParamID, int profID, double? minVal, double? maxVal)
        {
            //изменение значений норм в таблице MANUALINPUTPARAMNORMS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE MANUALINPUTPARAMNORMS
                           SET MINVAL=@MinVal,
                               MAXVAL=@MaxVal
                           WHERE (
                                  (MANUALINPUTPARAMID=@ManualInputParamID) AND
                                  (PROF_ID=@ProfID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@ProfID", SqlDbType.Int).Value = profID;

            if (minVal == null)
            {
                command.Parameters.Add("@MinVal", SqlDbType.Float).Value = DBNull.Value;
            }
            else
                command.Parameters.Add("@MinVal", SqlDbType.Float).Value = minVal;

            if (maxVal == null)
            {
                command.Parameters.Add("@MaxVal", SqlDbType.Float).Value = DBNull.Value;
            }
            else
                command.Parameters.Add("@MaxVal", SqlDbType.Float).Value = maxVal;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        private static void DeleteFromManualInputParamNorms(int manualInputParamID, int profID)
        {
            //удаление записи о значениях норм на созданный вручную параметр из таблицы MANUALINPUTPARAMNORMS с идентификатором manualInputParamID
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM MANUALINPUTPARAMNORMS
                           WHERE (
                                  (MANUALINPUTPARAMID=@ManualInputParamID) AND
                                  (PROF_ID=@ProfID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@ProfID", SqlDbType.Int).Value = profID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void SaveToManualInputParamNorms(int manualInputParamID, int profID, double? minVal, double? maxVal)
        {
            //выполняет сохранение значения норм на параметр не предназначенный для измерения КИП СПП (его значение может быть введено пользователем вручную, оно никогда не будет измеряться средствами КИП СПП)
            //если принятый manualInputParamID=null - выполняется попытка создания нового параметра, иначе выполняется редактирование параметра
            bool normExists = IsExistsManualInputParamNorms(manualInputParamID, profID);

            if (normExists)
            {
                //выполняется попытка редактирования норм
                UpdateManualInputParamNorms(manualInputParamID, profID, minVal, maxVal);
            }
            else
            {
                //выполняется попытка создания новой записи
                InsertToManualInputParamNorms(manualInputParamID, profID, minVal, maxVal);
            }
        }

        /*
        public static bool LoadManualInputParamNorms(string paramName, int profID, out double? minVal, out double? maxVal)
        {
            //чтение норм на вручную созданный параметр paramName
            //возвращает:
            //            true  - по принятым paramName, profID найдена запись со значениями minVal и maxVal;
            //            false - по принятым paramName, profID нет данных

            minVal = null;
            maxVal = null;
            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            string sql = @"SELECT MIPN.MINVAL, MIPN.MAXVAL
                           FROM MANUALINPUTPARAMNORMS MIPN WITH(NOLOCK)
                            INNER JOIN MANUALINPUTPARAMS MIP WITH(NOLOCK) ON (
                                                                              (MIPN.MANUALINPUTPARAMID=MIP.MANUALINPUTPARAMID) AND
                                                                              (MIP.NAME=@Name)
                                                                             )
                           WHERE (MIPN.PROF_ID=@Prof_ID)";

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = paramName;
                command.Parameters.Add("@Prof_ID", SqlDbType.Int).Value = profID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    int index;
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        index = reader.GetOrdinal("MINVAL");
                        object value = values[index];
                        minVal = (value == DBNull.Value) ? null : double.TryParse(value.ToString(), out double min) ? (double?)min : null;

                        index = reader.GetOrdinal("MAXVAL");
                        value = values[index];
                        maxVal = (value == DBNull.Value) ? null : double.TryParse(value.ToString(), out double max) ? (double?)max : null;

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //искомой записи не существует в таблице MANUALINPUTPARAMNORMS
                    return false;

                case 1:
                    //запись найдена
                    return true;

                default:
                    //считано более одной записи для принятых paramName, profID
                    string identificators = string.Format("paramName='{0}', profID={1}", paramName, profID.ToString());
                    throw new Exception(string.Format("LoadManualInputParamNorms. ", cReadedRecordNotSingle, count.ToString(), identificators));
            }
        }
        */

        #endregion

        #region ManualInputDevParam

        public static void InsertToManualInputDevParam(SqlConnection connection, SqlTransaction transaction, int dev_ID, int manualInputParamID, double? value)
        {
            //вставка новой записи в таблицу ManualInputDevParam
            string sql = @"INSERT INTO MANUALINPUTDEVPARAM(DEV_ID, MANUALINPUTPARAMID, VALUE)
                           VALUES (@Dev_ID, @ManualInputParamID, @Value)";

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = dev_ID;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@Value", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(value);

            command.ExecuteNonQuery();
        }

        private static void UpdateManualInputDevParam(SqlConnection connection, SqlTransaction transaction, int dev_ID, int manualInputParamID, double? value)
        {
            //редактирование записи в таблице MANUALINPUTDEVPARAM
            string sql = @"UPDATE MANUALINPUTDEVPARAM
                           SET VALUE=@Value
                           WHERE (
                                  (DEV_ID=@Dev_ID) AND
                                  (MANUALINPUTPARAMID=@ManualInputParamID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = dev_ID;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@Value", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(value);

            command.ExecuteNonQuery();
        }

        private static bool ExistManualInputDevParam(SqlConnection connection, SqlTransaction transaction, int dev_ID, int manualInputParamID)
        {
            //проверяет наличие записи в талице MANUALINPUTDEVPARAM для сочетания dev_ID - manualInputParamID
            //возвращает:
            // true - сочетание dev_ID - manualInputParamID найдено;
            // false - сочетания dev_ID - manualInputParamID не существует;
            string sql = @"SELECT COUNT(*)
                           FROM MANUALINPUTDEVPARAM WITH(NOLOCK)
                           WHERE (
                                  (DEV_ID=@Dev_ID) AND
                                  (MANUALINPUTPARAMID=@ManualInputParamID)
                                 )";

            int recordCount = 0;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = dev_ID;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;

            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(values);
                    recordCount = Convert.ToInt32(values[0]);
                }
            }

            finally
            {
                reader.Close();
            }

            //одна запись всегда есть
            return (recordCount > 0);
        }

        public static void SaveToManualInputDevParam(SqlConnection connection, SqlTransaction transaction, int dev_ID, int manualInputParamID, double? value)
        {
            //выполняет сохранение значения вручную созданного параметра применительно к изделию            
            if (ExistManualInputDevParam(connection, transaction, dev_ID, manualInputParamID))
            {
                //выполняется попытка редактирования значения параметра
                UpdateManualInputDevParam(connection, transaction, dev_ID, manualInputParamID, value);
            }
            else
            {
                //выполняется попытка создания новой записи
                InsertToManualInputDevParam(connection, transaction, dev_ID, manualInputParamID, value);
            }
        }

        public static void ExchangeManualInputDevParam(int devID, int oldManualInputParamID, int newManualInputParamID)
        {
            //для изделия devID выполняет замену идентификатора вручную введённого параметра с oldManualInputParamID на newManualInputParamID
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE MANUALINPUTDEVPARAM
                           SET MANUALINPUTPARAMID=@NewManualInputParamID
                           WHERE (
                                   (DEV_ID=@Dev_ID) AND
                                   (MANUALINPUTPARAMID=@OldManualInputParamID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = devID;
            command.Parameters.Add("@NewManualInputParamID", SqlDbType.Int).Value = newManualInputParamID;
            command.Parameters.Add("@OldManualInputParamID", SqlDbType.Int).Value = oldManualInputParamID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteFromManualInputDevParam(int devID, int manualInputParamID)
        {
            //удаление места хранения значения параметра с идентификатором devID, manualInputParamID из таблицы ManualInputDevParam
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM MANUALINPUTDEVPARAM
                           WHERE (
                                  (DEV_ID=@Dev_ID) AND
                                  (MANUALINPUTPARAMID=@ManualInputParamID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = devID;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteFromManualInputDevParamByDevID(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //удаление места хранения всех имеющихся значений параметров изделия с идентификатором devID из таблицы ManualInputDevParam
            string sql = @"DELETE
                           FROM MANUALINPUTDEVPARAM
                           WHERE (DEV_ID=@Dev_ID)";

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = devID;

            command.ExecuteNonQuery();
        }

        private static void DeleteFromManualInputDevParam(int manualInputParamID)
        {
            //удаление всех мест хранения значений параметра с идентификатором manualInputParamID из таблицы ManualInputDevParam
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM MANUALINPUTDEVPARAM
                           WHERE (MANUALINPUTPARAMID=@ManualInputParamID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteFromManualInputAssemblyProtocolParam(int assemblyProtocolID, int manualInputParamID)
        {
            //удаление места хранения значения параметра с идентификатором manualInputParamID протокола сбоки с идентификатором assemblyProtocolID из таблицы ManualInputDevParam
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM MANUALINPUTASSEMBLYPROTOCOLPARAM
                           WHERE (
                                  (ASSEMBLYPROTOCOLID=@AssemblyProtocolID) AND
                                  (MANUALINPUTPARAMID=@ManualInputParamID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteFromManualInputAssemblyProtocolParamByAssemblyProtocolID(int assemblyProtocolID)
        {
            //удаление всех мест хранения параметров протокола сборки assemblyProtocolID из таблицы ManualInputAssemblyProtocolParam
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM MANUALINPUTASSEMBLYPROTOCOLPARAM
                           WHERE (ASSEMBLYPROTOCOLID=@AssemblyProtocolID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void InsertToManualInputAssemblyProtocolParam(int assemblyProtocolID, int manualInputParamID, double value)
        {
            //вставка новой записи в таблицу ManualInputDevParam
            SqlConnection connection = DBConnections.Connection;

            string sql = @"INSERT INTO MANUALINPUTASSEMBLYPROTOCOLPARAM(ASSEMBLYPROTOCOLID, MANUALINPUTPARAMID, VALUE)
                           VALUES (@AssemblyProtocolID, @ManualInputParamID, @Value)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@Value", SqlDbType.Decimal).Value = value;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        private static void UpdateManualInputAssemblyProtocolParam(int assemblyProtocolID, int manualInputParamID, double value)
        {
            //редактирование записи в таблице MANUALINPUTDEVPARAM
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE MANUALINPUTASSEMBLYPROTOCOLPARAM
                           SET VALUE=@Value
                           WHERE (
                                  (ASSEMBLYPROTOCOLID=@AssemblyProtocolID) AND
                                  (MANUALINPUTPARAMID=@ManualInputParamID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
            command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
            command.Parameters.Add("@Value", SqlDbType.Decimal).Value = value;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        private static bool ExistManualInputAssemblyProtocolParam(int assemblyProtocolID, int manualInputParamID)
        {
            //проверяет наличие записи в талице MANUALINPUTASSEMBLYPROTOCOLPARAM для сочетания assemblyProtocolID - manualInputParamID
            //возвращает:
            // true - сочетание assemblyProtocolID - manualInputParamID найдено;
            // false - сочетания assemblyProtocolID - manualInputParamID не существует;
            string sql = @"SELECT COUNT(*)
                           FROM MANUALINPUTASSEMBLYPROTOCOLPARAM WITH(NOLOCK)
                           WHERE (
                                  (MANUALINPUTPARAMID=@ManualInputParamID) AND
                                  (ASSEMBLYPROTOCOLID=@AssemblyProtocolID)
                                 )";

            long recordCount = 0;
            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@ManualInputParamID", SqlDbType.Int).Value = manualInputParamID;
                command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        recordCount = Convert.ToInt32(values[0]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 1:
                        return (recordCount == 0) ? false : true;

                    default:
                        //считано более одной записи
                        throw new Exception(string.Concat("ExistManualInputAssemblyProtocolParam: ", string.Format(cReadedRecordNotSingle, count.ToString())));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void SaveToManualInputAssemblyProtocolParam(int assemblyProtocolID, int manualInputParamID, double value)
        {
            //выполняет сохранение значения вручную созданного параметра применительно к параметру протокола
            //если место хранения для принытых assemblyProtocolID, manualInputParamID существует - выполняется редактирование значения параметра, иначе выполняется попытка создания места хранения значения параметра с идентификатором manualInputParamID для протокола сборки assemblyProtocolID
            if (ExistManualInputAssemblyProtocolParam(assemblyProtocolID, manualInputParamID))
            {
                //выполняется попытка редактирования значения параметра
                UpdateManualInputAssemblyProtocolParam(assemblyProtocolID, manualInputParamID, value);
            }
            else
            {
                //выполняется попытка создания нового места хранения параметра manualInputParamID для протокола сборки assemblyProtocolID
                InsertToManualInputAssemblyProtocolParam(assemblyProtocolID, manualInputParamID, value);
            }
        }

        public class ColumnBindingDescr
        {
            public string Header { get; set; }
            public string BindPath { get; set; }
        }

        public static List<ColumnBindingDescr> LoadManualInputDevParam(DataTable dataTable, string groupName, string profileGUID, int? assemblyProtocolID)
        {
            //загрузка созданных вручную параметров в принятый dataTable
            //данная реализация вызывается для двух случаев:
            // фильтр по протоколу сборки не включен - входной параметр assemblyProtocolID=null;
            // фильтр по протоколу сборки включен - входной параметр assemblyProtocolID не равен null
            List<ColumnBindingDescr> result = null;

            if ((dataTable != null) && (!string.IsNullOrEmpty(groupName)) && (!string.IsNullOrEmpty(profileGUID)))
            {
                //по принятому groupName вычисляем идентификатор ПЗ
                int? groupID = GroupIDByGroupName(groupName);

                if (groupID != null)
                {
                    result = new List<ColumnBindingDescr>();

                    //получаем список DevID по вычисленному groupID
                    SqlConnection connection = DBConnections.Connection;

                    bool connectionOpened = false;

                    if (!IsDBConnectionAlive(connection))
                    {
                        connection.Open();
                        connectionOpened = true;
                    }

                    try
                    {
                        string sql = @"SELECT D.DEV_ID, D.NUMFROMCODE, MIP.NAME, MIDP.MANUALINPUTPARAMID, MIDP.VALUE
                                       FROM DEVICES D WITH(NOLOCK)
                                        LEFT JOIN MANUALINPUTDEVPARAM MIDP WITH(NOLOCK) ON (D.DEV_ID=MIDP.DEV_ID)
                                        INNER JOIN MANUALINPUTPARAMS MIP WITH(NOLOCK) ON (MIDP.MANUALINPUTPARAMID=MIP.MANUALINPUTPARAMID)
                                       WHERE (
                                              (D.GROUP_ID=@GroupID) AND
                                              (D.PROFILE_ID=@ProfileGUID) AND
                                              (D.ISLASTMEASUREMENT=1) AND
                                              (IIF(ISNULL(D.ASSEMBLYPROTOCOLID, -1)=ISNULL(ISNULL(@AssemblyProtocolID, D.ASSEMBLYPROTOCOLID), -1), 1, 0)=1)
                                             )
                                       ORDER BY D.NUMFROMCODE";

                        string code = null;
                        string manualInputParamID = null;

                        SqlCommand command = new SqlCommand(sql, connection);
                        command.Parameters.Add("@GroupID", SqlDbType.Int).Value = groupID;
                        command.Parameters.Add("@ProfileGUID", SqlDbType.UniqueIdentifier).Value = new Guid(profileGUID);
                        command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = ChangeNullToDBNullValue(assemblyProtocolID);

                        SqlDataReader reader = command.ExecuteReader();

                        try
                        {
                            //нулевым столбцом должен быть столбец CODE
                            //он уже должен быть создан

                            if (reader.HasRows)
                                dataTable.Clear();

                            while (reader.Read())
                            {
                                if (int.TryParse(reader["DEV_ID"].ToString(), out int devID))
                                {
                                    //в столбце "NUMFROMCODE" допускается значение null
                                    code = reader["NUMFROMCODE"].ToString();

                                    if (!string.IsNullOrEmpty(code))
                                    {
                                        //ищем в dataTable.Rows запись с идентификатором devID
                                        DataRow[] rows = dataTable.Select(string.Format("DEV_ID={0}", devID));

                                        DataRow row;

                                        switch (rows.Length)
                                        {
                                            //запись с обрабатываемым значением devID необходимо создать
                                            case 0:
                                                row = dataTable.NewRow();

                                                row["DEV_ID"] = devID;
                                                row["CODE"] = code;

                                                dataTable.Rows.Add(row);
                                                break;

                                            //запись с обрабатываемым значением devID уже имеется в dataTable
                                            default:
                                                row = rows[0];
                                                break;
                                        }

                                        manualInputParamID = reader["MANUALINPUTPARAMID"].ToString();

                                        //столбец имя которого есть идентификатор параметра может содержаться в dataTable.Columns только в единственном числе
                                        if (dataTable.Columns.IndexOf(manualInputParamID) == -1)
                                        {
                                            //сознательно отказался от использования typeof(double) т.к. при этом типе столбца нельзя вводить значения с разделителем дробной части
                                            //чтобы вписать число с  разделителем целой части от дробной надо написать число например 025 и потом поставить разделитель после нуля - получится 0.25
                                            //это не удобно - поэтому стал использовать тип typeof(string)
                                            DataColumn tableColumn = dataTable.Columns.Add(manualInputParamID, typeof(string)); //исправить на double
                                            tableColumn.Unique = false;
                                            tableColumn.AllowDBNull = true;
                                            tableColumn.AutoIncrement = false;
                                            tableColumn.DefaultValue = DBNull.Value;

                                            //формируем описание столбцов, чтобы вызывающая реализация могла создать столбцы в DataGrid
                                            result.Add(new ColumnBindingDescr
                                            {
                                                Header = reader["NAME"].ToString(),
                                                BindPath = manualInputParamID
                                            });
                                        }

                                        string sValue = reader["VALUE"].ToString();

                                        switch (string.IsNullOrEmpty(sValue))
                                        {
                                            case true:
                                                row[manualInputParamID] = DBNull.Value;

                                                break;

                                            default:
                                                if (double.TryParse(reader["VALUE"].ToString(), out double dValue))
                                                    row[manualInputParamID] = dValue;

                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        finally
                        {
                            reader.Close();
                        }
                    }

                    finally
                    {
                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                        //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                        if (connectionOpened)
                            connection.Close();
                    }
                }
            }

            return result;
        }

        public static void LoadCodeNumbers(DataTable dataTable, string groupName, string profileGUID, int? assemblyProtocolID)
        {
            //загрузка списка номеров изделий в принятый dataTable для облегчения труда оператора при формировании списка параметров созданных вручную
            //данная реализация вызывается для двух случаев:
            // фильтр по протоколу сборки не включен - входной параметр assemblyProtocolID=null;
            // фильтр по протоколу сборки включен - входной параметр assemblyProtocolID не равен null

            if ((dataTable != null) && (!string.IsNullOrEmpty(groupName)) && (!string.IsNullOrEmpty(profileGUID)))
            {
                //по принятому groupName вычисляем идентификатор ПЗ
                int? groupID = GroupIDByGroupName(groupName);

                if (groupID != null)
                {
                    //получаем список DevID по вычисленному groupID
                    SqlConnection connection = DBConnections.Connection;

                    bool connectionOpened = false;

                    if (!IsDBConnectionAlive(connection))
                    {
                        connection.Open();
                        connectionOpened = true;
                    }

                    try
                    {
                        string sql = @"SELECT DEV_ID, NUMFROMCODE, CODE
                                       FROM DEVICES WITH(NOLOCK)
                                       WHERE (
                                              (GROUP_ID=@GroupID) AND
                                              (PROFILE_ID=@ProfileGUID) AND
                                              (ISLASTMEASUREMENT=1) AND
                                              (IIF(ISNULL(ASSEMBLYPROTOCOLID, -1)=ISNULL(ISNULL(@AssemblyProtocolID, ASSEMBLYPROTOCOLID), -1), 1, 0)=1)
                                             )
                                       ORDER BY NUMFROMCODE";

                        SqlCommand command = new SqlCommand(sql, connection);
                        command.Parameters.Add("@GroupID", SqlDbType.Int).Value = groupID;
                        command.Parameters.Add("@ProfileGUID", SqlDbType.UniqueIdentifier).Value = new Guid(profileGUID);
                        command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = ChangeNullToDBNullValue(assemblyProtocolID);

                        SqlDataReader reader = command.ExecuteReader();

                        try
                        {
                            //столбцы 'CODE' и 'DEV_ID' в dataTable должны быть созданы до вызова данной реализации
                            object codeObj = null;
                            string code = null;

                            if (reader.HasRows)
                                dataTable.Clear();

                            while (reader.Read())
                            {
                                if (int.TryParse(reader["DEV_ID"].ToString(), out int devID))
                                {
                                    //в столбце "NUMFROMCODE" допускается значение null
                                    //столбец "CODE" не допускает значение null
                                    codeObj = reader["NUMFROMCODE"];
                                    code = (codeObj == DBNull.Value) ? reader["CODE"].ToString() : codeObj.ToString();

                                    if (!string.IsNullOrEmpty(code))
                                    {
                                        DataRow row = dataTable.NewRow();

                                        row["DEV_ID"] = devID;
                                        row["CODE"] = code;

                                        dataTable.Rows.Add(row);
                                    }
                                }
                            }
                        }

                        finally
                        {
                            reader.Close();
                        }
                    }

                    finally
                    {
                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                        //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                        if (connectionOpened)
                            connection.Close();
                    }
                }
            }
        }
        #endregion

        #region Devices

        public static bool IsDeviсesUseAssemblyProtocolID(int assemblyProtocolID)
        {
            //проверяет наличие ссылок в DEVICES.ASSEMBLYPROTOCOLID на принятый assemblyProtocolID
            //возвращает:
            // true - в DEVICES.ASSEMBLYPROTOCOLID есть хотя бы одна ссылка на assemblyProtocolID;
            // false - поле ASSEMBLYPROTOCOLID таблицы DEVICES не ссылается на принятый assemblyProtocolID;
            string sql = @"SELECT COUNT(*) AS NUMROWS
                           FROM DEVICES WITH(NOLOCK)
                           WHERE (ASSEMBLYPROTOCOLID=@AssemblyProtocolID)";

            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                        reader.GetValues(values);

                    count = Convert.ToInt32(values[0]);
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            //если нет ни одной ссылки на принятый assemblyProtocolID - возвращаем false
            //если есть хотя-бы одна ссылка на принятый assemblyProtocolID в таблице DEVICES - возвращаем true
            return (count == 0) ? false : true;
        }

        #endregion

        #region DevParam

        public static bool DoesDeviceHaveParameters(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //проверяет наличие хотя-бы одного места хранения измеренного параметра в таблице DEV_PARAM
            //возвращает:
            // true - таблица DEV_PARAM хранит как минимум одно значение измеренного параметра;
            // false - в таблице DEV_PARAM нет ни одного измеренного параметра;
            string sql = @"SELECT COUNT(*) AS PARAMCOUNT
                           FROM DEV_PARAM WITH(NOLOCK)
                           WHERE (DEV_ID=@DevID)";

            int count = 0;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;

            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                    reader.GetValues(values);

                count = Convert.ToInt32(values[0]);
            }

            finally
            {
                reader.Close();
            }

            switch (count)
            {
                case 0:
                    //нет ни одного места хранения измеренного параметра для принятого изделия devID
                    return false;

                default:
                    //места хранения измеренного параметра для принятого изделия devID в таблице DEV_PARAM существуют
                    return true;
            }
        }

        #endregion

        #region DeviceTypes
        public static bool DeviceTypeByDeviceTypeID(int deviceTypeID, out string deviceTypeRU, out string deviceTypeEN)
        {
            //вычисляет значение DeviceTypes.DeviceTypeRU и DeviceTypeEN по принятому deviceTypeID
            //возвращает:
            //            true  - по принятому deviceTypeID найдена запись со значениями DeviceTypeRU и DeviceTypeEN;
            //            false - по принятому deviceTypeID нет данных

            deviceTypeRU = null;
            deviceTypeEN = null;
            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            string sql = @"SELECT DEVICETYPERU, DEVICETYPEEN
                           FROM DEVICETYPES WITH(NOLOCK)
                           WHERE (DEVICETYPEID=@DeviceTypeID)";

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@DeviceTypeID", SqlDbType.Int).Value = deviceTypeID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    int index;
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        index = reader.GetOrdinal("DEVICETYPERU");
                        deviceTypeRU = Convert.ToString(values[index]);

                        index = reader.GetOrdinal("DeviceTypeEN");
                        deviceTypeEN = Convert.ToString(values[index]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //искомой записи не существует в таблице DEVICETYPES
                    return false;

                case 1:
                    //запись найдена
                    return true;

                default:
                    //считано более одной записи для принятых deviceTypeRU, deviceTypeEN
                    string identificators = string.Format("deviceTypeID='{0}'", deviceTypeID);
                    throw new Exception(string.Format("DeviceTypeDeviceTypeID. ", cReadedRecordNotSingle, count.ToString(), identificators));
            }
        }

        public static int? DeviceTypeID(string deviceTypeRU)
        {
            //чтение идентификатора по строковому значению типа изделия в русском обозначении deviceTypeRU
            //возвращает: 
            // идентификатор записи DeviceTypeID это успешный результат;
            // null - ничего не найдено

            int? deviceTypeID = null;
            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            string sql = @"SELECT DEVICETYPEID
                           FROM DEVICETYPES WITH(NOLOCK)
                           WHERE (DeviceTypeRU=@DeviceTypeRU)";

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@DeviceTypeRU", SqlDbType.NVarChar).Value = deviceTypeRU;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    int index;
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        //идентификатор гарантированно не равен Null
                        index = reader.GetOrdinal("DEVICETYPEID");
                        deviceTypeID = Convert.ToInt32(values[index]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //искомой записи не существует в таблице DEVICETYPES
                    return null;

                case 1:
                    //запись найдена
                    return deviceTypeID;

                default:
                    //считано более одной записи для принятых deviceTypeRU, deviceTypeEN
                    string identificators = string.Format("deviceTypeRU='{0}'", deviceTypeRU);
                    throw new Exception(string.Format("DeviceTypeID. ", cReadedRecordNotSingle, count.ToString(), identificators));
            }
        }

        public static void LoadDeviceTypes(System.Collections.IList listForDataFill)
        {
            //загрузка значениями DEVICETYPE.DEVICETYPEID, DEVICETYPE.DEVICETYPERU, DEVICETYPE.DEVICETYPEEN принятого listForDataFill
            //на момент вызова принятый listForDataFill должен быть созданным
            if (listForDataFill != null)
            {
                listForDataFill.Clear();

                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                string sql = @"SELECT DEVICETYPEID, DEVICETYPERU, DEVICETYPEEN
                               FROM DEVICETYPES WITH(NOLOCK)
                               ORDER BY DEVICETYPERU";

                try
                {
                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        int index;

                        object[] values = new object[reader.FieldCount];

                        while (reader.Read())
                        {
                            reader.GetValues(values);
                            string[] deviceType = new string[3];

                            index = reader.GetOrdinal("DEVICETYPEID");
                            deviceType[0] = values[index].ToString();

                            index = reader.GetOrdinal("DEVICETYPERU");
                            deviceType[1] = values[index].ToString();

                            index = reader.GetOrdinal("DEVICETYPEEN");
                            deviceType[2] = values[index].ToString();

                            listForDataFill.Add(deviceType);
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        #endregion

        #region DeviceReferences

        public static int DeviceReferences(int itav, int deviceTypeID, string constructive, string modification, out int? igtMax, out decimal? ugtMax, out decimal? tgtMax, out int? ubrMin, out int? udsmMin, out int? ursmMin, out decimal? utmMax, out decimal? ufmMax, out int? idrmMax, out int? irrmMax, out int? dUdtMin, out int? prsmMin, out decimal? trrMin, out decimal? tqMin, out int? risolMin, out int? uisolMin, out int? qrrMax, out int? tjMax, out string caseType, out decimal? utmCorrection)
        {
            //чтение одной записи таблицы DEVICEREFERENCES по уникальному сочетанию полей (ITAV, DEVICETYPEID, CONSTRUCTIVE, MODIFICATION)
            //возвращает: 
            // успешный результат: идентификатор записи DEVICEREFERENCEID;
            // -1 значит ничего не найдено

            int deviceReferenceID = -1;
            igtMax = null;
            ugtMax = null;
            tgtMax = null;
            ubrMin = null;
            udsmMin = null;
            ursmMin = null;
            utmMax = null;
            ufmMax = null;
            idrmMax = null;
            irrmMax = null;
            dUdtMin = null;
            prsmMin = null;
            trrMin = null;
            tqMin = null;
            risolMin = null;
            uisolMin = null;
            qrrMax = null;
            tjMax = null;
            caseType = null;
            utmCorrection = null;

            int count = 0;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            object modificationValue = ChangeNullToDBNullValue(modification);

            string sql = string.Concat(@"SELECT DEVICEREFERENCEID, IGTMAX, UGTMAX, TGTMAX, UBRMIN, UDSMMIN, URSMMIN, UTMMAX, UFMMAX, IDRMMAX, IRRMMAX, DUDTMIN, PRSMMIN, TRRMIN, TQMIN, RISOLMIN, UISOLMIN, QRRMAX, TJMAX, CASETYPE, UTMCORRECTION
                                         FROM DEVICEREFERENCES WITH(NOLOCK)
                                         WHERE (ITAV=@Itav) AND
                                               (DEVICETYPEID=@DeviceTypeID) AND
                                               (CONSTRUCTIVE=@Constructive) AND
                                              ", SqlParameterEqualNull("MODIFICATION", "@Modification", modificationValue));

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@Itav", SqlDbType.Int).Value = itav;
                command.Parameters.Add("@DeviceTypeID", SqlDbType.Int).Value = deviceTypeID;
                command.Parameters.Add("@Constructive", SqlDbType.NVarChar).Value = constructive;
                command.Parameters.Add("@Modification", SqlDbType.NVarChar).Value = modificationValue;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    int index;
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        //идентификатор гарантированно не равен Null
                        index = reader.GetOrdinal("DEVICEREFERENCEID");
                        deviceReferenceID = Convert.ToInt32(values[index]);

                        index = reader.GetOrdinal("IGTMAX");
                        igtMax = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("UGTMAX");
                        ugtMax = (decimal?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("TGTMAX");
                        tgtMax = (decimal?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("UBRMIN");
                        ubrMin = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("UDSMMIN");
                        udsmMin = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("URSMMIN");
                        ursmMin = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("UTMMAX");
                        utmMax = (decimal?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("UFMMAX");
                        ufmMax = (decimal?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("IDRMMAX");
                        idrmMax = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("IRRMMAX");
                        irrmMax = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("DUDTMIN");
                        dUdtMin = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("PRSMMIN");
                        prsmMin = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("TRRMIN");
                        trrMin = (decimal?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("TQMIN");
                        tqMin = (decimal?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("RISOLMIN");
                        risolMin = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("UISOLMIN");
                        uisolMin = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("QRRMAX");
                        qrrMax = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("TJMAX");
                        tjMax = (int?)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("CASETYPE");
                        caseType = (string)ChangeDBNullToNullValue(values[index]);

                        index = reader.GetOrdinal("UTMCORRECTION");
                        utmCorrection = (decimal?)ChangeDBNullToNullValue(values[index]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //искомой записи не существует в таблице DEVICEREFERENCES
                    return -1;

                case 1:
                    //запись найдена
                    return deviceReferenceID;

                default:
                    //считано более одной записи для принятых itav, deviceTypeID, constructive, modification
                    string identificators = string.Format("itav={0}, deviceTypeID='{1}', constructive={2}, modification={3}", itav, deviceTypeID, constructive, modification);
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), identificators));
            }
        }

        public static void GetDeviceReferences(DataTable data)
        {
            if (data != null)
            {
                data.Clear();

                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();

                    string sqlText = @"SELECT DR.DEVICEREFERENCEID, DR.ITAV, DR.DEVICETYPEID, DT.DEVICETYPERU, DT.DEVICETYPEEN, DR.CONSTRUCTIVE, DR.MODIFICATION,
                                              DR.IGTMAX, DR.UGTMAX, DR.TGTMAX, DR.UBRMIN, DR.UDSMMIN, DR.URSMMIN, DR.UTMMAX, DR.UFMMAX, DR.IDRMMAX, DR.IRRMMAX, DR.DUDTMIN, DR.PRSMMIN,
                                              DR.TRRMIN, DR.TQMIN, DR.RISOLMIN, DR.UISOLMIN, DR.QRRMAX, DR.TJMAX, DR.CASETYPE, DR.UTMCORRECTION,
                                              DR.USR, DR.TS
                                       FROM DEVICEREFERENCES DR WITH(NOLOCK)
                                        INNER JOIN DEVICETYPES DT WITH(NOLOCK) ON (DR.DEVICETYPEID=DT.DEVICETYPEID)";

                    adapter.SelectCommand = new SqlCommand(sqlText, connection);
                    adapter.Fill(data);
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        public static int InsertToDeviceReferences(int itav, int deviceTypeID, string constructive, string modification, int? igtMax, decimal? ugtMax, decimal? tgtMax, int? ubrMin, int? udsmMin, int? ursmMin, decimal? utmMax, decimal? ufmMax, int? idrmMax, int? irrmMax, int? dUdtMin, int? prsmMin, decimal? trrMin, decimal? tqMin, int? risolMin, int? uisolMin, int? qrrMax, int? tjMax, string caseType, decimal? utmCorrection, string user)
        {
            //вставка новой записи в таблицу DEVICEREFERENCES
            int deviceReferenceID = -1;

            SqlConnection connection = DBConnections.Connection;

            //string sql = @"INSERT INTO DEVICEREFERENCES(ITAV, DEVICETYPEID, CONSTRUCTIVE, MODIFICATION, IDRMMAX, UTMMAX, QRRMAX, IGTMAX, UGTMAX, TJMAX, PRSMMIN, CASETYPE, UTMCORRECTION, USR, TS)
            //               OUTPUT INSERTED.DEVICEREFERENCEID VALUES (@Itav, @DeviceTypeID, @Constructive, @Modification, @IdrmMax, @UtmMax, @QrrMax, @IgtMax, @UgtMax, @TjMax, @PrsmMin, @CaseType, @UtmCorrection, @Usr, GETDATE())";

            string sql = @"INSERT INTO DEVICEREFERENCES (ITAV, DEVICETYPEID, CONSTRUCTIVE, MODIFICATION, IGTMAX, UGTMAX, TGTMAX, UBRMIN, UDSMMIN, URSMMIN, UTMMAX, UFMMAX, IDRMMAX, IRRMMAX, DUDTMIN, PRSMMIN, TRRMIN, TQMIN, RISOLMIN, UISOLMIN, QRRMAX, TJMAX, CASETYPE, UTMCORRECTION, USR, TS)
                           OUTPUT INSERTED.DEVICEREFERENCEID VALUES (@Itav, @DeviceTypeID, @Constructive, @Modification, @IgtMax, @UgtMax, @TgtMax, @UbrMin, @UdsmMin, @UrsmMin, @UtmMax, @UfmMax, @IdrmMax, @IrrmMax, @dUdtMin, @PrsmMin, @TrrMin, @TqMin, @RisolMin, @UisolMin, @QrrMax, @TjMax, @CaseType, @UtmCorrection, @Usr, GETDATE())";

            SqlCommand command = new SqlCommand(sql, connection);

            command.Parameters.Add("@Itav", SqlDbType.Int).Value = itav;
            command.Parameters.Add("@DeviceTypeID", SqlDbType.Int).Value = deviceTypeID;
            command.Parameters.Add("@Constructive", SqlDbType.NVarChar).Value = constructive;
            command.Parameters.Add("@Modification", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(modification);

            command.Parameters.Add("@IgtMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(igtMax);
            command.Parameters.Add("@UgtMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(ugtMax);
            command.Parameters.Add("@TgtMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(tgtMax);
            command.Parameters.Add("@UbrMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(ubrMin);
            command.Parameters.Add("@UdsmMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(udsmMin);
            command.Parameters.Add("@UrsmMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(ursmMin);
            command.Parameters.Add("@UtmMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(utmMax);
            command.Parameters.Add("@UfmMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(ufmMax);
            command.Parameters.Add("@IdrmMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(idrmMax);
            command.Parameters.Add("@IrrmMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(irrmMax);
            command.Parameters.Add("@dUdtMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(dUdtMin);
            command.Parameters.Add("@PrsmMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(prsmMin);
            command.Parameters.Add("@TrrMin", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(trrMin);
            command.Parameters.Add("@TqMin", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(tqMin);
            command.Parameters.Add("@RisolMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(risolMin);
            command.Parameters.Add("@UisolMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(uisolMin);
            command.Parameters.Add("@QrrMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(qrrMax);
            command.Parameters.Add("@TjMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(tjMax);
            command.Parameters.Add("@CaseType", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(caseType);
            command.Parameters.Add("@UtmCorrection", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(utmCorrection);
            command.Parameters.Add("@Usr", SqlDbType.NVarChar).Value = user;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                //считываем идентификатор созданной записи
                deviceReferenceID = (int)command.ExecuteScalar();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return deviceReferenceID;
        }

        private static void UpdateDeviceReferences(int deviceReferenceID, int itav, int deviceTypeID, string constructive, string modification, int? igtMax, decimal? ugtMax, decimal? tgtMax, int? ubrMin, int? udsmMin, int? ursmMin, decimal? utmMax, decimal? ufmMax, int? idrmMax, int? irrmMax, int? dUdtMin, int? prsmMin, decimal? trrMin, decimal? tqMin, int? risolMin, int? uisolMin, int? qrrMax, int? tjMax, string caseType, decimal? utmCorrection, string user)
        {
            //редактирование записи в таблице MANUALINPUTDEVPARAM
            SqlConnection connection = DBConnections.Connection;

            /*
            string sql = @"UPDATE DEVICEREFERENCES
                           SET ITAV=@Itav, DEVICETYPEID=@DeviceTypeID, CONSTRUCTIVE=@Constructive, MODIFICATION=@Modification,
                               IDRMMAX=@IdrmMax, UTMMAX=@UtmMax, QRRMAX=@QrrMax, IGTMAX=@IgtMax, UGTMAX=@UgtMax, TJMAX=@TjMax,
                               PRSMMIN=@PrsmMin, CASETYPE=@CaseType, UTMCORRECTION=@UtmCorrection,
                               USR=@Usr, TS=GETDATE()
                           WHERE (DEVICEREFERENCEID=@DeviceReferenceID)";
            */

            string sql = @"UPDATE DEVICEREFERENCES
                           SET ITAV=@Itav, DEVICETYPEID=@DeviceTypeID, CONSTRUCTIVE=@Constructive, MODIFICATION=@Modification,
                               IGTMAX=@IgtMax, UGTMAX=@UgtMax, TGTMAX=@TgtMax, UBRMIN=@UbrMin, UDSMMIN=@UdsmMin, URSMMIN=@UrsmMin, UTMMAX=@UtmMax, UFMMAX=@UfmMax, IDRMMAX=@IdrmMax, IRRMMAX=@IrrmMax, DUDTMIN=@dUdtMin, PRSMMIN=@PrsmMin,
                               TRRMIN=@TrrMin, TQMIN=@TqMin, RISOLMIN=@RisolMin, UISOLMIN=@UisolMin, QRRMAX=@QrrMax, TJMAX=@TjMax, CASETYPE=@CaseType, UTMCORRECTION=@UtmCorrection,
                               USR=@Usr, TS=GETDATE()
                           WHERE (DEVICEREFERENCEID=@DeviceReferenceID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@DeviceReferenceID", SqlDbType.Int).Value = deviceReferenceID;
            command.Parameters.Add("@Itav", SqlDbType.Int).Value = itav;
            command.Parameters.Add("@DeviceTypeID", SqlDbType.Int).Value = deviceTypeID;
            command.Parameters.Add("@Constructive", SqlDbType.NVarChar).Value = constructive;
            command.Parameters.Add("@Modification", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(modification);

            command.Parameters.Add("@IgtMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(igtMax);
            command.Parameters.Add("@UgtMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(ugtMax);
            command.Parameters.Add("@TgtMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(tgtMax);
            command.Parameters.Add("@UbrMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(ubrMin);
            command.Parameters.Add("@UdsmMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(udsmMin);
            command.Parameters.Add("@UrsmMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(ursmMin);
            command.Parameters.Add("@UtmMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(utmMax);
            command.Parameters.Add("@UfmMax", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(ufmMax);
            command.Parameters.Add("@IdrmMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(idrmMax);
            command.Parameters.Add("@IrrmMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(irrmMax);
            command.Parameters.Add("@dUdtMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(dUdtMin);
            command.Parameters.Add("@PrsmMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(prsmMin);
            command.Parameters.Add("@TrrMin", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(trrMin);
            command.Parameters.Add("@TqMin", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(tqMin);
            command.Parameters.Add("@RisolMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(risolMin);
            command.Parameters.Add("@UisolMin", SqlDbType.Int).Value = ChangeNullToDBNullValue(uisolMin);
            command.Parameters.Add("@QrrMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(qrrMax);
            command.Parameters.Add("@TjMax", SqlDbType.Int).Value = ChangeNullToDBNullValue(tjMax);
            command.Parameters.Add("@CaseType", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(caseType);
            command.Parameters.Add("@UtmCorrection", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(utmCorrection);
            command.Parameters.Add("@Usr", SqlDbType.NVarChar).Value = user;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static int? SaveToDeviceReferences(int? deviceReferenceID, int itav, int deviceTypeID, string constructive, string modification, int? igtMax, decimal? ugtMax, decimal? tgtMax, int? ubrMin, int? udsmMin, int? ursmMin, decimal? utmMax, decimal? ufmMax, int? idrmMax, int? irrmMax, int? dUdtMin, int? prsmMin, decimal? trrMin, decimal? tqMin, int? risolMin, int? uisolMin, int? qrrMax, int? tjMax, string caseType, decimal? utmCorrection, string user)
        {
            //выполняет сохранение записи в справочник норм на изделия
            //если принятый deviceReferenceID=null - выполняется попытка создания новой записи, иначе выполняется редактирование записи
            //возвращает идентификатор созданной записи
            if (deviceReferenceID == null)
            {
                //выполняется попытка создания новой записи
                int createdDeviceReferenceID = InsertToDeviceReferences(itav, deviceTypeID, constructive, modification, igtMax, ugtMax, tgtMax, ubrMin, udsmMin, ursmMin, utmMax, ufmMax, idrmMax, irrmMax, dUdtMin, prsmMin, trrMin, tqMin, risolMin, uisolMin, qrrMax, tjMax, caseType, utmCorrection, user);

                return createdDeviceReferenceID;
            }
            else
            {
                //выполняется попытка редактирования записи
                UpdateDeviceReferences((int)deviceReferenceID, itav, deviceTypeID, constructive, modification, igtMax, ugtMax, tgtMax, ubrMin, udsmMin, ursmMin, utmMax, ufmMax, idrmMax, irrmMax, dUdtMin, prsmMin, trrMin, tqMin, risolMin, uisolMin, qrrMax, tjMax, caseType, utmCorrection, user);

                return null;
            }
        }

        public static void DeleteFromDeviceReferences(int deviceReferenceID)
        {
            //удаление записи из таблицы ManualInputAssemblyProtocolParam
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM DEVICEREFERENCES
                           WHERE (DEVICEREFERENCEID=@DeviceReferenceID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@DeviceReferenceID", SqlDbType.Int).Value = deviceReferenceID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void LoadModificationsFromDeviceReferences(System.Collections.IList listForDataFill)
        {
            //загрузка значениями DEVICEREFERENCES.MODIFICATION принятого listForDataFill
            //на момент вызова принятый listForDataFill должен быть созданным
            if (listForDataFill != null)
            {
                listForDataFill.Clear();

                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                string sql = @"SELECT DISTINCT MODIFICATION
                               FROM DEVICEREFERENCES WITH(NOLOCK)
                               WHERE NOT(MODIFICATION IS NULL)
                               ORDER BY MODIFICATION";

                try
                {
                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        int index;
                        string modification = null;

                        object[] values = new object[reader.FieldCount];

                        while (reader.Read())
                        {
                            reader.GetValues(values);

                            index = reader.GetOrdinal("MODIFICATION");
                            modification = values[index].ToString();

                            listForDataFill.Add(modification);
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        #endregion

        #region DeviceComments

        public static void FillDataTableByDeviceComments(DataTable dt, int[] devIDArray)
        {
            //считываем комментарии и заливаем их в принятый dt
            const string delimeter = ",";

            SqlConnection connection = DBConnections.Connection;

            /*
            string sql = @"SELECT STRING_AGG(C.COMMENTS, '') AS COMMENTS, MAX(C1.DEV_ID) AS DEV_ID, MAX(C1.RECORDDATE) AS RECORDDATE, MAX(DC.ID) AS ID, MAX(DC.USERLOGIN) AS USERLOGIN, CONCAT(MAX(DC.LASTNAME), ' ', CONCAT(SUBSTRING(MAX(DC.FIRSTNAME), 1, 1), '.', SUBSTRING(MAX(DC.MIDDLENAME), 1, 1), '.')) AS FULLUSERNAME
                           FROM
                                (
                                 SELECT DISTINCT COMMENTS,
                                        (
			                             SELECT TOP 1 DEV_ID
                                         FROM DEVICECOMMENTS WITH(NOLOCK)
                                         WHERE (
                                                DEV_ID IN (
			                                               SELECT VALUE
			                                               FROM STRING_SPLIT(@Dev_ID, ',')
							                              )
					                           )
                                         ORDER BY RECORDDATE DESC
			                            ) AS DEV_ID
                                 FROM DEVICECOMMENTS WITH(NOLOCK)
                                 WHERE (DEV_ID IN(SELECT VALUE FROM STRING_SPLIT(@Dev_ID, ',')))  
                                ) C
                            INNER JOIN DEVICECOMMENTS C1 WITH(NOLOCK) ON (C.DEV_ID=C1.DEV_ID)
                            INNER JOIN [SA-011].[SL_PE_DC20002].dbo.RUSDC_USERS DC ON (C1.USERID=DC.ID)";
            */

            string sql = @"SELECT C.DEV_ID, C.RECORDDATE, DC.ID, DC.USERLOGIN, CONCAT(DC.LASTNAME, ' ', CONCAT(SUBSTRING(DC.FIRSTNAME, 1, 1), '.', SUBSTRING(DC.MIDDLENAME, 1, 1), '.')) AS FULLUSERNAME, C.COMMENTS
                           FROM DEVICECOMMENTS C WITH(NOLOCK)
                            INNER JOIN [SA-011].[SL_PE_DC20002].dbo.RUSDC_USERS DC WITH(NOLOCK) ON (C.USERID=DC.ID)
                           WHERE (C.DEV_ID IN(SELECT VALUE FROM STRING_SPLIT(@Dev_ID, @Delimeter)))  
                           ORDER BY C.RECORDDATE";

            SqlDataAdapter adapter = new SqlDataAdapter
            {
                SelectCommand = new SqlCommand(sql, connection)
            };

            adapter.SelectCommand.Parameters.Add("@Dev_ID", SqlDbType.NVarChar).Value = string.Join(delimeter, devIDArray);
            adapter.SelectCommand.Parameters.Add("@Delimeter", SqlDbType.NVarChar).Value = delimeter;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                adapter.Fill(dt);
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void InsertToDeviceComments(int dev_ID, long userID, string comments)
        {
            //вставка новой записи в таблицу DEVICECOMMENTS
            if ((!string.IsNullOrEmpty(comments)) && (comments.Trim() != string.Empty))
            {
                SqlConnection connection = DBConnections.Connection;

                string sql = @"INSERT INTO DEVICECOMMENTS(DEV_ID, USERID, RECORDDATE, COMMENTS)
                               VALUES (@Dev_ID, @UserID, GETDATE(), @Comments)";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = dev_ID;
                command.Parameters.Add("@UserID", SqlDbType.BigInt).Value = userID;
                command.Parameters.Add("@Comments", SqlDbType.NVarChar).Value = comments.Trim();

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    command.ExecuteNonQuery();
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        private static void UpdateDeviceComments(int dev_ID, long userID, string comments)
        {
            //редактирование комментария в таблице DEVICECOMMENTS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE DEVICECOMMENTS
                           SET USERID=@UserID,
                               RECORDDATE=GETDATE(),
                               COMMENTS=@Comments
                           WHERE (DEV_ID=@Dev_ID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = dev_ID;
            command.Parameters.Add("@UserID", SqlDbType.BigInt).Value = userID;
            //поле DeviceComments.COMMENTS объявлено NOT NULL
            command.Parameters.Add("@Comments", SqlDbType.NVarChar).Value = string.IsNullOrEmpty(comments) ? string.Empty : comments.Trim();

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        private static void DeleteDeviceComments(int dev_ID)
        {
            //удаление комментария в таблице DEVICECOMMENTS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM DEVICECOMMENTS
                           WHERE (DEV_ID=@Dev_ID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = dev_ID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static bool IsExistsDeviceComments(int devID)
        {
            //проверяет использование изделия devID в таблице DeviceComments
            //возвращает:
            // true - таблица DeviceComments использует изделие devID;
            // false - таблица DeviceComments не использует изделие devID;
            SqlConnection connection = DBConnections.Connection;

            string sql = @"SELECT COUNT(*) AS NUMROWS
                           FROM DEVICECOMMENTS WITH(NOLOCK)
                           WHERE (DEV_ID=@DevID)";

            int count = 0;

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;

            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                    reader.GetValues(values);

                count = Convert.ToInt32(values[0]);
            }

            finally
            {
                reader.Close();
            }

            switch (count)
            {
                case 0:
                    //для изделия devID не написано ни одного комментария пользователя
                    return false;

                default:
                    //для изделия devID написан как минимум один комментарий
                    return true;
            }
        }

        public static void SaveToDeviceComment(int devID, long userID, string comments)
        {
            bool commentExists = IsExistsDeviceComments(devID);

            if (commentExists)
            {
                switch (string.IsNullOrEmpty(comments))
                {
                    case true:
                        //пользователь написал пустой комментарий - не храним пустые комментарии
                        DeleteDeviceComments(devID);
                        break;

                    default:
                        //выполняется редактирование уже существующего комментария
                        UpdateDeviceComments(devID, userID, comments);
                        break;
                }
            }
            else
            {
                //выполняется создание комментария
                InsertToDeviceComments(devID, userID, comments);
            }
        }

        public static bool IsDeviceCommentsUseDevice(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //проверяет использование изделия devID в таблице DeviceComments
            //возвращает:
            // true - таблица DeviceComments использует изделие devID;
            // false - таблица DeviceComments не использует изделие devID;
            string sql = @"SELECT COUNT(*) AS NUMROWS
                           FROM DEVICECOMMENTS WITH(NOLOCK)
                           WHERE (DEV_ID=@DevID)";

            int count = 0;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;

            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                    reader.GetValues(values);

                count = Convert.ToInt32(values[0]);
            }

            finally
            {
                reader.Close();
            }

            switch (count)
            {
                case 0:
                    //для изделия devID не написано ни одного комментария пользователя
                    return false;

                default:
                    //для изделия devID написан как минимум один комментарий
                    return true;
            }
        }

        #endregion

        #region Assemblys
        public static int CreateAssembly(string groupName, string deviceCode, string deviceCode2, string packageSerialNum, string usr)
        {
            //создание связи ППЭ-корпус
            int result;
            SqlConnection connection = DBConnections.Connection;

            SqlCommand command = new SqlCommand("CreateAssembly", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@Group_Name", SqlDbType.NChar).Value = groupName;
            command.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = deviceCode;

            SqlParameter param = command.Parameters.Add("@DeviceCode2", SqlDbType.NVarChar);
            if ((deviceCode2 == null) || (deviceCode2 == string.Empty))
                param.Value = DBNull.Value;
            else param.Value = deviceCode2;

            command.Parameters.Add("@PackageSerialNum", SqlDbType.NVarChar).Value = packageSerialNum;
            command.Parameters.Add("@Usr", SqlDbType.NVarChar).Value = usr;

            SqlParameter outputParameter = command.Parameters.Add("@PackageID", SqlDbType.Int);
            outputParameter.Direction = ParameterDirection.Output;

            connection.Open();

            try
            {
                command.ExecuteNonQuery();

                result = (int)outputParameter.Value;
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return result;
        }

        public static int UpdateAssembly(string oldPackageSerialNum, string packageSerialNum, string groupName, string deviceCode, string deviceCode2, string usr)
        {
            //редактирование связи корпус - ППЭ/ППЭ2 таблице ASSEMBLYS
            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            SqlCommand command = new SqlCommand("UpdateAssembly", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@OldPackageSerialNum", SqlDbType.NVarChar).Value = oldPackageSerialNum;
            command.Parameters.Add("@PackageSerialNum", SqlDbType.NVarChar).Value = packageSerialNum;
            command.Parameters.Add("@Group_Name", SqlDbType.NChar).Value = groupName;
            command.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = deviceCode;

            SqlParameter param = command.Parameters.Add("@DeviceCode2", SqlDbType.NVarChar);
            if ((deviceCode2 == null) || (deviceCode2 == string.Empty))
                param.Value = DBNull.Value;
            else param.Value = deviceCode2;

            command.Parameters.Add("@Usr", SqlDbType.NVarChar).Value = usr;

            SqlParameter packageID = command.Parameters.Add("@PackageID", SqlDbType.Int);
            packageID.Direction = ParameterDirection.Output;

            connection.Open();

            try
            {
                command.ExecuteNonQuery();
                result = (packageID.Value == DBNull.Value) ? -1 : (int)packageID.Value;
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return result;
        }

        public static int Relabeling(string groupName, string oldSerialNum, string newSerialNum, string usr, out int packageID)
        {
            //перемаркировка - изменение кода корпуса. было oldDeviceCode, стало newDeviceCode
            //Возвращает:
            //          -1 - ошибка в данной реализации;
            //           0 - замена старого серийного номера корпуса успешно произведена на новый серийный номер корпуса;
            //           1 - новый серийный номер корпуса @NewSerialNum уже используется - не корректен;
            //           2 - старый серийный номер корпуса @OldSerialNum не найден
            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            SqlCommand command = new SqlCommand("Relabeling", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@GroupName", SqlDbType.NVarChar).Value = groupName;
            command.Parameters.Add("@OldSerialNum", SqlDbType.NVarChar).Value = oldSerialNum;
            command.Parameters.Add("@NewSerialNum", SqlDbType.NVarChar).Value = newSerialNum;
            command.Parameters.Add("@Usr", SqlDbType.NVarChar).Value = usr;

            SqlParameter PackageID = command.Parameters.Add("@PackageID", SqlDbType.Int);
            PackageID.Direction = System.Data.ParameterDirection.Output;

            SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
            returnValue.Direction = System.Data.ParameterDirection.ReturnValue;
            connection.Open();

            try
            {
                command.ExecuteNonQuery();

                result = (int)returnValue.Value;
                packageID = PackageID.Value == DBNull.Value ? -1 : (int)PackageID.Value;
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return result;
        }

        public static bool IsAssemblysUseDevice(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //проверяет использование изделия devID в таблице Assemblys
            //возвращает:
            // true - таблица Assemblys использует изделие devID;
            // false - таблица Assemblys не использует изделие devID;
            string sql = @"SELECT COUNT(*) AS NUMROWS
                           FROM ASSEMBLYS WITH(NOLOCK)
                           WHERE (
                                  (DEV_ID=@DevID) OR 
                                  (DEV_ID2=@DevID)
                                 )";

            int count = 0;


            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;

            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                    reader.GetValues(values);

                count = Convert.ToInt32(values[0]);
            }
            finally
            {
                reader.Close();
            }

            switch (count)
            {
                case 0:
                    //изделие devID не используется ни в одной сборке
                    return false;

                default:
                    //изделие devID используется как минимум в одной сборке
                    return true;
            }
        }

        public static bool IsAssemblyExists(string serialNum)
        {
            //корпус можно собрать только один раз
            int count = 0;
            SqlConnection connection = DBConnections.Connection;

            connection.Open();

            try
            {
                //смотрим есть ли хоть одна сборка с серийным номером serialNum
                string sql = "SELECT COUNT(A.PACKAGEID) AS NUMROWS" +
                             " FROM ASSEMBLYS A WITH(NOLOCK)" +
                             "  INNER JOIN PACKAGES P WITH(NOLOCK) ON (" +
                             "                                         (A.PACKAGEID=P.PACKAGEID) AND" +
                             "                                         (P.SERIALNUM=@SerialNum)" +
                             "                                        )";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@SerialNum", SqlDbType.NVarChar).Value = serialNum;
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                        count = int.Parse(reader["NUMROWS"].ToString());
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return (count != 0);
        }

        public static bool IsDeviceCodeUsed(string deviceCode)
        {
            //любой ППЭ может иметь только одну связь со своим корпусом. нельзя вставить один и тот же ППЭ в более чем один корпус
            int totalCount = 0;
            SqlConnection connection = DBConnections.Connection;

            connection.Open();

            try
            {
                //смотрим сколько ППЭ с обозначением deviceCode, прошедших через КИП СПП использовано в качестве единственного элемента в корпусе
                string sql = "SELECT COUNT(A.DEV_ID) AS NUMROWS" +
                             " FROM ASSEMBLYS A WITH(NOLOCK)" +
                             "  INNER JOIN DEVICES D WITH(NOLOCK) ON (" +
                             "                                         (A.DEV_ID=D.DEV_ID) AND" +
                             "                                         (D.CODE=@DeviceCode)" +
                             "                                       )" +
                             " WHERE NOT(A.DEV_ID=1)";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = deviceCode;
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                        totalCount = int.Parse(reader["NUMROWS"].ToString());
                }

                finally
                {
                    reader.Close();
                }

                //смотрим сколько ППЭ с обозначением deviceCode, прошедших через КИП СПП использовано в качестве второго элемента в корпусе
                sql = "SELECT COUNT(A.DEV_ID2) AS NUMROWS" +
                      " FROM ASSEMBLYS A WITH(NOLOCK)" +
                      "  INNER JOIN DEVICES D WITH(NOLOCK) ON (" +
                      "                                         (A.DEV_ID2=D.DEV_ID) AND" +
                      "                                         (D.CODE=@DeviceCode)" +
                      "                                       )" +
                      " WHERE NOT(A.DEV_ID2=1)";

                command = new SqlCommand(sql, connection);
                command.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = deviceCode;
                reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                        totalCount += int.Parse(reader["NUMROWS"].ToString());
                }

                finally
                {
                    reader.Close();
                }

                //смотрим сколько ППЭ с обозначением deviceCode, не прошедших через КИП СПП использовано в качестве единственного или второго элемента в корпусе
                sql = "SELECT COUNT(*) AS NUMROWS" +
                      " FROM ASSEMBLYS WITH(NOLOCK)" +
                      " WHERE ((DEVICECODE=@DeviceCode) OR (DEVICECODE2=@DeviceCode))";

                command = new SqlCommand(sql, connection);
                command.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = deviceCode;
                reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                        totalCount += int.Parse(reader["NUMROWS"].ToString());
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return (totalCount != 0);
        }

        public class RecordOfAssembly : INotifyPropertyChanged
        {
            //корпус
            private string packageSerialNum;
            public string PackageSerialNum
            {
                get
                {
                    return packageSerialNum;
                }

                set
                {
                    packageSerialNum = value;
                    OnPropertyChanged("PackageSerialNum");
                }
            }

            //первый ППЭ (заполнен всегда)
            private string deviceCode;
            public string DeviceCode
            {
                get
                {
                    return deviceCode;
                }

                set
                {
                    deviceCode = value;
                    OnPropertyChanged("DeviceCode");
                }
            }

            //второй ППЭ (заполнен если применяемость ППЭ равна 2)
            private string deviceCode2;
            public string DeviceCode2
            {
                get
                {
                    return deviceCode2;
                }

                set
                {
                    deviceCode2 = value;
                    OnPropertyChanged("DeviceCode2");
                }
            }

            //отметка времени о выполненной сборке изделия
            private DateTime tS;
            public DateTime TS
            {
                get
                {
                    return tS;
                }

                set
                {
                    tS = value;
                    OnPropertyChanged("TS");
                }
            }

            //сборщик
            private string usr;
            public string Usr
            {
                get
                {
                    return usr;
                }

                set
                {
                    usr = value;
                    OnPropertyChanged("Usr");
                }
            }

            //старый серийный номер изделия/корпуса до выполнения перемаркировки
            private string oldSerialNum;
            public string OldSerialNum
            {
                get
                {
                    return oldSerialNum;
                }

                set
                {
                    oldSerialNum = value;
                    OnPropertyChanged("OldSerialNum");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(string prop = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }

            public string ValueByIndex(int index)
            {
                switch (index)
                {
                    case 0:
                        return this.PackageSerialNum;

                    case 1:
                        return this.DeviceCode;

                    case 2:
                        return this.DeviceCode2;

                    case 3:
                        return this.TS.ToString("dd.MM.yyyy HH:mm:ss");

                    case 4:
                        return Usr;

                    case 5:
                        return OldSerialNum;

                    default:
                        return string.Empty;
                }
            }
        }

        public static void LoadFromAssemblyToList(ObservableCollection<RecordOfAssembly> recordOfAssemblyList, string groupName)
        {
            if (recordOfAssemblyList == null)
                throw new Exception("recordOfAssemblyList=null. Waiting not null recordOfAssemblyList.");

            string sql = @"SELECT P.SERIALNUM, ASS.DEVICECODE, ASS.DEVICECODE2, ASS.TS, D.CODE, D2.CODE AS CODE2, P2.SERIALNUM AS OLDSERIALNUM, DCU.LASTNAME, DCU.FIRSTNAME, DCU.MIDDLENAME
                           FROM ASSEMBLYS ASS WITH(NOLOCK)
                            LEFT JOIN DEVICES D WITH(NOLOCK) ON (ASS.DEV_ID=D.DEV_ID)
                            LEFT JOIN DEVICES D2 WITH(NOLOCK) ON (ASS.DEV_ID2=D.DEV_ID)
                            INNER JOIN PACKAGES P WITH(NOLOCK) ON (ASS.PACKAGEID=P.PACKAGEID)
                            LEFT JOIN PACKAGES P2 WITH(NOLOCK) ON (ASS.OLDPACKAGEID=P2.PACKAGEID)
                            INNER JOIN [sa-011].[SL_PE_DC20002].[dbo].[RUSDC_Users] DCU ON (ASS.USR=DCU.USERLOGIN)
                           WHERE (ASS.GROUP_NAME=@GroupName)
                           ORDER BY ASS.TS DESC";

            SqlConnection connection = DBConnections.Connection;
            connection.Open();

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@GroupName", SqlDbType.NChar).Value = groupName;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        string deviceCode = (reader["DEVICECODE"] == DBNull.Value) ? reader["CODE"].ToString() : reader["DEVICECODE"].ToString();
                        string deviceCode2 = (reader["DEVICECODE2"] == DBNull.Value) ? reader["CODE2"].ToString() : reader["DEVICECODE2"].ToString();

                        RecordOfAssembly recordOfAssembly = new RecordOfAssembly()
                        {
                            PackageSerialNum = reader["SERIALNUM"].ToString(),
                            DeviceCode = deviceCode,
                            DeviceCode2 = deviceCode2,
                            TS = (DateTime)reader["TS"],
                            OldSerialNum = reader["OLDSERIALNUM"].ToString(),
                            Usr = string.Concat(reader["LASTNAME"].ToString(), " ", reader["FIRSTNAME"].ToString().Substring(0, 1), ".", reader["MIDDLENAME"].ToString().Substring(0, 1), ".")
                        };

                        recordOfAssemblyList.Add(recordOfAssembly);
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        public static bool LoadSingleRecordFromAssembly(RecordOfAssembly recordOfAssembly, int packageID)
        {
            //загрузка одной единственной записи, идентифицируемой packageID - идентификатор корпуса в принимаемый recordOfAssembly
            //возвращает:
            //           true - связь корпуса packageID с ППЭ успешно считана (единственная связь);
            //           false - связь корпуса packageID с ППЭ не удалось считать
            if (recordOfAssembly == null)
                throw new Exception("recordOfAssembly=null. Waiting not null recordOfAssembly.");

            int count = 0;

            string sql = "SELECT P.SERIALNUM, ASS.DEVICECODE, ASS.DEVICECODE2, D.CODE, D2.CODE AS CODE2, ASS.TS, P2.SERIALNUM AS OLDSERIALNUM, DCU.LASTNAME, DCU.FIRSTNAME, DCU.MIDDLENAME" +
                         " FROM ASSEMBLYS ASS WITH(NOLOCK)" +
                         "  LEFT JOIN DEVICES D WITH(NOLOCK) ON (ASS.DEV_ID=D.DEV_ID)" +
                         "  LEFT JOIN DEVICES D2 WITH(NOLOCK) ON (ASS.DEV_ID2=D.DEV_ID)" +
                         "  INNER JOIN PACKAGES P WITH(NOLOCK) ON (ASS.PACKAGEID=P.PACKAGEID)" +
                         "  LEFT JOIN PACKAGES P2 WITH(NOLOCK) ON (ASS.OLDPACKAGEID=P2.PackageID)" +
                         "  INNER JOIN [sa-011].[SL_PE_DC20002].[dbo].[RUSDC_Users] DCU WITH(NOLOCK) ON (ASS.USR=DCU.USERLOGIN)" +
                         " WHERE (P.PACKAGEID=@PackageID)";

            SqlConnection connection = DBConnections.Connection;
            connection.Open();

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@PackageID", SqlDbType.Int).Value = packageID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        string deviceCode = (reader["DEVICECODE"] == DBNull.Value) ? reader["CODE"].ToString() : reader["DEVICECODE"].ToString();
                        string deviceCode2 = (reader["DEVICECODE2"] == DBNull.Value) ? reader["CODE2"].ToString() : reader["DEVICECODE2"].ToString();

                        recordOfAssembly.PackageSerialNum = reader["SERIALNUM"].ToString();
                        recordOfAssembly.DeviceCode = deviceCode;
                        recordOfAssembly.DeviceCode2 = deviceCode2;
                        recordOfAssembly.TS = (DateTime)reader["TS"];
                        recordOfAssembly.OldSerialNum = reader["OLDSERIALNUM"].ToString();
                        recordOfAssembly.Usr = string.Concat(reader["LASTNAME"].ToString(), " ", reader["FIRSTNAME"].ToString().Substring(0, 1), ".", reader["MIDDLENAME"].ToString().Substring(0, 1), ".");

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            return (count == 1);
        }

        #endregion

        #region Linker check data in SyteLine

        public static bool JobExist(string job, short suffix)
        {
            //проверяет наличие ПЗ с обозначением job и суффикс suffix в системе SL
            //возвращает:
            //true  - ПЗ зарегистрировано в системе SL;
            //false - ПЗ не зарегистрировано в системе SL

            long recordCount = 0;
            int count = 0;

            SqlConnection connection = DBConnections.ConnectionSL;

            connection.Open();

            try
            {
                string sql = @"SELECT COUNT(*)
                               FROM JOB_MST WITH(NOLOCK)
                               WHERE (
                                      (JOB=@Job) AND
                                      (SUFFIX=@Suffix) AND
                                      (TYPE='J') AND
                                      (SITE_REF='PE')
                                     )";

                SqlCommand command = new SqlCommand(sql, connection);

                command.Parameters.Add("@Job", SqlDbType.NVarChar).Value = job;
                command.Parameters.Add("@Suffix", SqlDbType.SmallInt).Value = suffix;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        recordCount = Convert.ToInt32(values[0]);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 1:
                    return (recordCount == 0) ? false : true;

                default:
                    //считано более одной записи
                    throw new Exception(string.Concat("JobExist: ", string.Format(cReadedRecordNotSingle, count.ToString())));
            }
        }

        public static double QtyReleased(string groupName)
        {
            //чтение количества запущенных изделий из системы SL по принятому ПЗ groupName
            //возвращает:
            //-1  - groupName не зарегистрирована в системе SL;
            //число - запущенное количество изделий по принятому ПЗ

            //вырезаем из принятого groupName значение суффикса
            string job = groupName.Substring(0, 10);
            string suffix = groupName.Substring(11, 4);

            double result = -1;
            int count = 0;

            SqlConnection connection = DBConnections.ConnectionSL;

            connection.Open();

            try
            {
                string sql = "SELECT QTY_RELEASED" +
                             " FROM JOB_MST WITH(NOLOCK)" +
                             " WHERE (" +
                             "        (JOB=@Job) AND" +
                             "        (SUFFIX=@Suffix) AND" +
                             "        (TYPE='J') AND" +
                             "        (SITE_REF='PE')" +
                             "       )";

                SqlCommand command = new SqlCommand(sql, connection);

                command.Parameters.Add("@Job", SqlDbType.NVarChar).Value = job;
                command.Parameters.Add("@Suffix", SqlDbType.SmallInt).Value = int.Parse(suffix);

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        result = double.Parse(values[0].ToString());

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //принятый ПЗ не имеет регистрации в системе SL
                    return -1;

                case 1:
                    //ПЗ найден, информация успешно считана
                    return result;

                default:
                    //считано более одной записи для принятого groupName
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), "groupName={0}", groupName));
            }
        }

        public static string ItemByGroupName(string groupName, out string description)
        {
            //вычисление кода изделия, который рождается в результате исполнения над ним принятого groupName
            //возвращает:
            //null  - groupName не зарегистрирована в системе SL;
            //строка - код изделия, который рождается в результате исполнения над ним принятого groupName
            //вырезаем из принятого groupName значение суффикса
            string job = groupName.Substring(0, 10);
            string suffix = groupName.Substring(11, 4);
            int count;

            string result = null;
            description = null;

            SqlConnection connection = DBConnections.ConnectionSL;

            connection.Open();

            try
            {
                string sql = @"SELECT ITEM, DESCRIPTION
                               FROM JOB_MST WITH(NOLOCK)
                               WHERE (
                                      (JOB=@Job) AND
                                      (SUFFIX=@Suffix) AND
                                      (TYPE='J') AND
                                      (SITE_REF='PE')
                                     )";

                SqlCommand command = new SqlCommand(sql, connection);

                command.Parameters.Add("@Job", SqlDbType.NVarChar).Value = job;
                command.Parameters.Add("@Suffix", SqlDbType.SmallInt).Value = int.Parse(suffix);

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    count = 0;
                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        result = values[0].ToString();
                        description = values[1].ToString();

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //принятый ПЗ не имеет регистрации в системе SL
                    description = null;

                    return null;

                case 1:
                    //ПЗ найден, информация успешно считана
                    return result;

                default:
                    //считано более одной записи для принятого groupName
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), "groupName={0}", groupName));
            }
        }

        /*
        public static string ItemByGroupName(string groupName)
        {
            //вычисляет обозначение Item изделия, которое обрабатывается по ПЗ с обозначением groupName
            //если по принятому groupName обрабатывается список изделий - возвращает первое встретившееся обозначение из этого списка
            string result = null;

            if (groupName != null)
            {
                SqlConnection connection = DBConnections.ConnectionSL;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    string sql = @"SELECT ITEM
                                   FROM JOB_MST WITH(NOLOCK)
                                   WHERE(
                                         (TYPE='J') AND
                                         (STAT='C') AND
                                         (JOB=@Job)
                                        )";

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@Job", SqlDbType.NVarChar).Value = string.Concat("'", groupName, "-%'");
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        object[] values = new object[reader.FieldCount];

                        while (reader.Read())
                        {
                            reader.GetValues(values);

                            if (result == null)
                            {
                                result = values[0].ToString();
                                break;
                            }
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }

            return result;
        }
        */

        public static string ItemByAssemblyJob(string assemblyJob)
        {
            //вычисление кода изделия, который рождается в результате исполнения над ним принятого assemblyJob
            //суффикс сборочного ПЗ на входе отсутствует, но конечное изделие, которое рождается в результате сборки надо искать только по суффиксу 0 (это правило действует примерно с 2019 года)
            //возвращает:
            //null  - assemblyJob не зарегистрирована в системе SL;
            //строка - код изделия, который рождается в результате исполнения над ним принятого assemblyJob
            //вырезаем из принятого groupName значение суффикса
            int count;

            string result = null;

            SqlConnection connection = DBConnections.ConnectionSL;

            connection.Open();

            try
            {
                string sql = @"SELECT ITEM
                               FROM JOB_MST WITH(NOLOCK)
                               WHERE (
                                      (JOB=@Job) AND
                                      (SUFFIX=0) AND
                                      (TYPE='J') AND
                                      (SITE_REF='PE')
                                     )";

                SqlCommand command = new SqlCommand(sql, connection);

                command.Parameters.Add("@Job", SqlDbType.NVarChar).Value = assemblyJob;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    count = 0;
                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        result = values[0].ToString();

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //принятый ПЗ не имеет регистрации в системе SL
                    return null;

                case 1:
                    //место хранения  найдено, информация успешно считана
                    return result;

                default:
                    //считано более одной записи для принятого groupName
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), "assemblyJob={0}", assemblyJob));
            }
        }

        public static string JobFromItemStructure(string item, out string suffix)
        {
            //чтение кода ПЗ и суффикса из текущей структуры изделия по принятому item
            //возвращает:
            //null  - по принятому item не найден код ПЗ в текущих структурах изделий;
            //строка - обозначение найденного кода ПЗ
            string result = null;
            string suff = null;

            int count = 0;
            SqlConnection connection = DBConnections.ConnectionSL;

            connection.Open();

            try
            {
                string sql = "SELECT JOB, SUFFIX" +
                             " FROM JOB_MST WITH(NOLOCK)" +
                             " WHERE (" +
                             "        (ITEM=@Item) AND" +
                             "        (TYPE='S') AND" +
                             "        (STAT='F') AND" +
                             "        (SITE_REF='PE')" +
                             "       )";

                SqlCommand command = new SqlCommand(sql, connection);

                command.Parameters.Add("@Item", SqlDbType.NVarChar).Value = item;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        result = values[0].ToString();
                        suff = values[1].ToString();

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //принятый item не имеет утверждённой структуры изделия в системе SL
                    suffix = null;

                    return null;

                case 1:
                    suffix = suff;

                    return result;

                default:
                    //найдено более одного описания утверждённой текущей структуры изделия
                    throw new Exception(string.Concat("Current product structure. ", string.Format(cReadedRecordNotSingle, count.ToString(), "item={0}", item)));
            }
        }

        public static bool AmountOfElements(string job, string suffix, out int amountOfElements)
        {
            //вычисление числа ППЭ, устанавливаемых в корпус
            //job - обозначение ПЗ (текущая структура изделия);
            //suffix - обозначение суффикса (текущая структура изделия)
            //возвращает:
            //false  - по принятым данным в системе SL ничего не найдено;
            //число - число ППЭ, устанавливаемых в корпус. элементов в одном корпусе может быть максимум два

            amountOfElements = -1;

            int count = 0;
            SqlConnection connection = DBConnections.ConnectionSL;

            connection.Open();

            try
            {
                string sql = "SELECT SUM(MATL_QTY_CONV) AS AMOUNTOFELEMENTS" +
                             " FROM JOBMATL_MST WITH(NOLOCK)" +
                             " WHERE (" +
                             "        (JOB=@Job) AND" +
                             "        (SUFFIX=@Suffix) AND" +
                             "        (RUS_BASIC_MATERIAL=1) AND" +
                             "        (SITE_REF='PE')" +
                             "       )";

                SqlCommand command = new SqlCommand(sql, connection);

                command.Parameters.Add("@Job", SqlDbType.NVarChar).Value = job;
                command.Parameters.Add("@Suffix", SqlDbType.SmallInt).Value = int.Parse(suffix);

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        if (values[0] == DBNull.Value)
                        {
                            return false;
                        }
                        else
                        {
                            amountOfElements = Convert.ToInt32(values[0]);
                        }

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //в системе SL ничего не найдено
                    return false;

                case 1:
                    return true;

                default:
                    //найдено более одного описания утверждённой текущей структуры изделия
                    throw new Exception(string.Concat("Current product structure. ", string.Format(cReadedRecordNotSingle, count.ToString()), string.Format(" Job={0}, Suffix={1}.", job, suffix)));
            }
        }

        public static bool IsLotIssuedToGroupName(string lot, string groupName)
        {
            //проверяет списана ли партия lot в groupName. проверяет равна ли нулю сумма по транзакциям I + W
            int qty = 0;

            if ((lot != null) && (lot.Trim() != string.Empty))
            {
                //SL не даст провести транзакцию I если списать нечего
                //вырезаем из принятого groupName значение суффикса
                string job = groupName.Substring(0, 10);
                string suffix = groupName.Substring(11, 4);

                string sLot = string.Concat(lot.ToString(), "%");

                SqlConnection connection = DBConnections.ConnectionSL;

                connection.Open();

                try
                {
                    string sql = "SELECT SUM(QTY)" +
                                 " FROM MATLTRAN_MST WITH(NOLOCK)" +
                                 " WHERE (" +
                                 "        (REF_NUM=@RefNum) AND" +
                                 "        (REF_LINE_SUF=@RefLineSuf) AND" +
                                 "        (LTRIM(LOT) LIKE @Lot) AND" +
                                 "        ((TRANS_TYPE='I') OR (TRANS_TYPE='W'))" +
                                 "       )";

                    SqlCommand command = new SqlCommand(sql, connection);

                    command.Parameters.Add("@RefNum", SqlDbType.NVarChar).Value = job;
                    command.Parameters.Add("@RefLineSuf", SqlDbType.SmallInt).Value = int.Parse(suffix);
                    command.Parameters.Add("@Lot", SqlDbType.NVarChar).Value = sLot;

                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        object[] values = new object[reader.FieldCount];

                        //всегда будет считана одна единственная запись
                        while (reader.Read())
                        {
                            reader.GetValues(values);

                            if (values[0] == DBNull.Value)
                            {
                                return false;
                            }
                            else
                            {
                                qty = Convert.ToInt32(values[0]);
                            }
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
            }

            switch (qty)
            {
                case 0:
                    //в системе SL ничего не найдено
                    return false;

                default:
                    //значение qty не ноль - значит партия lot связана с groupName
                    return true;
            }

            /*
            //данная реализация строит список всех партий прямо или косвенно приходящих на обработку принятому ПЗ groupName, и ищет в построенном списке партий принятую партию lot
            //поиск партии выполняется после отбрасывания суффикса партии и порядкового номера изделия в партии
            //если принятая партия lot есть в построенном списке - возвращает true
            //если принятой партии lot нет в построенном списке - возвращает false

            List<int> list = new List<int>();

            SqlConnection connection = DBConnections.ConnectionSL;

            connection.Open();

            try
            {
                SqlCommand command = new SqlCommand("PE_0805_LotsByJob", connection);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add("@Job", SqlDbType.NVarChar).Value = job;
                command.Parameters.Add("@Suffix", SqlDbType.SmallInt).Value = int.Parse(suffix);

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    const char delimeter = '-';
                    string readedLot = null;

                    while (reader.Read())
                    {
                        readedLot = reader["LOT"].ToString();

                        //выбрасываем суффикс и порядковый номер изделия в партии из считанного обозначения
                        //пример: партия 4-00020941-7-01 после выбрасывания лишнего для поиска будет 4-00020941
                        int index = readedLot.IndexOf(delimeter);

                        if (index != -1)
                        {
                            //выбрасываем префикс
                            readedLot = readedLot.Substring(index + 1);

                            //выбрасываем всё, что стоит за номером партии
                            index = readedLot.IndexOf(delimeter);

                            if (index != -1)
                                readedLot = readedLot.Substring(0, index);
                        }

                        int iReadedLot;
                        if (int.TryParse(readedLot, out iReadedLot))
                            list.Add(iReadedLot);
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }

            //ищем в list партию lot
            return (list.IndexOf(lot) != -1);
            */
        }

        #endregion

        #region Prinet

        public static void IntervalsOfSerialNumsByGroupName(string groupName, List<Interval<int>> listOfIntervals)
        {
            //извлекает описание интервалов серийных номеров по коду принятого сборочного ПЗ groupName
            if (listOfIntervals == null)
                return;

            SqlConnection connection = DBConnections.ConnectionPRINET;

            connection.Open();

            try
            {
                string sql = "SELECT HMSNS, HMSNE" +
                             " FROM HISTMTBL WITH(NOLOCK)" +
                             string.Format(" WHERE (HMZNP='{0}')", groupName);

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    listOfIntervals.Clear();

                    int strartSerialNum;
                    int endSerialNum;

                    while (reader.Read())
                    {
                        strartSerialNum = int.Parse(reader["HMSNS"].ToString());
                        endSerialNum = int.Parse(reader["HMSNE"].ToString());

                        Interval<int> interval = new Interval<int>(strartSerialNum, endSerialNum);
                        listOfIntervals.Add(interval);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        #endregion

        public static int? QtyReleasedByGroupName(string groupName)
        {
            //возвращает количество изделий, запущенных по ПЗ groupName
            if (groupName != null)
            {
                SqlConnection connection = DBConnections.Connection;

                SqlCommand selectCommand = new SqlCommand("SELECT dbo.SL_Qty_ReleasedByJob(@Job)", connection);
                selectCommand.Parameters.Add("@Job", SqlDbType.NVarChar).Value = groupName;

                object res = null;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    res = selectCommand.ExecuteScalar();
                }
                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }

                return (res == DBNull.Value) ? null : (int?)res;
            }

            return null;
        }

        public static int? GroupIDByGroupName(string groupName)
        {
            //вычисление идентификатора GROUPS.GROUP_ID по GROUPS.GROUP_NAME
            //возвращает:
            //            null - по принятому groupName не удалось вычислить идентификтор GROUPS.GROUP_ID (возможно принято groupName=null);
            //            идентификтор GROUPS.GROUP_ID
            int? groupID = null;

            if (groupName != null)
            {
                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    string sql = @"SELECT GROUP_ID
                                   FROM GROUPS WITH(NOLOCK)
                                   WHERE (GROUP_NAME=@GroupName)";

                    int count = 0;
                    int? readedGroupID = null;

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@GroupName", SqlDbType.NChar).Value = groupName;
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        object[] values = new object[reader.FieldCount];

                        while (reader.Read())
                        {
                            reader.GetValues(values);

                            readedGroupID = Convert.ToInt32(values[0]);
                            count++;
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }

                    switch (count)
                    {
                        case 0:
                            //ПЗ с обозначением groupName не существует
                            break;

                        case 1:
                            //одно ПЗ найдено
                            groupID = readedGroupID;
                            break;

                        default:
                            //считано более одной записи для groupName
                            throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("GROUPS.GROUP_NAME=", groupName)));
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }

            return groupID;
        }

        public static int InsertToGroups(string groupName)
        {
            //вставка новой записи в таблицу GROUPS
            int groupID;

            SqlConnection connection = DBConnections.Connection;

            string sql = "INSERT INTO GROUPS(GROUP_NAME)" +
                         " OUTPUT INSERTED.GROUP_ID VALUES(@GroupName)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@GroupName", SqlDbType.NChar).Value = groupName;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                //считываем идентификатор созданного параметра
                groupID = (int)command.ExecuteScalar();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return groupID;
        }

        public static int CreateGroupID(string groupName)
        {
            //создание производственного задания (ПЗ, оно же ЗНП)
            //возвращает идентификатор созданного ПЗ
            int result;

            //проверяем наличие ПЗ groupName в GROUPS
            int? groupID = GroupIDByGroupName(groupName);

            if (groupID == null)
            {
                result = InsertToGroups(groupName);
            }
            else
                result = (int)groupID;

            return result;
        }

        /*
        public static bool? SapIDBySapDescr(string sapDescr)
        {
            //вычисление идентификатора STATUSBYASSEMBLYPROTOCOL.SAPID по STATUSBYASSEMBLYPROTOCOL.DESCR
            //возвращает:
            //            null - по принятому sapDescr не удалось вычислить идентификтор STATUSBYASSEMBLYPROTOCOL.SAPID (возможно принято sapDescr=null);
            //            идентификтор STATUSBYASSEMBLYPROTOCOL.SAPID
            bool? sapID = null;

            if (sapDescr != null)
            {
                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    string sql = "SELECT SAPID" +
                                 " FROM STATUSBYASSEMBLYPROTOCOL WITH(NOLOCK)" +
                                 string.Format(" WHERE (DESCR='{0}')", sapDescr);

                    int count = 0;
                    bool? readedSapID = null;

                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        object[] values = new object[reader.FieldCount];

                        while (reader.Read())
                        {
                            reader.GetValues(values);

                            readedSapID = Convert.ToBoolean(values[0]);
                            count++;
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }

                    switch (count)
                    {
                        case 0:
                            //статуса по протоколу сборки с обозначением sapDescr не существует
                            break;

                        case 1:
                            //один статус найден
                            sapID = readedSapID;
                            break;

                        default:
                            //считано более одной записи для sapDescr
                            throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("STATUSBYASSEMBLYPROTOCOL.DESCR=", sapDescr)));
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }

            return sapID;
        }
        */

        public static bool DeleteFromDevices(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //удаление записи в таблице DEVICES с идентификатором devID
            //удаляются только такие записи, которые были созданы вручную, если запись создал КИП - она не будет удалена
            //возвращает:
            // true - запись была удалена;
            // false - запись не удалена
            string sql = @"DELETE
                           FROM DEVICES
                           WHERE (
                                   (DEV_ID=@DevID) AND
                                   (MME_CODE=@MmeCode)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
            command.Parameters.Add("@MmeCode", SqlDbType.NVarChar).Value = cManually;

            int recordDeletedCount = command.ExecuteNonQuery();

            return (recordDeletedCount > 0);
        }

        public static void UpdateDeviceChoice(int devID, bool choice)
        {
            //запоминаем значение флага выбора choice группы изделий в поле DEVICES.CHOICE
            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = @"UPDATE DEVICES
                               SET CHOICE=@Choice
                               WHERE (DEV_ID=@DevID)"; //Convert.ToInt32(choice).ToString(), devID);

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
                command.Parameters.Add("@Choice", SqlDbType.Bit).Value = choice;

                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void SetDevicesChoice(IEnumerable<string> devIDEnumerable, bool choice)
        {
            //установка флага выбора choice группе изделий
            //идентификаторы DevID (изделий, входящих в группу) в принятом devIDEnumerable
            if (devIDEnumerable != null)
            {
                foreach (string sDevID in devIDEnumerable)
                {
                    if (int.TryParse(sDevID, out int devID))
                        UpdateDeviceChoice(devID, choice);
                }
            }
        }

        public static void UpdateDeviceAssemblyStatusID(int devID, int assemblyProtocolID, byte assemblyStatusID)
        {
            //установка идентификатора протокола сборки AssemblyProtocolID и состояния AssemblyStatusID для измерения с идентификатором devID
            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                //убрал установку поля MEMBER
                string sql = @"UPDATE DEVICES
                               SET ASSEMBLYPROTOCOLID=@AssemblyProtocolID,
                                   ASSEMBLYSTATUSID=@AssemblyStatusID
                               WHERE (DEV_ID=@DevID)";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
                command.Parameters.Add("@AssemblyStatusID", SqlDbType.TinyInt).Value = assemblyStatusID;
                command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteManualInputDevice(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //удаление созданного вручную изделия со всеми его вручную созданными параметрами
            //удаляем изделие если оно:
            // - не имеет сохранённых средствами КИП СПП значений измеренных параметров;
            // - не использовано в Assemblys;
            // - не имеет комментариев пользователя

            //удаляем все места хранения вручную созданных параметров для изделия devID
            DeleteFromManualInputDevParamByDevID(connection, transaction, devID);

            if (!DoesDeviceHaveParameters(connection, transaction, devID) && !IsAssemblysUseDevice(connection, transaction, devID) && !IsDeviceCommentsUseDevice(connection, transaction, devID))
            {
                //раз ссылки на devID отсутствуют - удаляем его
                DeleteFromDevices(connection, transaction, devID);
            }
        }

        public static void LoadListOfValues(List<string> listOfValues, string fieldName)
        {
            //загружает список возможных значений для выбора пользователем в принятый listOfValues - для реализации возможности пользователя вводить значение из выпадающего списка ComboBox
            if ((listOfValues != null) && !string.IsNullOrEmpty(fieldName))
            {
                if (fieldName.ToUpper() == "ASSEMBLYSTATUSDESCR")
                {
                    //считываем из базы данных множество допустимых значений для поля 'Состояние в протоколе сборки'
                    LoadListOfAssemblyStatusDescr(listOfValues);
                }
            }
        }

        private static void LoadListOfAssemblyStatusDescr(List<string> listOfValues)
        {
            //считывает из базы данных все возможные значения поля STATUSBYASSEMBLYPROTOCOL.DESCR
            if (listOfValues != null)
            {
                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    string sql = @"SELECT DESCR
                                   FROM ASSEMBLYSTATUSES WITH(NOLOCK)
                                   ORDER BY DESCR";

                    string readedDescr = null;

                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        listOfValues.Clear();
                        object[] values = new object[reader.FieldCount];

                        while (reader.Read())
                        {
                            reader.GetValues(values);

                            readedDescr = Convert.ToString(values[0]);
                            listOfValues.Add(readedDescr);
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        public static int InsertToAssemblyProtocols(SqlConnection connection, SqlTransaction transaction, string user)
        {
            //user - пользователь, который выполняет создание протокола сборки
            //создание записи в таблице AssemblyProtocols
            //возвращает идентификатор созданного протокола сборки
            int result = -1;

            SqlCommand command = new SqlCommand("CreateAssemblyProtocol", connection)
            {
                Transaction = transaction,
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@Usr", SqlDbType.NVarChar).Value = user;
            SqlParameter outputParameter = command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int);
            outputParameter.Direction = ParameterDirection.Output;

            command.ExecuteNonQuery();

            result = (int)outputParameter.Value;

            return result;
        }

        public static void UpdateAssemblyProtocols(int assemblyProtocolID, bool deviceModeView, string assemblyJob, bool? export, int? deviceTypeID, int? averageCurrent, string modification, string constructive, int? deviceClass, int? dVdT, double? trr, string tq, double? tgt, int? qrr, string climatic, int? omnity)
        {
            //редактирование протокола сборки с идентификатором assemblyProtocolID в таблице ASSEMBLYPROTOCOLS
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE ASSEMBLYPROTOCOLS
                           SET DEVICEMODEVIEW=@DeviceModeView,
                               ASSEMBLYJOB=@AssemblyJob,
                               EXPORT=@Export,
                               DEVICETYPEID=@DeviceTypeID,
                               AVERAGECURRENT=@AverageCurrent,
                               MODIFICATION=@Modification,
                               CONSTRUCTIVE=@Constructive,
                               DEVICECLASS=@DeviceClass,
                               DVDT=@dVdT,
                               TRR=@Trr,
                               TQ=@Tq,
                               TGT=@Tgt,
                               QRR=@Qrr,
                               CLIMATIC=@Climatic,
                               OMNITY=@Omnity
                           WHERE (ASSEMBLYPROTOCOLID=@AssemblyProtocolID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
            command.Parameters.Add("@DeviceModeView", SqlDbType.Bit).Value = deviceModeView;
            command.Parameters.Add("@AssemblyJob", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(assemblyJob);
            command.Parameters.Add("@Export", SqlDbType.Bit).Value = ChangeNullToDBNullValue(export);
            command.Parameters.Add("@DeviceTypeID", SqlDbType.Int).Value = ChangeNullToDBNullValue(deviceTypeID);
            command.Parameters.Add("@AverageCurrent", SqlDbType.Int).Value = ChangeNullToDBNullValue(averageCurrent);
            command.Parameters.Add("@Modification", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(modification);
            command.Parameters.Add("@Constructive", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(constructive);
            command.Parameters.Add("@DeviceClass", SqlDbType.Int).Value = ChangeNullToDBNullValue(deviceClass);
            command.Parameters.Add("@dVdT", SqlDbType.Int).Value = ChangeNullToDBNullValue(dVdT);
            command.Parameters.Add("@Trr", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(trr);
            command.Parameters.Add("@Tq", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(tq);
            command.Parameters.Add("@Tgt", SqlDbType.Decimal).Value = ChangeNullToDBNullValue(tgt);
            command.Parameters.Add("@Qrr", SqlDbType.Int).Value = ChangeNullToDBNullValue(qrr);
            command.Parameters.Add("@Climatic", SqlDbType.NVarChar).Value = ChangeNullToDBNullValue(climatic);
            command.Parameters.Add("@Omnity", SqlDbType.Int).Value = ChangeNullToDBNullValue(omnity);

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteFromAssemblyProtocols(int assemblyProtocolID)
        {
            //удаление протокола сборки
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE
                           FROM ASSEMBLYPROTOCOLS
                           WHERE (ASSEMBLYPROTOCOLID=@AssemblyProtocolID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void DeleteEmptyAssemblyProtocols()
        {
            //удаление всех пустых протоколов сборки            
            //удаление всех протоколов сборки у которых количество включённых в них групп измерений равно нулю
            SqlConnection connection = DBConnections.Connection;

            string sql = @"DELETE A
                           FROM ASSEMBLYPROTOCOLS AS A
                            LEFT JOIN DEVICES D ON (A.ASSEMBLYPROTOCOLID=D.ASSEMBLYPROTOCOLID)
                           WHERE (D.ASSEMBLYPROTOCOLID IS NULL)";

            SqlCommand command = new SqlCommand(sql, connection);

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static void LoadAssemblyProtocol(int assemblyProtocolID, out bool? deviceModeView, out string assemblyJob, out bool? export, out int? deviceTypeID, out int? averageCurrent, out string modification, out string constructive, out int? deviceClass, out int? dVdT, out double? trr, out string tq, out double? tgt, out int? qrr, out string climatic, out int? omnity)
        {
            //загрузка одного протокола сборки
            deviceModeView = false;
            assemblyJob = null;
            export = false;
            deviceTypeID = null;
            averageCurrent = null;
            modification = null;
            constructive = null;
            deviceClass = null;
            dVdT = null;
            trr = null;
            tq = null;
            tgt = null;
            qrr = null;
            climatic = null;
            omnity = null;

            if (assemblyProtocolID != -1)
            {
                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    string sql = @"SELECT DEVICEMODEVIEW, ASSEMBLYJOB, EXPORT, DEVICETYPEID, AVERAGECURRENT, MODIFICATION, CONSTRUCTIVE, DEVICECLASS,
                                          DVDT, TRR, TQ, TGT, QRR, CLIMATIC, OMNITY
                                   FROM ASSEMBLYPROTOCOLS WITH(NOLOCK)
                                   WHERE (ASSEMBLYPROTOCOLID=@AssemblyProtocolID)";

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        //всегда будет прочитана только одна запись
                        while (reader.Read())
                        {
                            int index = reader.GetOrdinal("DEVICEMODEVIEW");
                            deviceModeView = reader.IsDBNull(index) ? null : (bool?)reader.GetBoolean(index);

                            index = reader.GetOrdinal("ASSEMBLYJOB");
                            assemblyJob = reader.IsDBNull(index) ? null : reader.GetString(index);

                            index = reader.GetOrdinal("EXPORT");
                            export = reader.IsDBNull(index) ? null : (bool?)reader.GetBoolean(index);

                            index = reader.GetOrdinal("DEVICETYPEID");
                            deviceTypeID = reader.IsDBNull(index) ? null : (int?)reader.GetInt32(index);

                            index = reader.GetOrdinal("AVERAGECURRENT");
                            averageCurrent = reader.IsDBNull(index) ? null : (int?)reader.GetInt32(index);

                            index = reader.GetOrdinal("MODIFICATION");
                            modification = reader.IsDBNull(index) ? null : reader.GetString(index);

                            index = reader.GetOrdinal("CONSTRUCTIVE");
                            constructive = reader.IsDBNull(index) ? null : reader.GetString(index);

                            index = reader.GetOrdinal("DEVICECLASS");
                            deviceClass = reader.IsDBNull(index) ? null : (int?)reader.GetInt32(index);

                            index = reader.GetOrdinal("DVDT");
                            dVdT = reader.IsDBNull(index) ? null : (int?)reader.GetInt32(index);

                            index = reader.GetOrdinal("TRR");
                            trr = reader.IsDBNull(index) ? null : (double?)reader.GetDecimal(index);

                            index = reader.GetOrdinal("TQ");
                            tq = reader.IsDBNull(index) ? null : reader.GetString(index);

                            index = reader.GetOrdinal("TGT");
                            tgt = reader.IsDBNull(index) ? null : (double?)reader.GetDecimal(index);

                            index = reader.GetOrdinal("QRR");
                            qrr = reader.IsDBNull(index) ? null : (int?)reader.GetInt32(index);

                            index = reader.GetOrdinal("CLIMATIC");
                            climatic = reader.IsDBNull(index) ? null : reader.GetString(index);

                            index = reader.GetOrdinal("OMNITY");
                            omnity = reader.IsDBNull(index) ? null : (int?)reader.GetInt32(index);
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        public static void ExchangeManualInputAssemblyProtocolParam(int assemblyProtocolID, int oldManualInputParamID, int newManualInputParamID)
        {
            //для изделия assemblyProtocolID выполняет замену идентификатора вручную введённого параметра с oldManualInputParamID на newManualInputParamID
            SqlConnection connection = DBConnections.Connection;

            string sql = @"UPDATE MANUALINPUTASSEMBLYPROTOCOLPARAM
                           SET MANUALINPUTPARAMID=@NewManualInputParamID
                           WHERE (
                                   (ASSEMBLYPROTOCOLID=@AssemblyProtocolID) AND
                                   (MANUALINPUTPARAMID=@OldManualInputParamID)
                                 )";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
            command.Parameters.Add("@NewManualInputParamID", SqlDbType.Int).Value = newManualInputParamID;
            command.Parameters.Add("@OldManualInputParamID", SqlDbType.Int).Value = oldManualInputParamID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static List<ColumnBindingDescr> LoadManualInputAssemblyProtocolParam(DataTable dataTable, int asemblyProtocolID)
        {
            //загрузка созданных вручную параметров протокола сборки с идентификатором asemblyProtocolID в принятый dataTable
            //в dataTable данная реализация всегда создаёт только одну запись, т.к. читаются параметры только для одного протокола сборки
            List<ColumnBindingDescr> result = null;

            if ((dataTable != null) && (asemblyProtocolID != -1))
            {
                result = new List<ColumnBindingDescr>();

                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    string sql = string.Format(@"SELECT APM.ASSEMBLYPROTOCOLID, AP.DESCR, MIP.NAME, APM.MANUALINPUTPARAMID, APM.VALUE
                                                 FROM MANUALINPUTASSEMBLYPROTOCOLPARAM APM
                                                  INNER JOIN ASSEMBLYPROTOCOLS AP ON (APM.ASSEMBLYPROTOCOLID=AP.ASSEMBLYPROTOCOLID)
                                                  INNER JOIN MANUALINPUTPARAMS MIP ON (APM.MANUALINPUTPARAMID=MIP.MANUALINPUTPARAMID)
                                                 WHERE (APM.ASSEMBLYPROTOCOLID={0})", asemblyProtocolID);

                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        while (reader.Read())
                        {
                            if (double.TryParse(reader["VALUE"].ToString(), out double value))
                            {
                                //ищем в dataTable.Rows запись с идентификатором asemblyProtocolID
                                DataRow[] rows = dataTable.Select(string.Format("ASSEMBLYPROTOCOLID={0}", asemblyProtocolID));

                                DataRow row;

                                switch (rows.Length)
                                {
                                    //запись с обрабатываемым значением asemblyProtocolID надо создать
                                    case 0:
                                        row = dataTable.NewRow();

                                        row["ASSEMBLYPROTOCOLID"] = asemblyProtocolID;
                                        row["DESCR"] = reader["DESCR"].ToString();

                                        dataTable.Rows.Add(row);
                                        break;

                                    //запись с обрабатываемым значением asemblyProtocolID уже имеется в dataTable
                                    default:
                                        row = rows[0];
                                        break;
                                }

                                string manualInputParamID = reader["MANUALINPUTPARAMID"].ToString();

                                //столбец имя которого есть идентификатор параметра может содержаться в dataTable.Columns только в единственном числе
                                if (dataTable.Columns.IndexOf(manualInputParamID) == -1)
                                {
                                    dataTable.Columns.Add(manualInputParamID);

                                    //формируем описание столбцов, чтобы вызывающая реализация могла создать столбцы в DataGrid
                                    result.Add(new ColumnBindingDescr
                                    {
                                        Header = reader["NAME"].ToString(),
                                        BindPath = manualInputParamID
                                    });
                                }

                                row[manualInputParamID] = value;
                            }
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }

            return result;
        }

        public static void LoadProfileParameters(string profileGuid, List<string> profileParameters)
        {
            //загрузка списка измеряемых параметров профиля profileName в принятый profileParameters
            if ((profileParameters != null) && (profileGuid != null))
            {
                int profileID = ProfileIDByProfileGUID(profileGuid);

                if (profileID != -1)
                {
                    SqlConnection connection = DBConnections.Connection;

                    bool connectionOpened = false;

                    if (!IsDBConnectionAlive(connection))
                    {
                        connection.Open();
                        connectionOpened = true;
                    }

                    try
                    {
                        string sql = string.Format(@"SELECT P.PARAM_NAME
                                                     FROM PROF_PARAM PP WITH(NOLOCK)
                                                      INNER JOIN PARAMS P WITH(NOLOCK) ON (PP.PARAM_ID=P.PARAM_ID)
                                                     WHERE (PP.PROF_ID={0})
                                                     ORDER BY PP.PROF_TESTTYPE_ID, P.PARAM_NAME", profileID);

                        SqlCommand command = new SqlCommand(sql, connection);
                        SqlDataReader reader = command.ExecuteReader();

                        try
                        {
                            while (reader.Read())
                            {
                                profileParameters.Add(reader["PARAM_NAME"].ToString().TrimEnd());
                            }
                        }
                        finally
                        {
                            reader.Close();
                        }
                    }

                    finally
                    {
                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                        //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                        if (connectionOpened)
                            connection.Close();
                    }
                }
            }
        }

        public static bool IsDBConnectionAlive(SqlConnection connection)
        {
            //проверка не разорвано ли ранее установленное соединение с базой данных
            //возвращает:
            //  true  - соединение живо;
            //  false - соединение разрушено
            SqlCommand command = new SqlCommand("SELECT 1", connection);

            object retObj = null;

            try
            {
                retObj = command.ExecuteScalar();
            }

            catch (Exception)
            {
                return false;
            }

            if (int.TryParse(retObj.ToString(), out int sel))
            {
                return (sel == 1) ? true : false;
            }
            else
                return false;
        }

        private static bool IsCacheExist(SqlConnection connection)
        {
            //проверяет наличие временной таблицы #PE_CACHE в базе данных
            string sql = @"SELECT @ID=OBJECT_ID('TEMPDB..#PE_CACHE')";

            SqlCommand command = new SqlCommand(sql, connection);
            SqlParameter id = command.Parameters.Add("@ID", SqlDbType.Bit);
            id.Direction = System.Data.ParameterDirection.Output;
            command.ExecuteNonQuery();

            return id.Value != DBNull.Value;
        }

        public static int CacheBuild(bool needCount, char delimeter)
        {
            //построение кеша для хранения отобранных групп изделий
            //входные параметры:
            // needCount   - флаг необходимости возвратить количество записей в кеше;
            //возвращает:
            // -1   - нет подключения к базе данных;
            //  0   - кеш построен, входной параметр needCount не требует возврата количества записей в кеше;
            //не 0 - кеш построен, входной параметр needCount требует возврата количества записей кеша - возвращается количество записей в кеше
            int result = -1;

            SqlConnection connection = DBConnections.Connection;
            SqlCommand command = null;

            if (IsDBConnectionAlive(connection))
            {
                bool cacheNotExist = !IsCacheExist(connection);

                if (cacheNotExist)
                {
                    string sql = @"CREATE TABLE #PE_CACHE(
                                                           G_ID INT IDENTITY(1, 1),
                                                           DEV_ID NVARCHAR(400),
                                                           SORTINGVALUE INT
                                                         )";

                    command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                }

                //строим кеш, либо только узнаём количество хранящихся в нём записей
                command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheBuild",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@NeedCount", SqlDbType.Bit).Value = needCount;
                command.Parameters.Add("@NeedRebuild", SqlDbType.Bit).Value = cacheNotExist;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();

                result = Convert.ToInt32(returnValue.Value);
            }

            return result;
        }

        public static int FirstRowNumByNotNullSortingValue()
        {
            //вычисляет порядковый номер последней записи #PE_CACHE, в которой значение поля #PE_CACHE.SORTINGVALUE равно NULL
            //за последней записью с #PE_CACHE.SORTINGVALUE=NULL идут записи с не NULL значениями в порядке, определяемом применённой сортировкой #PE_CACHE.SORTINGVALUE
            //возвращает:
            //порядковый номер последней записи #PE_CACHE в которой #PE_CACHE.SORTINGVALUE=NULL
            //если искомая запись не найдена - возвращает -1;
            //если запиь найдена - возвращает больше чем -1
            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                string sql = @"SELECT COUNT(*)
                               FROM #PE_CACHE
                               WHERE (SORTINGVALUE IS NULL)";

                int count = 0;
                int readedCount = -1;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedCount = Convert.ToInt32(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //записей с NOT(SORTINGVALUE=NULL) значением в поле по которому выполнена сортировка не существует
                        break;

                    case 1:
                        //запись найдена
                        result = readedCount;
                        break;

                    default:
                        //считано более одной записи
                        throw new Exception(string.Format(cReadedRecordNotSingle, count));
                }
            }

            return result;
        }

        public static void CacheFree()
        {
            //уничтожение кеша сортированных групп изделий
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                string sql = @"IF NOT(OBJECT_ID('TEMPDB..#PE_CACHE') IS NULL)
                                  DROP TABLE #PE_CACHE";

                SqlCommand command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
            }
        }

        public delegate void FillPortionData(System.Collections.IList listOfItems, SqlDataReader reader);
        public delegate void ReloadPortionData(System.Collections.IList listOfItems, int index, SqlDataReader reader);
        public static void CacheReadData(FillPortionData fillPortionDataHandler, ReloadPortionData reloadPortionDataHandler, System.Collections.IList listOfItems, int offSet, int portionSize, char delimeter)
        {
            //чтение порции данных размером portionSize со смещением offSet относительно самой первой записи кеша
            if (portionSize > 0)
            {
                SqlConnection connection = DBConnections.Connection;

                if (IsDBConnectionAlive(connection))
                {
                    SqlCommand command = new SqlCommand()
                    {
                        Connection = connection,
                        CommandType = System.Data.CommandType.StoredProcedure,
                        CommandText = "CacheReadData",
                        CommandTimeout = 0
                    };

                    command.Parameters.Add("@Offset", SqlDbType.Int).Value = offSet;
                    command.Parameters.Add("@PortionSize", SqlDbType.Int).Value = portionSize;
                    command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        bool isReload = (listOfItems.Count != 0);
                        int index = 0;

                        while (reader.Read())
                        {
                            switch (isReload)
                            {
                                case true:
                                    reloadPortionDataHandler?.Invoke(listOfItems, index, reader);
                                    index++;
                                    break;

                                default:
                                    fillPortionDataHandler?.Invoke(listOfItems, reader);
                                    break;
                            }
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }
        }

        public static void CacheDevicesSetSortingValue(string columnName, bool direction, char delimeter)
        {
            //заполнение поля кеша SORTINGVALUE значениями для выполнения сортировки по значению полей из таблицы Devices
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheDevicesSetSortingValue",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar).Value = columnName;
                command.Parameters.Add("@Direction", SqlDbType.Bit).Value = direction;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                int result = Convert.ToInt32(returnValue.Value);

                if (result != 0)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheDevicesSetSortingValue'. @RETURN_VALUE={0}. Waiting 0.", result));
            }
        }

        public static void CacheConditionsSetSortingValue(int index, string testTypeName, string condName, bool temperatureMode, bool direction, char delimeter)
        {
            //заполнение поля кеша SORTINGVALUE значениями для выполнения сортировки по значению condition
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheConditionsSetSortingValue",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@Index", SqlDbType.Int).Value = index;
                command.Parameters.Add("@TestTypeName", SqlDbType.NVarChar).Value = testTypeName;
                command.Parameters.Add("@CondName", SqlDbType.Char).Value = condName;
                command.Parameters.Add("@TemperatureMode", SqlDbType.Bit).Value = temperatureMode;
                command.Parameters.Add("@Direction", SqlDbType.Bit).Value = direction;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                int result = Convert.ToInt32(returnValue.Value);

                if (result != 0)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheConditionsSetSortingValue'. @RETURN_VALUE={0}. Waiting 0.", result));
            }
        }

        public static void CacheParametersSetSortingValue(int index, string testTypeName, string paramName, bool temperatureMode, bool direction, char delimeter)
        {
            //заполнение поля кеша SORTINGVALUE значениями для выполнения сортировки по значению parameter
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheParametersSetSortingValue",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@Index", SqlDbType.Int).Value = index;
                command.Parameters.Add("@TestTypeName", SqlDbType.NVarChar).Value = testTypeName;
                command.Parameters.Add("@ParamName", SqlDbType.Char).Value = paramName;
                command.Parameters.Add("@TemperatureMode", SqlDbType.Bit).Value = temperatureMode;
                command.Parameters.Add("@Direction", SqlDbType.Bit).Value = direction;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                int result = Convert.ToInt32(returnValue.Value);

                if (result != 0)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheConditionsSetSortingValue'. @RETURN_VALUE={0}. Waiting 0.", result));
            }
        }

        public static void CacheManuallyParametersSetSortingValue(int index, string manualInputParamName, bool temperatureMode, bool direction, char delimeter)
        {
            //заполнение поля кеша SORTINGVALUE значениями для выполнения сортировки по значениям параметров созданных вручную
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheManuallyParametersSetSortingValue",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@Index", SqlDbType.Int).Value = index;
                command.Parameters.Add("@ManualInputParamName", SqlDbType.NVarChar).Value = manualInputParamName;
                command.Parameters.Add("@TemperatureMode", SqlDbType.Bit).Value = temperatureMode;
                command.Parameters.Add("@Direction", SqlDbType.Bit).Value = direction;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                int result = Convert.ToInt32(returnValue.Value);

                if (result != 0)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheManuallyParametersSetSortingValue'. @RETURN_VALUE={0}. Waiting 0.", result));
            }
        }

        private static string DevicesValuesTypeColumnNameByDevicesColumnName(string columnName)
        {
            //возвращает имя столбца в пользовательском табличном типе базы данных 'dbo.DevicesValues' который надо использовать для выполнения фильтрации по столбцу columnName таблицы Devices
            switch (columnName)
            {
                case "CODE":
                case "PROFILEBODY":
                case "DEVICETYPERU":
                case "CONSTRUCTIVE":
                case "GROUP_NAME":
                case "ASSEMBLYSTATUSDESCR":
                case "REASON":
                case "CODEOFNONMATCH":
                case "ITEM":
                case "MME_CODE":
                case "USR":
                case "DEVICECOMMENTS":
                    return "NVARCHARVALUE";

                case "ASSEMBLYPROTOCOLDESCR":
                case "DEVICECLASS":
                case "AVERAGECURRENT":
                case "STATUS":
                    return "INTVALUE";

                case "CHOICE":
                    return "BITVALUE";

                case "TS":
                    return "DATETIMEVALUE";

                default:
                    throw new Exception(string.Format("DatabaseRoutines.cs. DevicesValuesTypeColumnNameByDevicesColumnName. Для '{0}' обработка не предусмотрена.", columnName));
            }
        }

        private static string AssemblyProtocolsColumnByColumnName(string columnName)
        {
            //возвращает имя столбца в пользовательском табличном типе базы данных 'dbo.AssemblyProtocols' который надо использовать для выполнения фильтрации по столбцу columnName таблицы AssemblyProtocols
            switch (columnName.ToUpper())
            {
                case "DESCR":
                case "RECORDCOUNT":
                case "AVERAGECURRENT":
                case "DEVICECLASS":
                case "DVDT":
                case "QRR":
                case "OMNITY":
                    return "INTVALUE";

                case "TS":
                    return "DATETIMEVALUE";

                case "USR":
                case "ASSEMBLYJOB":
                case "DEVICETYPERU":
                case "DEVICETYPEEN":
                case "CONSTRUCTIVE":
                case "TQ":
                case "CLIMATIC":
                    return "NVARCHARVALUE";

                case "EXPORT":
                    return "BITVALUE";

                case "TRR":
                case "TGT":
                    return "DECIMALVALUE";

                default:
                    throw new Exception(string.Format("DatabaseRoutines.cs. AssemblyProtocolsColumnByColumnName. Для '{0}' обработка не предусмотрена.", columnName));
            }
        }

        private static object AssemblyProtocolsTypedValueByColumnName(string column, string value)
        {
            //возвращает типизированное значение для формирования табличного типа базы данных 'dbo.AssemblyProtocols' который надо использовать для выполнения фильтрации по столбцу columnName таблицы AssemblyProtocols
            switch (column.ToUpper())
            {
                case "NVARCHARVALUE":
                    return value;

                case "INTVALUE":
                    return int.Parse(value);

                case "BITVALUE":
                    return ChoiceToBoolValue(value);

                case "DATETIMEVALUE":
                    return value;

                case "DECIMALVALUE":
                    return double.Parse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);

                default:
                    throw new Exception(string.Format("DatabaseRoutines.cs. AssemblyProtocolsTypedValueByColumnName. Для '{0}' обработка не предусмотрена.", column));
            }
        }

        private static void FillSqlDbTypeStructuredDevicesValues(DataTable dt, string columnNameForDataFill, IEnumerable<string> values)
        {
            //формирование значения параметра хранимой процедуры имеющего тип System.Data.SqlDbType.Structured для фильтрации данных по значениям столбца columnNameForDataFill таблицы Devices
            if ((dt != null) && !string.IsNullOrEmpty(columnNameForDataFill) && (values != null))
            {
                dt.Columns.Clear();
                dt.Rows.Clear();

                //создаём все 4 столбца, но данными наполняем только один столбец - все остальные заполняем значением DBNull.Value
                dt.Columns.Add("NVARCHARVALUE", typeof(string));
                dt.Columns.Add("INTVALUE", typeof(int));
                dt.Columns.Add("BITVALUE", typeof(bool));
                dt.Columns.Add("DATETIMEVALUE", typeof(DateTime));

                //вычисляем имя столбца в пользовательском табличном типе данных, который надо заполнить данными
                string columnName = DevicesValuesTypeColumnNameByDevicesColumnName(columnNameForDataFill);

                if (!string.IsNullOrEmpty(columnName))
                {
                    foreach (string value in values)
                    {
                        DataRow row = dt.NewRow();

                        foreach (DataColumn colum in dt.Columns)
                        {
                            switch (colum.ColumnName == columnName)
                            {
                                case true:
                                    switch (columnNameForDataFill)
                                    {
                                        case "CHOICE":
                                            row[colum] = ChoiceToBoolValue(value);
                                            break;

                                        case "STATUS":
                                            row[colum] = DeviceStatusToIntValue(value);
                                            break;

                                        default:
                                            row[colum] = ChangeNullToDBNullValue(value);
                                            break;
                                    }

                                    break;

                                default:
                                    row[colum] = DBNull.Value;
                                    break;
                            }
                        }

                        dt.Rows.Add(row);
                    }
                }
            }
        }

        private static void FillSqlDbTypeStructuredAssemblyProtocols(DataTable dt, string columnNameForDataFill, IEnumerable<string> values)
        {
            //формирование значения параметра хранимой процедуры имеющего тип System.Data.SqlDbType.Structured для фильтрации данных по значениям столбца columnNameForDataFill таблицы AssemblyProtocols
            if ((dt != null) && !string.IsNullOrEmpty(columnNameForDataFill) && (values != null))
            {
                dt.Columns.Clear();
                dt.Rows.Clear();

                //создаём 5 столбцов, но данными наполняем только один столбец - все остальные заполняем значением DBNull.Value
                dt.Columns.Add("NVARCHARVALUE", typeof(string));
                dt.Columns.Add("INTVALUE", typeof(int));
                dt.Columns.Add("BITVALUE", typeof(bool));
                dt.Columns.Add("DATETIMEVALUE", typeof(DateTime));
                dt.Columns.Add("DECIMALVALUE", typeof(double));

                //вычисляем имя столбца в пользовательском табличном типе данных, который надо заполнить данными
                string column = AssemblyProtocolsColumnByColumnName(columnNameForDataFill);

                if (!string.IsNullOrEmpty(column))
                {
                    foreach (string value in values)
                    {
                        DataRow row = dt.NewRow();

                        foreach (DataColumn datacolumn in dt.Columns)
                        {
                            switch (datacolumn.ColumnName == column)
                            {
                                case true:
                                    object obj = AssemblyProtocolsTypedValueByColumnName(column, value);
                                    row[datacolumn] = ChangeNullToDBNullValue(obj);
                                    break;

                                default:
                                    row[datacolumn] = DBNull.Value;
                                    break;
                            }
                        }

                        dt.Rows.Add(row);
                    }
                }
            }
        }

        public static int CacheDevicesApplyFilter(string columnName, byte comparison, IEnumerable<string> values, char delimeter)
        {
            //применение фильтра по значению реквизита с именем columnName
            //возвращает:
            // больше либо равно 0 - количество записей, которые удалила данная реализация из кеша - успешный результат;
            // иначе - возбуждает исключение ибо кеш не создан
            int result = 0;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheDevicesApplyFilter",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@ColumnName", SqlDbType.VarChar).Value = columnName;
                command.Parameters.Add("@Comparison", SqlDbType.TinyInt).Value = comparison;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                DataTable dt = new DataTable();
                FillSqlDbTypeStructuredDevicesValues(dt, columnName, values);
                command.Parameters.Add(new SqlParameter("@Values", System.Data.SqlDbType.Structured) { TypeName = "dbo.DevicesValues", Value = dt });

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                result = Convert.ToInt32(returnValue.Value);

                if (result == -1)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheDevicesApplyFilter'. @RETURN_VALUE={0}. Waiting 0.", result));
            }

            return result;
        }

        private static void FillSqlDbTypeStructuredConditionValues(DataTable dt, string columnName, Type columnType, IEnumerable<string> values)
        {
            //формирование значения параметра хранимой процедуры имеющего тип System.Data.SqlDbType.Structured для condition
            if ((dt != null) && !string.IsNullOrEmpty(columnName) && (columnType != null) && (values != null))
            {
                dt.Columns.Clear();
                dt.Rows.Clear();

                dt.Columns.Add(columnName, columnType);

                foreach (string value in values)
                {
                    switch (value == null)
                    {
                        case true:
                            dt.Rows.Add(DBNull.Value);
                            break;

                        default:
                            dt.Rows.Add(AddTrailingZeros(value, 10));
                            break;
                    }
                }
            }
        }

        public static int CacheConditionsApplyFilter(int index, string testTypeName, string condName, bool temperatureMode, byte comparison, IEnumerable<string> values, char delimeter)
        {
            //применение фильтра по значению condition
            //возвращает:
            // больше либо равно 0 - количество записей, которые удалила данная реализация из кеша - успешный результат;
            // иначе - возбуждает исключение ибо кеш не создан
            int result = 0;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheConditionsApplyFilter",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@Index", SqlDbType.Int).Value = index;
                command.Parameters.Add("@TestTypeName", SqlDbType.NVarChar).Value = testTypeName;
                command.Parameters.Add("@CondName", SqlDbType.Char).Value = condName;
                command.Parameters.Add("@TemperatureMode", SqlDbType.Bit).Value = temperatureMode;
                command.Parameters.Add("@Comparison", SqlDbType.TinyInt).Value = comparison;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                DataTable dt = new DataTable();
                FillSqlDbTypeStructuredConditionValues(dt, "VALUE", typeof(string), values);
                command.Parameters.Add(new SqlParameter("@Values", System.Data.SqlDbType.Structured) { TypeName = "dbo.ConditionValues", Value = dt });

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                result = Convert.ToInt32(returnValue.Value);

                if (result == -1)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheConditionsApplyFilter'. @RETURN_VALUE={0}. Waiting 0.", result));
            }

            return result;
        }

        private static void FillSqlDbTypeStructuredParameterValues(DataTable dt, string columnName, Type columnType, IEnumerable<double?> values)
        {
            //формирование значения параметра хранимой процедуры имеющего тип System.Data.SqlDbType.Structured для parameter
            if ((dt != null) && !string.IsNullOrEmpty(columnName) && (columnType != null) && (values != null))
            {
                dt.Columns.Clear();
                dt.Rows.Clear();

                dt.Columns.Add(columnName, columnType);

                foreach (double? value in values)
                {
                    switch (value == null)
                    {
                        case true:
                            dt.Rows.Add(DBNull.Value);
                            break;

                        default:
                            dt.Rows.Add((double)value);
                            break;
                    }
                }
            }
        }

        public static int CacheParametersApplyFilter(int index, string testTypeName, string paramName, bool temperatureMode, byte comparison, IEnumerable<double?> values, char delimeter)
        {
            //применение фильтра по значению parameter
            //возвращает:
            // больше либо равно 0 - количество записей, которые удалила данная реализация из кеша - успешный результат;
            // иначе - возбуждает исключение ибо кеш не создан
            int result = 0;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheParametersApplyFilter",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@Index", SqlDbType.Int).Value = index;
                command.Parameters.Add("@TestTypeName", SqlDbType.NVarChar).Value = testTypeName;
                command.Parameters.Add("@ParamName", SqlDbType.Char).Value = paramName;
                command.Parameters.Add("@TemperatureMode", SqlDbType.Bit).Value = temperatureMode;
                command.Parameters.Add("@Comparison", SqlDbType.TinyInt).Value = comparison;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                DataTable dt = new DataTable();
                FillSqlDbTypeStructuredParameterValues(dt, "VALUE", typeof(double), values);
                command.Parameters.Add(new SqlParameter("@Values", System.Data.SqlDbType.Structured) { TypeName = "dbo.ParameterValues", Value = dt });

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                result = Convert.ToInt32(returnValue.Value);

                if (result == -1)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheParametersApplyFilter'. @RETURN_VALUE={0}. Waiting 0.", result));
            }

            return result;
        }

        public static int CacheManuallyParametersApplyFilter(int index, string manualInputParamName, bool temperatureMode, byte comparison, IEnumerable<double?> values, char delimeter)
        {
            //применение фильтра по значению parameter
            //возвращает:
            // больше либо равно 0 - количество записей, которые удалила данная реализация из кеша - успешный результат;
            // иначе - возбуждает исключение ибо кеш не создан
            int result = 0;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheManuallyParametersApplyFilter",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@Index", SqlDbType.Int).Value = index;
                command.Parameters.Add("@ManualInputParamName", SqlDbType.NVarChar).Value = manualInputParamName;
                command.Parameters.Add("@TemperatureMode", SqlDbType.Bit).Value = temperatureMode;
                command.Parameters.Add("@Comparison", SqlDbType.TinyInt).Value = comparison;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                DataTable dt = new DataTable();
                FillSqlDbTypeStructuredParameterValues(dt, "VALUE", typeof(double), values);
                command.Parameters.Add(new SqlParameter("@Values", System.Data.SqlDbType.Structured) { TypeName = "dbo.ParameterValues", Value = dt });

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                result = Convert.ToInt32(returnValue.Value);

                if (result == -1)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheManuallyParametersApplyFilter'. @RETURN_VALUE={0}. Waiting 0.", result));
            }

            return result;
        }

        public static void SetChoiceForAllDevicesInCache()
        {
            //выполняет установку DEVICES.CHOICE для всех групп изделий, хранящихся в кеше - временная таблица на SQL сервере #PE_CACHE
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand("UpdateChoiceForAllDevicesInCache", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add("@Choice", SqlDbType.Bit).Value = true;

                command.ExecuteNonQuery();
            }
        }

        public static void DropChoiceForAllDevicesInCache()
        {
            //сбрасывает флаг выбора DEVICES.CHOICE со всех групп изделий, хранящихся в кеше - временная таблица на SQL сервере #PE_CACHE
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand("UpdateChoiceForAllDevicesInCache", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add("@Choice", SqlDbType.Bit).Value = false;

                command.ExecuteNonQuery();
            }
        }

        public static void SetChoiceForRowCountDevicesInCache(int rowCount)
        {
            //выполняет установку DEVICES.CHOICE для rowCount групп изделий, хранящихся в кеше - временная таблица на SQL сервере #PE_CACHE
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand("SetChoiceForRowCountDevicesInCache", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add("@RowCount", SqlDbType.Int).Value = rowCount;

                command.ExecuteNonQuery();
            }
        }

        private static bool IsCacheAssemblyProtocolsExist(SqlConnection connection)
        {
            //проверяет наличие временной таблицы #PE_CACHEASSEMBLYPROTOCOLS в базе данных
            string sql = @"SELECT @ID=OBJECT_ID('TEMPDB..#PE_CACHEASSEMBLYPROTOCOLS')";

            SqlCommand command = new SqlCommand(sql, connection);
            SqlParameter id = command.Parameters.Add("@ID", SqlDbType.Bit);
            id.Direction = System.Data.ParameterDirection.Output;
            command.ExecuteNonQuery();

            return id.Value != DBNull.Value;
        }

        public static void CacheAssemblyProtocolsFree()
        {
            //уничтожение кеша протоколов сборки
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                string sql = @"IF NOT(OBJECT_ID('TEMPDB..#PE_CACHEASSEMBLYPROTOCOLS') IS NULL)
                                  DROP TABLE #PE_CACHEASSEMBLYPROTOCOLS";

                SqlCommand command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
            }
        }

        public static int FirstRowNumByNotNullSortingValueAssemblyProtocols()
        {
            //вычисляет порядковый номер последней записи #PE_CACHEASSEMBLYPROTOCOLS, в которой значение поля #PE_CACHEASSEMBLYPROTOCOLS.SORTINGVALUE равно NULL
            //за последней записью с #PE_CACHEASSEMBLYPROTOCOLS.SORTINGVALUE=NULL идут записи с не NULL значениями в порядке, определяемом применённой сортировкой #PE_CACHEASSEMBLYPROTOCOLS.SORTINGVALUE
            //возвращает:
            //порядковый номер последней записи #PE_CACHEASSEMBLYPROTOCOLS в которой #PE_CACHEASSEMBLYPROTOCOLS.SORTINGVALUE=NULL
            //если искомая запись не найдена - возвращает -1;
            //если запиь найдена - возвращает больше чем -1
            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                string sql = @"SELECT COUNT(*)
                               FROM #PE_CACHEASSEMBLYPROTOCOLS
                               WHERE (SORTINGVALUE IS NULL)";

                int count = 0;
                int readedCount = -1;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedCount = Convert.ToInt32(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //записей с NOT(SORTINGVALUE=NULL) значением в поле по которому выполнена сортировка не существует
                        break;

                    case 1:
                        //запись найдена
                        result = readedCount;
                        break;

                    default:
                        //считано более одной записи
                        throw new Exception(string.Format(cReadedRecordNotSingle, count));
                }
            }

            return result;
        }

        public static int CacheAssemblyProtocolsBuild(bool needCount)
        {
            //построение кеша для хранения отобранных протоколов сборки
            //входные параметры:
            // needCount   - флаг необходимости возвратить количество записей в кеше;
            //возвращает:
            // -1    - нет подключения к базе данных;
            // иначе - кеш построен, в нём такое (возвращаемый результат) количество записей
            int result = -1;

            SqlConnection connection = DBConnections.Connection;
            SqlCommand command = null;

            if (IsDBConnectionAlive(connection))
            {
                bool cacheNotExist = !IsCacheAssemblyProtocolsExist(connection);
                string sqlText;

                if (cacheNotExist)
                {
                    sqlText = @"CREATE TABLE #PE_CACHEASSEMBLYPROTOCOLS(
                                                                        ASSEMBLYPROTOCOLID INT,
                                                                        SORTINGVALUE INT
                                                                       )";

                    command = new SqlCommand(sqlText, connection);
                    command.ExecuteNonQuery();
                }

                //строим кеш, либо только узнаём количество хранящихся в нём записей
                command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheAssemblyProtocolsBuild",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@NeedCount", SqlDbType.Bit).Value = needCount;
                command.Parameters.Add("@NeedRebuild", SqlDbType.Bit).Value = cacheNotExist;

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();

                result = Convert.ToInt32(returnValue.Value);
            }

            return result;
        }

        public static void CacheAssemblyProtocolsReadData(FillPortionData fillPortionDataHandler, System.Collections.IList listOfItems, int offSet, int portionSize, char delimeter)
        {
            //чтение порции данных размером portionSize со смещением offSet относительно самой первой записи кеша
            if (portionSize > 0)
            {
                SqlConnection connection = DBConnections.Connection;

                if (IsDBConnectionAlive(connection))
                {
                    //группировка в запросе вычисления количества групп измерений в протоколе сборки должна быть точно такой как в функии возвращающей табличное значение dbo.AllMeasurementGroups
                    string sqlText = @";WITH DS AS
                                       (
                                         SELECT ASSEMBLYPROTOCOLID, SORTINGVALUE
                                         FROM #PE_CACHEASSEMBLYPROTOCOLS
                                         ORDER BY SORTINGVALUE
                                         OFFSET @Offset ROWS FETCH NEXT @PortionSize ROWS ONLY
                                       )

                                       SELECT AP.ASSEMBLYPROTOCOLID, AP.DESCR, AP.TS, AP.RECORDCOUNT,
                                              AP.USR, AP.DEVICEMODEVIEW, AP.ASSEMBLYJOB, AP.EXPORT, DT.DEVICETYPERU, DT.DEVICETYPEEN, AP.AVERAGECURRENT, AP.CONSTRUCTIVE, AP.DEVICECLASS,
                                              AP.DVDT, AP.TRR, AP.TQ, AP.TGT, AP.QRR, AP.CLIMATIC, AP.OMNITY
                                       FROM DS
                                        INNER JOIN ASSEMBLYPROTOCOLS AP WITH(NOLOCK) ON (DS.ASSEMBLYPROTOCOLID=AP.ASSEMBLYPROTOCOLID)
                                        LEFT JOIN DEVICETYPES DT WITH(NOLOCK) ON (AP.DEVICETYPEID=DT.DEVICETYPEID)
                                       ORDER BY DS.SORTINGVALUE";

                    SqlCommand command = new SqlCommand(sqlText, connection);
                    command.Parameters.Add("@Offset", SqlDbType.Int).Value = offSet;
                    command.Parameters.Add("@PortionSize", SqlDbType.Int).Value = portionSize;
                    command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        while (reader.Read())
                            fillPortionDataHandler?.Invoke(listOfItems, reader);
                    }

                    finally
                    {
                        reader.Close();
                    }
                }
            }
        }

        public static int CacheAssemblyProtocolsApplyFilter(string columnName, byte comparison, IEnumerable<string> values)
        {
            //применение фильтра по значению реквизита с именем columnName
            //возвращает:
            // больше либо равно 0 - количество записей, которые удалила данная реализация из кеша - успешный результат;
            // иначе - возбуждает исключение ибо кеш не создан
            int result = 0;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheAssemblyProtocolsApplyFilter",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@ColumnName", SqlDbType.VarChar).Value = columnName;
                command.Parameters.Add("@Comparison", SqlDbType.TinyInt).Value = comparison;

                DataTable dt = new DataTable();
                FillSqlDbTypeStructuredAssemblyProtocols(dt, columnName, values);
                command.Parameters.Add(new SqlParameter("@Values", System.Data.SqlDbType.Structured) { TypeName = "dbo.AssemblyProtocols", Value = dt });

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                result = Convert.ToInt32(returnValue.Value);

                if (result == -1)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheAssemblyProtocolsApplyFilter'. @RETURN_VALUE={0}. Waiting 0.", result));
            }

            return result;
        }

        public static void CacheAssemblyProtocolsSetSortingValue(string columnName, bool direction)
        {
            //заполнение поля кеша SORTINGVALUE значениями для выполнения сортировки по значению полей из таблицы AssemblyProtocols
            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "CacheAssemblyProtocolsSetSortingValue",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar).Value = columnName;
                command.Parameters.Add("@Direction", SqlDbType.Bit).Value = direction;

                SqlParameter returnValue = command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                returnValue.Direction = System.Data.ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                int result = Convert.ToInt32(returnValue.Value);

                if (result != 0)
                    throw new InvalidOperationException(string.Format("StoredProcedure 'CacheAssemblyProtocolsSetSortingValue'. @RETURN_VALUE={0}. Waiting 0.", result));
            }
        }

        public static int FillAssemblyProtocol(SqlConnection connection, SqlTransaction transaction, bool assemblyProtocolMode, int assemblyProtocolID)
        {
            //assemblyProtocolMode - режим работы приложения:
            //                       True - приложение а режиме отображения прототокола сборки;
            //                       False - приложение а режиме отображения данных.
            //assemblyProtocolID - идентификатор протокола сборки в который помещаются записи кеша
            //возвращает количество строк протокола сборки
            int result = -1;

            SqlCommand command = new SqlCommand("FillAssemblyProtocol", connection)
            {
                Transaction = transaction,
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@AssemblyProtocolMode", SqlDbType.Bit).Value = assemblyProtocolMode;
            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
            SqlParameter outputParameter = command.Parameters.Add("@MovedRowsCount", SqlDbType.Int);
            outputParameter.Direction = ParameterDirection.Output;

            command.ExecuteNonQuery();

            result = (int)outputParameter.Value;

            return result;
        }

        public static int CreateAssemblyProtocol(string user, bool assemblyProtocolMode)
        {
            //выполняет установку состояния 'Сборка' для всех групп изделий, хранящихся в кеше имеющих значение DEVICE.CHOICE=1 и состояние ASSEMBLYSTATUSID соответствующее 'НЗП' для случая создания протокола сборки из режима просмотра данных или ASSEMBLYSTATUSID соответствующее 'Сборка' из режима просмотра протокола сборки
            //в случае успешного выполнения возвращает идентификатор созданного протокола сборки - число больше, чем ноль
            //в случае неудачи - данная реализация возвращает -1
            int assemblyProtocolID = -1;
            int movedRowsCount = -1;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                //создаём протокол сборки и получаем его идентификатор              
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    assemblyProtocolID = InsertToAssemblyProtocols(connection, transaction, user);
                    movedRowsCount = FillAssemblyProtocol(connection, transaction, assemblyProtocolMode, assemblyProtocolID);
                }
                catch (Exception e)
                {
                    transaction.Rollback();

                    throw new Exception(e.Message);
                }

                //проверяем, что созданный протокол сборки не пустой
                if (movedRowsCount <= 0)
                {
                    //пустые протоколы не имеют смысла
                    transaction.Rollback();
                    return -1;
                }

                //раз мы здесь - ошибок не возникло, успешно завершаем транзакцию
                transaction.Commit();
            }

            return assemblyProtocolID;
        }

        public static bool SendToAssemblyProtocol(bool assemblyProtocolMode, int assemblyProtocolID)
        {
            //выполняет отправку выбранных пользователем групп измерений в протокол сборки assemblyProtocolID
            //не выполняет проверку существования протокола сборки assemblyProtocolID - считает что он существует
            //возвращает:
            //           true - группы измерений успешно отправлены в протокол сборки;
            //           false - реализация отработала с ошибкой, ничего не отправлено в протокол сборки

            int movedRowsCount = -1;
            bool result = false;

            SqlConnection connection = DBConnections.Connection;

            if (IsDBConnectionAlive(connection))
            {
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    movedRowsCount = FillAssemblyProtocol(connection, transaction, assemblyProtocolMode, assemblyProtocolID);
                }
                catch (Exception e)
                {
                    transaction.Rollback();

                    throw new Exception(e.Message);
                }

                //убеждаемся что протокол сборки был пополнен отправленными в него группами измерений
                if (movedRowsCount > 0)
                {
                    //раз мы здесь - ошибок не возникло, успешно завершаем транзакцию
                    transaction.Commit();
                    result = true;
                }
                else
                    transaction.Rollback();
            }

            return result;
        }

        public delegate void FillDataByAssemblyProtocolID(System.Collections.IList listOfItems, SqlDataReader reader);
        public static void ReadDataByAssemblyProtocolID(int assemblyProtocolID, char delimeter, FillDataByAssemblyProtocolID FillDataHandler, System.Collections.IList listOfItems)
        {
            //чтение исходных данных для построения отчёта по протоколу сборки
            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand()
                {
                    Connection = connection,
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = "DataByAssemblyProtocolID",
                    CommandTimeout = 0
                };

                command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;
                command.Parameters.Add("@Delimeter", SqlDbType.Char).Value = delimeter;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        FillDataHandler?.Invoke(listOfItems, reader);
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static int SetInitStateForSelectedDevices(int assemblyProtocolID)
        {
            //устанавливает состояние соответствующее 'НЗП', DEVICES.CHOICE=0, DEVICES.ASSEMBLYSTATUSID=0, DEVICES.ASSEMBLYPROTOCOLID=NULL для выбранных записей DEVICES имеющих идентификатор протокола сборки равного принятому assemblyProtocolID
            //возвращает количество записей в которых был установлен статус 'НЗП'
            int result = 0;

            SqlConnection connection = DBConnections.Connection;

            SqlCommand command = new SqlCommand("SetInitStateForSelectedDevices", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

            SqlParameter movedRowsCount = command.Parameters.Add("@MovedRowsCount", SqlDbType.Int);
            movedRowsCount.Direction = ParameterDirection.Output;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                command.ExecuteNonQuery();

                result = (int)movedRowsCount.Value;
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        public static void DestroyAssemblyProtocol(int assemblyProtocolID)
        {
            //расформирование протокола сборки с идентификатором @AssemblyProtocolID
            //устанавливает DEVICES.CHOICE = 0, DEVICES.ASSEMBLYSTATUSID = 0, DEVICES.ASSEMBLYPROTOCOLID = NULL
            //для записей DEVICES.ASSEMBLYPROTOCOLID = @AssemblyProtocolID
            //уничтожает протокол сборки с идентификатором @AssemblyProtocolID в таблице AssemblyProtocols

            SqlConnection connection = DBConnections.Connection;

            SqlCommand command = new SqlCommand("DestroyAssemblyProtocol", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }
            try
            {
                command.ExecuteNonQuery();
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        public static int DescrByAssemblyProtocolID(int assemblyProtocolID)
        {
            //вычисляет обозначение протокола сборки Descr по идентификатору протокола сборки assemblyProtocolID
            //возвращает:
            //-1  - протокол сборки с идентификаторрм assemblyProtocolID не существует в таблице ASSEMBLYPROTOCOLS
            //больше чем -1 - значение ASSEMBLYPROTOCOLS.DESCR по принятому идентификатору протокола сборки assemblyProtocolID

            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = @"SELECT DESCR
                               FROM ASSEMBLYPROTOCOLS WITH(NOLOCK)
                               WHERE (ASSEMBLYPROTOCOLID=@AssemblyProtocolID)";

                int count = 0;
                int readedDescr = -1;

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@AssemblyProtocolID", SqlDbType.Int).Value = assemblyProtocolID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedDescr = Convert.ToInt32(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //протокол сборки с идентификатором assemblyProtocolID не существует
                        break;

                    case 1:
                        //один протокол сборки найден
                        result = readedDescr;
                        break;

                    default:
                        //считано более одной записи для assemblyProtocolID
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("ASSEMBLYPROTOCOLS.ASSEMBLYPROTOCOLID=", assemblyProtocolID)));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        public static int AssemblyProtocolIDByDescr(int descr)
        {
            //вычисляет идентификатор протокола сборки ASSEMBLYPROTOCOLS.ASSEMBLYPROTOCOLID по обозначение протокола сборки ASSEMBLYPROTOCOLS.DESCR
            //возвращает:
            //-1  - протокол сборки с обозначением descr не существует в таблице ASSEMBLYPROTOCOLS
            //больше чем -1 - значение ASSEMBLYPROTOCOLS.ASSEMBLYPROTOCOLID по принятому обозначению протокола сборки descr

            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            int count = 0;
            int readedAssemblyProtocolID = -1;

            try
            {
                string sql = @"SELECT ASSEMBLYPROTOCOLID
                               FROM ASSEMBLYPROTOCOLS WITH(NOLOCK)
                               WHERE (DESCR=@Descr)";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@Descr", SqlDbType.Int).Value = descr;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedAssemblyProtocolID = Convert.ToInt32(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            switch (count)
            {
                case 0:
                    //протокол сборки с обозначением descr не существует
                    break;

                case 1:
                    //один протокол сборки найден
                    result = readedAssemblyProtocolID;
                    break;

                default:
                    //считано более одной записи для assemblyProtocolID
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("ASSEMBLYPROTOCOLS.DESCR=", descr)));
            }

            return result;
        }

        public static string AssemblyDescrByAssemblyStatusID(int assemblyStatusID)
        {
            //вычисляет обозначение статуса сборки Descr по идентификатору статуса сборки assemblyStatusID
            //возвращает:
            //null  - статус сборки с идентификаторрм assemblyStatusID не существует в таблице ASSEMBLYSTATUSES
            //не null - значение ASSEMBLYSTATUSES.DESCR по принятому идентификатору статуса сборки assemblyStatusID

            string result = null;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = @"SELECT DESCR
                               FROM ASSEMBLYSTATUSES WITH(NOLOCK)
                               WHERE (ASSEMBLYSTATUSID=@AssemblyStatusID)";

                int count = 0;
                string readedDescr = null;

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@AssemblyStatusID", SqlDbType.TinyInt).Value = assemblyStatusID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedDescr = Convert.ToString(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //статус сборки с идентификатором assemblyStatusID не существует
                        break;

                    case 1:
                        //один статус сборки найден
                        result = readedDescr;
                        break;

                    default:
                        //считано более одной записи для assemblyStatusID
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("ASSEMBLYSTATUSES.ASSEMBLYSTATUSID=", assemblyStatusID)));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        public static double? TemperatureByProfileGUID(string profileGUID)
        {
            //возвращает значение condition с именем 'CLAMP_Temperature' для профиля с принятым profileGUID
            double? result = null;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = @"SELECT PC.VALUE
                               FROM PROF_COND PC WITH(NOLOCK)
                                INNER JOIN CONDITIONS C WITH(NOLOCK) ON (PC.COND_ID=C.COND_ID)
                                INNER JOIN PROFILES P WITH(NOLOCK) ON (PC.PROF_ID=P.PROF_ID)
                               WHERE (
                                      (C.COND_NAME='CLAMP_Temperature') AND
                                      (P.PROF_GUID=@ProfileGUID)
                                     )";

                int count = 0;
                double readedTemperature = 0;

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@ProfileGUID", SqlDbType.UniqueIdentifier).Value = new Guid(profileGUID);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        if (!double.TryParse(values[0].ToString(), out readedTemperature))
                            readedTemperature = 0;

                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //профиль не содержит condition с именем 'CLAMP_Temperature'
                        break;

                    case 1:
                        //одно значение condition с именем 'CLAMP_Temperature' найдено
                        result = readedTemperature;
                        break;

                    default:
                        //считано более одного значения condition с именем 'CLAMP_Temperature'
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("Table 'PROF_COND'. PROF_GUID=", profileGUID, " COND_NAME='CLAMP_Temperature'")));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        public static int TestTypeIDByTestTypeName(string testTypeName)
        {
            //вычисляет идентификатор теста TEST_TYPE.TEST_TYPE_ID по принятому наименованию теста testTypeName
            //возвращает:
            //-1  - тест с именем testTypeName не существует в таблице TEST_TYPE
            //больше чем -1 - значение TEST_TYPE.TEST_TYPE_ID по принятому наименованию теста testTypeName
            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = "SELECT TEST_TYPE_ID" +
                             " FROM TEST_TYPE WITH(NOLOCK)" +
                             string.Format(" WHERE (TEST_TYPE_NAME='{0}')", testTypeName);

                int count = 0;
                int readedTestTypeID = -1;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedTestTypeID = Convert.ToInt32(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //тест с именем testTypeName не существует
                        break;

                    case 1:
                        //один тест найден
                        result = readedTestTypeID;
                        break;

                    default:
                        //считано более одной записи для testTypeName
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("TEST_TYPE.TEST_TYPE_NAME=", testTypeName)));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        public static int ConditionIDByName(string conditionName)
        {
            //чтение поля CONDITIONS.COND_ID по принятому обозначению условия conditionName
            //возвращает:
            //-1  - условие с именем conditionName не зарегистрировано в справочнике условий CONDITIONS;
            //больше чем -1 - значение CONDITIONS.COND_ID по принятому обозначению условия conditionName
            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = "SELECT COND_ID" +
                             " FROM CONDITIONS WITH(NOLOCK)" +
                             string.Format(" WHERE (COND_NAME='{0}')", conditionName);

                int count = 0;
                int readedCondID = -1;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedCondID = Convert.ToInt32(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //условие с именем conditionName не существует
                        break;

                    case 1:
                        //одно условие найдено
                        result = readedCondID;
                        break;

                    default:
                        //считано более одной записи для conditionName
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("CONDITIONS.COND_NAME=", conditionName)));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        public static int ParamIDByName(string paramName)
        {
            //чтение поля PARAMS.PARAM_ID по принятому обозначению параметра paramName
            //возвращает:
            //-1  - параметр с именем paramName не зарегистрирован в справочнике параметров PARAMS;
            //больше чем -1 - значение PARAMS.PARAM_ID по принятому обозначению параметра paramName
            int result = -1;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = "SELECT PARAM_ID" +
                             " FROM PARAMS WITH(NOLOCK)" +
                             string.Format(" WHERE (PARAM_NAME='{0}')", paramName);

                int count = 0;
                int readedParamID = -1;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedParamID = Convert.ToInt32(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //параметр с именем paramName не существует
                        break;

                    case 1:
                        //один параметр найден
                        result = readedParamID;
                        break;

                    default:
                        //считано более одной записи для paramName
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("PARAMS.PARAM_NAME=", paramName)));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return result;
        }

        public static string DeviceCodeByDevID(int devID)
        {
            //чтение поля Devices.Code по принятому идентификатору изделия devID
            //возвращает:
            //null  - изделие devID не зарегистрировано;
            //не null - значение Devices.Code по принятому идентификатору изделия devID
            string deviceCode = null;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = @"SELECT CODE
                               FROM DEVICES WITH(NOLOCK)
                               WHERE (DEV_ID=@DevID)";

                int count = 0;
                string readedDeviceCode = null;

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];

                    while (reader.Read())
                    {
                        reader.GetValues(values);

                        readedDeviceCode = Convert.ToString(values[0]);
                        count++;
                    }
                }
                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //изделия с идентификатором devID не существует
                        break;

                    case 1:
                        //одно изделие найдено
                        deviceCode = readedDeviceCode;
                        break;

                    default:
                        //считано более одной записи для devID
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("DEVICES.DEV_ID=", devID.ToString())));
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return deviceCode;
        }

        public static string DeviceCodeByDevID(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //чтение поля Devices.Code по принятому идентификатору изделия devID
            //возвращает:
            //null  - изделие devID не зарегистрировано;
            //не null - значение Devices.Code по принятому идентификатору изделия devID
            string deviceCode = null;

            string sql = @"SELECT CODE
                           FROM DEVICES WITH(NOLOCK)
                           WHERE (DEV_ID=@DevID)";

            int count = 0;
            string readedDeviceCode = null;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(values);

                    readedDeviceCode = Convert.ToString(values[0]);
                    count++;
                }
            }
            finally
            {
                reader.Close();
            }

            switch (count)
            {
                case 0:
                    //изделия с идентификатором devID не существует
                    break;

                case 1:
                    //одно изделие найдено
                    deviceCode = readedDeviceCode;
                    break;

                default:
                    //считано более одной записи для devID
                    throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("DEVICES.DEV_ID=", devID.ToString())));
            }

            return deviceCode;
        }

        public static bool IsDeviceCreatedManually(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //проверка принятого devID на способ создания:
            //возвращает:
            // true - изделие создано вручную;
            // false - изделие создано КИПом
            bool result = false;

            string sql = @"SELECT MME_CODE
                           FROM DEVICES WITH(NOLOCK)
                           WHERE (DEV_ID=@DevID)";

            int count = 0;
            string readedMMECode = null;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(values);
                    readedMMECode = values[0].ToString();

                    count++;
                }
            }
            finally
            {
                reader.Close();
            }

            switch (count)
            {
                case 0:
                    //искомое изделие не найдено
                    break;

                case 1:
                    //одно изделие найдено
                    result = (readedMMECode == cManually);
                    break;

                default:
                    //считано более одной записи
                    string identifiers = string.Format("devID={0}", devID);

                    throw new Exception(string.Concat("DbRoutines.IsDeviceCreatedManually. ", string.Format(cReadedRecordNotSingle, count, identifiers)));
            }

            return result;
        }

        public static string ProfileGUIDByDevice(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //чтение GUID профиля по принятому devID
            string result = null;

            string sql = @"SELECT PROFILE_ID
                           FROM DEVICES WITH(NOLOCK)
                           WHERE (DEV_ID=@DevID)";

            int count = 0;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(values);
                    result = values[0].ToString();

                    count++;
                }
            }
            finally
            {
                reader.Close();
            }

            if (count > 1)
            {
                //считано более одной записи
                string identifiers = string.Format("devID={0}", devID);

                throw new Exception(string.Concat("DbRoutines.ProfileGUIDByDevice. ", string.Format(cReadedRecordNotSingle, count, identifiers)));
            }

            return result;
        }

        public static bool IsDeviceExist(SqlConnection connection, SqlTransaction transaction, int devID)
        {
            //ищет в БД изделие (идентификатор результата измерений) с идентификатором devID
            //возвращает:
            // true - идентификатор изделия найден в БД;
            // false -  идентификатор изделия отсутствует в БД;

            string sql = @"SELECT COUNT(*)
                           FROM DEVICES WITH(NOLOCK)
                           WHERE (DEV_ID=@DEV_ID)";

            int readedCount = 0;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DEV_ID", SqlDbType.Int).Value = devID;

            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(values);
                    readedCount = Convert.ToInt32(values[0]);
                }
            }
            finally
            {
                reader.Close();
            }

            //всегда будет прочитана одна запись
            return (readedCount > 0);
        }

        public static int IsDeviceExist(SqlConnection connection, SqlTransaction transaction, int groupID, string code, string profileGUID)
        {
            //ищет в БД изделие (идентификатор результата измерений)
            //возвращает:
            // -1 - изделие в БД отсутствует;
            // не -1 - идентификатор найденного изделия
            int result = -1;

            string sql = @"SELECT DEV_ID
                           FROM DEVICES WITH(NOLOCK)
                           WHERE (
                                  (GROUP_ID=@GROUP_ID) AND
                                  (CODE=@CODE) AND
                                  (PROFILE_ID=@PROFILE_ID)
                                 )";

            int count = 0;
            int readedDevId = -1;

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@GROUP_ID", SqlDbType.Int).Value = groupID;
            command.Parameters.Add("@CODE", SqlDbType.NVarChar).Value = code;
            command.Parameters.Add("@PROFILE_ID", SqlDbType.UniqueIdentifier).Value = new Guid(profileGUID);

            SqlDataReader reader = command.ExecuteReader();

            try
            {
                object[] values = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(values);

                    readedDevId = Convert.ToInt32(values[0]);
                    count++;
                }
            }
            finally
            {
                reader.Close();
            }

            switch (count)
            {
                case 0:
                    //искомое изделие не найдено
                    break;

                case 1:
                    //одно изделие найдено
                    result = readedDevId;
                    break;

                default:
                    //считано более одной записи
                    string identifiers = string.Format("groupID={0}, code={1}, profileGUID={2}", groupID, code, profileGUID);

                    throw new Exception(string.Concat("DbRoutines.IsDeviceExist. ", string.Format(cReadedRecordNotSingle, count, identifiers)));
            }

            return result;
        }

        public static int CreateDevice(SqlConnection connection, SqlTransaction transaction, int groupID, string profileGUID, string code, string usr)
        {
            int devId = -1;

            SqlCommand command = new SqlCommand("CreateDevice", connection)
            {
                Transaction = transaction,
                CommandType = CommandType.StoredProcedure
            };

            SqlParameter outputParameter = command.Parameters.Add("@DEV_ID", SqlDbType.Int);
            outputParameter.Direction = ParameterDirection.Output;

            command.Parameters.Add("@GROUP_ID", SqlDbType.Int).Value = groupID;
            command.Parameters.Add("@PROFILE_ID", SqlDbType.UniqueIdentifier).Value = new Guid(profileGUID);
            command.Parameters.Add("@CODE", SqlDbType.NVarChar).Value = code;
            command.Parameters.Add("@USR", SqlDbType.NVarChar).Value = usr;
            command.Parameters.Add("@MME_CODE", SqlDbType.NVarChar).Value = cManually;

            command.ExecuteNonQuery();

            devId = (int)outputParameter.Value;

            return devId;
        }

        public static void UpdateDeviceCode(SqlConnection connection, SqlTransaction transaction, int devID, string code)
        {
            string sql = @"UPDATE DEVICES
                           SET CODE=@Code
                           WHERE (DEV_ID=@DevID)";

            SqlCommand command = new SqlCommand(sql, connection)
            {
                Transaction = transaction
            };

            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
            command.Parameters.Add("@Code", SqlDbType.NVarChar).Value = code;
            command.ExecuteNonQuery();
        }

        /*
        public static int InsertToDevices(int groupID, string profileGUID, string code, string usr, bool? sapID)
        {
            //создание новой записи в таблице DEVICES
            //значение поля MME_CODE устанавливается равным cManually
            int devId;

            SqlConnection connection = DBConnections.Connection;

            string sql = "INSERT INTO DEVICES(GROUP_ID, PROFILE_ID, CODE, TS, USR, POS, MME_CODE, SAPID)" +
                         " OUTPUT INSERTED.DEV_ID VALUES(@GroupID, @ProfileID, @Code, @Ts, @Usr, @Pos, @MMECode, @SapID)";

            SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@GroupID", SqlDbType.Int).Value = groupID;
            command.Parameters.Add("@ProfileID", SqlDbType.UniqueIdentifier).Value = new Guid(profileGUID);
            command.Parameters.Add("@Code", SqlDbType.NVarChar).Value = code;
            command.Parameters.Add("@Ts", SqlDbType.DateTime).Value = DateTime.Now;
            command.Parameters.Add("@Usr", SqlDbType.NVarChar).Value = usr;
            command.Parameters.Add("@Pos", SqlDbType.Bit).Value = 0;
            command.Parameters.Add("@MMECode", SqlDbType.NVarChar).Value = cManually;

            SqlParameter sqlParameter = command.Parameters.Add("@SapID", SqlDbType.Bit);

            if (sapID == null)
            {
                sqlParameter.Value = DBNull.Value;
            }
            else
                sqlParameter.Value = (bool)sapID;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                //считываем идентификатор созданного параметра
                devId = (int)command.ExecuteScalar();
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return devId;
        }
        */

        public delegate void AddProfileByGroupName(System.Collections.IList listForDataFill, string profGUID, int profID, string profName);
        public static void ProfilesByGroupName(string groupName, System.Collections.IList listForDataFill, AddProfileByGroupName addProfileByGroupNameHandler)
        {
            //по принятому обозначению ПЗ groupName формирует список профилей которые упоминаются в таблице Devices по ПЗ с обозначением groupName
            if ((!string.IsNullOrEmpty(groupName)) && (addProfileByGroupNameHandler != null))
            {
                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    string sql = @"SELECT DISTINCT D.PROFILE_ID, P.PROF_ID, P.PROF_NAME
                                   FROM DEVICES D WITH(NOLOCK)
                                    INNER JOIN GROUPS G WITH(NOLOCK) ON (D.GROUP_ID=G.GROUP_ID)
                                    INNER JOIN PROFILES P WITH(NOLOCK) ON (
                                                                           (D.PROFILE_ID=P.PROF_GUID) AND
                                                                           (ISNULL(P.IS_DELETED, 0)=0)
                                                                          )
                                   WHERE (G.GROUP_NAME=@GroupName)
                                   ORDER BY P.PROF_NAME";

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@GroupName", SqlDbType.NChar).Value = groupName;
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        int index;

                        string profGUID;
                        int profID;
                        string profName;

                        while (reader.Read())
                        {
                            index = reader.GetOrdinal("PROFILE_ID");
                            profGUID = reader.GetGuid(index).ToString();

                            index = reader.GetOrdinal("PROF_ID");
                            profID = reader.GetInt32(index);

                            index = reader.GetOrdinal("PROF_NAME");
                            profName = reader.GetString(index);

                            addProfileByGroupNameHandler.Invoke(listForDataFill, profGUID, profID, profName);
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }
                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }

        public static int ProfileIDMaxVersionByProfileName(string profileName)
        {
            //вычисление идентификатора профиля самой последней версии по обозначению профиля
            //возвращает:
            // -1 - для принятого profileName не найден ProfileID;
            // иначе возвращает идентификатор профиля с обозначением profileName последней версии
            int profileID = -1;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = string.Format("SELECT MAX(PROF_ID)" +
                                           " FROM PROFILES WITH(NOLOCK)" +
                                           " WHERE (" +
                                           "        (PROF_NAME='{0}') AND" +
                                           "        ISNULL(IS_DELETED, 0)=0" +
                                           "       )", profileName);

                int count = 0;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];
                    object obj;

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        obj = values[0];

                        if (obj != DBNull.Value)
                            profileID = Convert.ToInt32(obj);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //профиля с принятым наименованием profileName не существует
                        break;

                    case 1:
                        //найден один профиль
                        break;

                    default:
                        //считано более одной записи для profileName
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("profileName=", profileName)));
                }
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return profileID;
        }

        public static string ProfileGUIDByProfileID(int profileID)
        {
            //вычисление ProfileGUID по идентификатору профиля profileID
            //возвращает:
            // null - принятого profileID не найдено в базе данных
            // иначе возвращает ProfileGUID
            string profileGUID = null;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = string.Format("SELECT PROF_GUID" +
                                           " FROM PROFILES WITH(NOLOCK)" +
                                           " WHERE " +
                                           "       (PROF_ID={0})" +
                                           "       ", profileID.ToString());

                int count = 0;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];
                    object obj;

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        obj = values[0];

                        if (obj != DBNull.Value)
                            profileGUID = Convert.ToString(obj);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //профиля с принятым идентификатором profileID не существует
                        break;

                    case 1:
                        //найден один профиль
                        break;

                    default:
                        //считано более одной записи для profileID
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("profileID=", profileID.ToString())));
                }
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return profileGUID;
        }

        public static int ProfileIDByProfileGUID(string profileGUID)
        {
            //вычисляет идентификатор профиля по его GUID
            //возвращает:
            // -1 - принятый profileGUID не найден в базе данных
            // иначе возвращает ProfileID
            int profileID = -1;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = string.Format(@"SELECT PROF_ID
                                             FROM PROFILES WITH(NOLOCK)
                                             WHERE
                                                   (PROF_GUID='{0}')"
                                                    , profileGUID);

                int count = 0;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];
                    object obj;

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        obj = values[0];

                        if (obj != DBNull.Value)
                            profileID = Convert.ToInt32(obj);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //профиля с принятым идентификатором profileGUID не существует
                        break;

                    case 1:
                        //найден один профиль
                        break;

                    default:
                        //считано более одной записи для profileGUID
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("profileGUID=", profileGUID)));
                }
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return profileID;
        }

        public static string ProfileNameByProfileGUID(string profileGUID)
        {
            //вычисляет обозначение профиля по его GUID
            //возвращает:
            // null - принятый profileGUID не найден в базе данных
            // иначе возвращает ProfileName
            string profileName = null;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = string.Format(@"SELECT PROF_NAME
                                             FROM PROFILES WITH(NOLOCK)
                                             WHERE
                                                   (PROF_GUID='{0}')", profileGUID);

                int count = 0;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];
                    object obj;

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        obj = values[0];

                        if (obj != DBNull.Value)
                            profileName = Convert.ToString(obj);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //профиля с принятым идентификатором profileGUID не существует
                        break;

                    case 1:
                        //найден один профиль
                        break;

                    default:
                        //считано более одной записи для profileGUID
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("profileGUID=", profileGUID)));
                }
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return profileName;
        }

        public static string ProfileGUIDByProfileName(string profileName)
        {
            //вычисляет GUID действующего профиля последней (наибольшей) версии по обозначению профиля profileName
            //возвращает:
            // null - принятый profileName не найден в базе данных
            // иначе возвращает ProfileGUID
            string profileGUID = null;

            int profileID = ProfileIDMaxVersionByProfileName(profileName);

            if (profileID != -1)
                profileGUID = ProfileGUIDByProfileID(profileID);

            return profileGUID;
        }

        public static string DeviceTypeRuByProfileName(string profileName)
        {
            //вычисление типа изделия записанного только русскими символами по коду профиля profileName
            string deviceType = null;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = string.Format("SELECT [dbo].DeviceTypeRuByProfileName('{0}') AS DeviceTypeRu", profileName);

                int count = 0;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];
                    object obj;

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        obj = values[0];

                        if (obj != DBNull.Value)
                            deviceType = Convert.ToString(obj);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //скалярная функция ничего не вернула
                        break;

                    case 1:
                        //тип изделия успешно вычислен
                        break;

                    default:
                        //считано более одной записи для profileName
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("profileName=", profileName)));
                }
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return deviceType;
        }

        public static string ProfileBodyByProfileName(string profileName)
        {
            //вычисление тела профиля по обозначению профиля
            string profileBody = null;

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                string sql = string.Format("SELECT [dbo].ProfileBodyByProfileName('{0}') AS ProfileBody", profileName);

                int count = 0;

                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    object[] values = new object[reader.FieldCount];
                    object obj;

                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        obj = values[0];

                        if (obj != DBNull.Value)
                            profileBody = Convert.ToString(obj);

                        count++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                switch (count)
                {
                    case 0:
                        //скалярная функция ничего не вернула
                        break;

                    case 1:
                        //тип изделия успешно вычислен
                        break;

                    default:
                        //считано более одной записи для profileName
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Concat("profileName=", profileName)));
                }
            }
            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }

            return profileBody;
        }

        public static string SqlParameterEqualNull(string fieldName, string parameterName, object value)
        {
            //сравнение вида fieldName=NULL SQL сервер обрабатывает не корректно, ему для корректного сравнения надо fieldName IS NULL
            //принятый parameterName должен начинатся с @
            return string.Concat("(", fieldName, DBNull.Value.Equals(value) ? " IS NULL" : string.Concat("=", parameterName), ")");
        }

        /*
        //описание статуса сборки по протоколу испытаний как он есть в справочнике базы данных
        public class AssemblyStatus
        {
            //значение идентификатора статуса сборки по протоколу испытаний
            private byte FAssemblyStatusID;
            public byte AssemblyStatusID
            {
                get { return this.FAssemblyStatusID; }
                set { this.FAssemblyStatusID = value; }
            }

            //значение обозначения статуса сборки по протоколу испытаний
            private string FDescr;
            public string Descr
            {
                get { return this.FDescr; }
                set { this.FDescr = value; }
            }
        }

        public static void LoadAssemblyStatuses(List<AssemblyStatus> assemblyStatusList)
        {
            //загрузка списка статусов сборки из справочника AssemblyStatuses базы данных
            if (assemblyStatusList == null)
                throw new Exception("assemblyStatusList=null. Waiting not null assemblyStatusList.");

            string sql = @"SELECT ASSEMBLYSTATUSID, DESCR
                           FROM ASSEMBLYSTATUSES WITH(NOLOCK)
                           ORDER BY ASSEMBLYSTATUSID";

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    int ordinalAssemblyStatusID = reader.GetOrdinal("ASSEMBLYSTATUSID");
                    int ordinalDescr = reader.GetOrdinal("DESCR");

                    assemblyStatusList.Clear();

                    while (reader.Read())
                    {
                        AssemblyStatus assemblyStatus = new AssemblyStatus()
                        {
                            AssemblyStatusID = reader.GetByte(ordinalAssemblyStatusID),
                            Descr = reader.GetString(ordinalDescr)
                        };

                        assemblyStatusList.Add(assemblyStatus);
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }
        */

        //описание параметра как он есть в справочнике базы данных
        public class Parameter : INotifyPropertyChanged
        {
            //значение идентификатора параметра до его редактирования пользователем
            private int? FOldParamID = null;
            public int? OldParamID
            {
                get { return this.FOldParamID; }
                set { this.FOldParamID = value; }
            }

            //значение идентификатора параметра после его редактирования пользователем
            private int FParamID;
            public int ParamID
            {
                get { return this.FParamID; }

                set
                {
                    //происходит инициализация измеряемого параметра - запоминаем этот идентификатор, если место его хранения ещё не занято
                    if (this.FOldParamID == null)
                        this.FOldParamID = value;

                    this.FParamID = value;
                    OnPropertyChanged("ParamID");
                }
            }

            private string FParamName;
            public string ParamName
            {
                get { return this.FParamName; }

                set
                {
                    this.FParamName = value;
                    OnPropertyChanged("ParamName");
                }
            }

            private string FParamUm;
            public string ParamUm
            {
                get { return this.FParamUm; }

                set
                {
                    this.FParamUm = value;
                    OnPropertyChanged("ParamUm");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(string info)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(info));
                }
            }
        }

        //для возможности вызывать реализацию Dictionaries.ParameterName объявляем delegate
        //public delegate string DelegateParameterName(string parameterName);
        public static void LoadParameters(ObservableCollection<Parameter> parameterList)
        {
            //загрузка списка параметров из справочника параметров PARAMS базы данных как есть без преобразования имён параметров
            if (parameterList == null)
                throw new Exception("parameters=null. Waiting not null parameters.");

            parameterList.Clear();

            string sql = "SELECT PARAM_ID, PARAM_NAME, PARAMUM" +
                         " FROM PARAMS WITH(NOLOCK)" +
                         " WHERE (PARAM_IS_HIDE=0)" +
                         " ORDER BY PARAM_NAME";

            SqlConnection connection = DBConnections.Connection;

            bool connectionOpened = false;

            if (!IsDBConnectionAlive(connection))
            {
                connection.Open();
                connectionOpened = true;
            }

            try
            {
                SqlCommand command = new SqlCommand(sql, connection);

                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        Parameter parameter = new Parameter()
                        {
                            ParamID = (int)reader["PARAM_ID"],
                            ParamName = reader["PARAM_NAME"].ToString().TrimEnd(),
                            ParamUm = (reader["PARAMUM"] == DBNull.Value) ? null : reader["PARAMUM"].ToString()
                        };

                        parameterList.Add(parameter);
                    }
                }

                finally
                {
                    reader.Close();
                }
            }

            finally
            {
                //если данная реализация открыла соединение к БД, то она же его должна закрыть
                //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                if (connectionOpened)
                    connection.Close();
            }
        }

        //измереннный параметр
        public class MeasuredParameter : Parameter, INotifyPropertyChanged
        {
            //ссылка на коллекцию, которая владеет данным параметром
            private CollectionOfMeasuredParameter FOwner = null;
            public CollectionOfMeasuredParameter Owner
            {
                get { return FOwner; }
                set { this.FOwner = value; }
            }

            //это служебный реквизит, его значение никогда не будет отображаться в интерфейсе пользователя
            //нужен для оптимизации сохранения в базу данных - помогает найти отредактированные пользователем параметры
            //представляет собой строку из значений public propertys this
            private string FLoadedImprint = null;
            private string LoadedImprint
            {
                get { return this.FLoadedImprint; }
            }

            //это служебный реквизит, его значение никогда не будет отображаться в интерфейсе
            //не Null - описание данного параметра существует в базе данных, оно редактировалось пользователем вручную;
            //Null - описание данного параметра нет в базе данных, пользователь определил значение этого параметра в интерфейсе ручного ввода
            private int? FDevParamID = null;
            public int? DevParamID
            {
                get { return this.FDevParamID; }
                set { this.FDevParamID = value; }
            }

            //значение измеренного параметра до его редактирования пользователем
            private decimal? FOldParamValue = null;
            public decimal? OldParamValue
            {
                get { return this.FOldParamValue; }
                set { this.FOldParamValue = value; }
            }

            private decimal? FParamValue;
            public decimal? ParamValue
            {
                get { return this.FParamValue; }

                set
                {
                    //происходит инициализация значения измеряемого параметра - запоминаем это значение, если место его хранения ещё не занято
                    if (this.FOldParamValue == null)
                        this.FOldParamValue = value;

                    this.FParamValue = value;
                    OnPropertyChanged("ParamValue");
                }
            }

            private string GetImprint()
            {
                //вычисление отпечатка себя
                string result = string.Empty;

                foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
                {
                    var value = propertyInfo.GetValue(this, null);
                    string sValue = (value == null) ? "null" : value.ToString();

                    result = string.Concat(result, sValue);
                }

                return result;
            }

            public void CalcImprint()
            {
                this.FLoadedImprint = this.GetImprint();
            }

            private void InsertToDB(int devID)
            {
                //вставка параметра новой записи в таблицу DEV_PARAM
                //если в выпадающем списке интерфеса выбора параметра ничего не выбрать, а только его открыть - софт создаст в списке новый параметр с идентификатором равным ноль
                //значение параметра не может быть null. созданный пользователем параметр всегда будет иметь DevParamID=null
                if ((this.DevParamID == null) && (this.ParamID != 0) && (this.ParamValue != null))
                {
                    SqlConnection connection = DBConnections.Connection;

                    string sql = "INSERT INTO DEV_PARAM(DEV_ID, TEST_TYPE_ID, PARAM_ID, VALUE)" +
                                 " VALUES(@Dev_ID, @TestTypeID, @ParamID, @Value)";

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@Dev_ID", SqlDbType.Int).Value = devID;
                    command.Parameters.Add("@TestTypeID", SqlDbType.Int).Value = this.Owner.TestTypeID;
                    command.Parameters.Add("@ParamID", SqlDbType.Int).Value = this.ParamID;
                    command.Parameters.Add("@Value", SqlDbType.Decimal).Value = this.ParamValue;

                    bool connectionOpened = false;

                    if (!IsDBConnectionAlive(connection))
                    {
                        connection.Open();
                        connectionOpened = true;
                    }

                    try
                    {
                        command.ExecuteNonQuery();
                    }

                    finally
                    {
                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                        //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                        if (connectionOpened)
                            connection.Close();
                    }

                    //считываем идентификатор DEV_PARAM_ID только что созданного места хранения. считаеть его используя OUTPUT INSERTED.DEV_PARAM_ID нельзя, т.к. в таблице DEV_PARAM есть триггеры,
                    //использование конструкции OUTPUT INSERTED.DEV_PARAM INTO @TABLE VALUES ... предполагает создание таблицы - что не лучше чем тупо считать идентификатор обычным способом
                    this.DevParamID = this.DevParamIDFromDB(devID, this.ParamID, this.ParamValue, this.Owner.TestTypeID);
                }
            }

            private void UpdateInDB()
            {
                //редактирование параметра в таблице DEV_PARAM выполняем при ParamID не равным нулю и если отпечаток параметра не совпадает с отпечатком, вычисленным для него же при загрузке из базы данных
                if ((this.ParamID != 0) && (this.LoadedImprint != this.GetImprint()))
                {
                    SqlConnection connection = DBConnections.Connection;

                    string sql = "UPDATE DEV_PARAM" +
                                 " SET PARAM_ID=@ParamID," +
                                 "     VALUE=@Value" +
                                 " WHERE (DEV_PARAM_ID=@DevParamID)";

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@ParamID", SqlDbType.Int).Value = this.ParamID;
                    command.Parameters.Add("@Value", SqlDbType.Decimal).Value = this.ParamValue;
                    command.Parameters.Add("@DevParamID", SqlDbType.Int).Value = (int)this.DevParamID;

                    bool connectionOpened = false;

                    if (!IsDBConnectionAlive(connection))
                    {
                        connection.Open();
                        connectionOpened = true;
                    }

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    finally
                    {
                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                        //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                        if (connectionOpened)
                            connection.Close();
                    }
                }
            }

            private void DeleteFromDB()
            {
                //удаление параметра из таблицы DEV_PARAM
                if ((this.DevParamID != null) && (this.ParamID == 0))
                {
                    SqlConnection connection = DBConnections.Connection;

                    string sql = "DELETE" +
                                 " FROM DEV_PARAM" +
                                 " WHERE (DEV_PARAM_ID=@DevParamID)";

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@DevParamID", SqlDbType.Int).Value = (int)this.DevParamID;

                    bool connectionOpened = false;

                    if (!IsDBConnectionAlive(connection))
                    {
                        connection.Open();
                        connectionOpened = true;
                    }

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    finally
                    {
                        //если данная реализация открыла соединение к БД, то она же его должна закрыть
                        //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                        if (connectionOpened)
                            connection.Close();
                    }
                }
            }

            private bool ExistInDB(int devID, int testTypeID)
            {
                //проверка наличия места хранения для данного измеренного параметра в тесте testTypeID изделия devID
                switch (this.DevParamID)
                {
                    case null:
                        //место хранения данного измеренного параметра отсутствует
                        return false;

                    default:
                        //проверяем наличие значения this.DevParamID в базе данных для принятых devID и testTypeID
                        bool result = false;

                        string sql = "SELECT DEV_PARAM_ID" +
                                     " FROM DEV_PARAM WITH(NOLOCK)" +
                                     " WHERE (" +
                                     "        (DEV_PARAM_ID=@DevParamID) AND" +
                                     "        (DEV_ID=@DevID) AND" +
                                     "        (TEST_TYPE_ID=@TestTypeID)" +
                                     "       )";

                        SqlConnection connection = DBConnections.Connection;

                        bool connectionOpened = false;

                        if (!IsDBConnectionAlive(connection))
                        {
                            connection.Open();
                            connectionOpened = true;
                        }

                        try
                        {
                            SqlCommand command = new SqlCommand(sql, connection);
                            command.Parameters.Add("@DevParamID", SqlDbType.Int).Value = (int)this.DevParamID;
                            command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
                            command.Parameters.Add("@TestTypeID", SqlDbType.Int).Value = testTypeID;

                            SqlDataReader reader = command.ExecuteReader();

                            try
                            {
                                //ожидаем одну единственную запись, либо ни одной записи
                                while (reader.Read())
                                {
                                    result = true;
                                    break;
                                }
                            }

                            finally
                            {
                                reader.Close();
                            }
                        }

                        finally
                        {
                            //если данная реализация открыла соединение к БД, то она же его должна закрыть
                            //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                            if (connectionOpened)
                                connection.Close();
                        }

                        return result;
                }
            }

            private int? DevParamIDFromDB(int devID, int paramID, decimal? paramValue, int testTypeID)
            {
                //чтение идентификатора места хранения DevParamID по принятым devID, testTypeID
                string sql = "SELECT DEV_PARAM_ID" +
                             " FROM DEV_PARAM WITH(NOLOCK)" +
                             " WHERE (" +
                             "        (DEV_ID=@DevID) AND" +
                             "        (PARAM_ID=@ParamID) AND" +
                             "        (VALUE=@Value) AND" +
                             "        (TEST_TYPE_ID=@TestTypeID)" +
                             "       )";

                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                int count = 0;
                int? readedDevParamID = null;

                try
                {
                    SqlCommand command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
                    command.Parameters.Add("@ParamID", SqlDbType.Int).Value = paramID;
                    command.Parameters.Add("@Value", SqlDbType.Decimal).Value = paramValue;
                    command.Parameters.Add("@TestTypeID", SqlDbType.Int).Value = testTypeID;

                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        object[] values = new object[reader.FieldCount];
                        object obj;

                        while (reader.Read())
                        {
                            reader.GetValues(values);
                            obj = values[0];

                            if (obj != DBNull.Value)
                                readedDevParamID = Convert.ToInt32(values[0]);

                            count++;
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }

                switch (count)
                {
                    case 0:
                        //места хранения не найдено
                        return null;

                    case 1:
                        //одно место хранения найдено
                        return readedDevParamID;

                    default:
                        //найдено более одного места хранения
                        throw new Exception(string.Format(cReadedRecordNotSingle, count.ToString(), string.Format("devID='{0}', paramID='{1}', paramValue={2}, testTypeID='{3}'", devID, paramID, paramValue, testTypeID)));
                }
            }

            public void SaveToDB(int devID, int testTypeID)
            {
                //сохранение параметра в БД. данная реализация самостоятельно разбирается что надо делать чтобы сохраненить параметр в БД
                //в одном и том же тесте не может быть двух и более параметров с одним и тем же именем
                //смотрим с чем сы имеем дело: либо это работа с текущими данными (itIsCopy=false), либо это копирование параметров из одного изделия в другое (itIsCopy=true)
                switch (this.ExistInDB(devID, testTypeID))
                {
                    case false:
                        //имеем дело с копированием параметров из одного изделия в другое
                        //место хранения не существует - очистим this.DevParamID
                        this.DevParamID = null;
                        break;

                    default:
                        //это работа с текущими данными того изделия, с которым работает пользователь
                        break;
                }

                switch (this.DevParamID == null)
                {
                    case true:
                        //данный параметр отсутствует в тесте testTypeID, требуется вставка параметра в БД
                        this.InsertToDB(devID);
                        break;

                    default:
                        //DevParamID не null
                        switch (this.ParamID)
                        {
                            case 0:
                                //если ParamID=0 - это случай удаления из базы данных
                                this.DeleteFromDB();
                                break;

                            default:
                                //ParamID не равен 0 - значит происходит изменение параметра, требуется его редактирование в БД                                
                                if (this.OldParamID != null)
                                {
                                    //уточняем место хранения редактируемого параметра
                                    this.DevParamID = this.DevParamIDFromDB(devID, (int)this.OldParamID, this.OldParamValue, testTypeID);
                                    this.UpdateInDB();

                                    //изменения успешно сохранениы в базу данных - разрешаем запись в this.OldParamID, this.OldParamValue и переписываем в Old успешно сохранённые значения
                                    this.OldParamID = null;
                                    this.ParamID = this.ParamID;

                                    this.OldParamValue = null;
                                    this.ParamValue = this.ParamValue;
                                }

                                break;
                        }

                        break;
                }
            }
        }

        //список измеренных параметров
        //public delegate string DelegateParameterNameFormattedValue(string parameterName, string value, out string formatValue);
        public class CollectionOfMeasuredParameter : ObservableCollection<MeasuredParameter>
        {
            //это служебный реквизит, его значение никогда не будет отображаться в интерфейсе пользователя
            private int FTestTypeID;
            public int TestTypeID
            {
                get { return this.FTestTypeID; }
                set { this.FTestTypeID = value; }
            }

            public CollectionOfMeasuredParameter(int testTypeID)
            {
                this.TestTypeID = testTypeID;
            }

            public new void Add(MeasuredParameter item)
            {
                item.Owner = this;
                base.Add(item);
            }

            public void Load(int devID)
            {
                //загрузка из базы данных списка параметров изделия this.DevID в тесте с идентификатором this.TestTypeID
                this.Clear();

                string sql = "SELECT DP.DEV_PARAM_ID, DP.PARAM_ID, DP.VALUE, P.PARAM_NAME, P.PARAMUM" +
                             " FROM DEV_PARAM DP WITH(NOLOCK)" +
                             "  INNER JOIN PARAMS P WITH(NOLOCK) ON (" +
                             "                                       (DP.PARAM_ID=P.PARAM_ID) AND" +
                             "                                       (ISNULL(P.PARAM_IS_HIDE, 0)=0)" +
                             "                                      )" +
                             " WHERE (" +
                             "        (DEV_ID=@DevID) AND" +
                             "        (TEST_TYPE_ID=@TestTypeID)" +
                             "       )";

                SqlConnection connection = DBConnections.Connection;

                bool connectionOpened = false;

                if (!IsDBConnectionAlive(connection))
                {
                    connection.Open();
                    connectionOpened = true;
                }

                try
                {
                    SqlCommand command = new SqlCommand(sql, connection);

                    command.Parameters.Add("@DevID", SqlDbType.Int).Value = devID;
                    command.Parameters.Add("@TestTypeID", SqlDbType.Int).Value = this.TestTypeID;

                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        while (reader.Read())
                        {
                            MeasuredParameter parameter = new MeasuredParameter()
                            {
                                DevParamID = (int)reader["DEV_PARAM_ID"],
                                ParamID = (int)reader["PARAM_ID"],
                                ParamName = reader["PARAM_NAME"].ToString().TrimEnd(),
                                ParamUm = (reader["PARAMUM"] == DBNull.Value) ? null : reader["PARAMUM"].ToString(),
                                ParamValue = (decimal)reader["VALUE"]
                            };
                            this.Add(parameter);

                            //сразу после загрузки параметра из базы данных фиксируем в нём его его отпечаток. чтобы Owner был зафиксирован в отпечатке не равным null - делаем фиксацию отпечатка после добавления данного измеренного параметра в this
                            parameter.CalcImprint();
                        }
                    }

                    finally
                    {
                        reader.Close();
                    }
                }

                finally
                {
                    //если данная реализация открыла соединение к БД, то она же его должна закрыть
                    //если соединение к БД было открыто вызывающей реализацией - не закрываем его в данной реализации
                    if (connectionOpened)
                        connection.Close();
                }
            }
        }
    }
}
