using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Text;
using SCME.InterfaceImplementations;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.Interfaces;

namespace SCME.Logger
{
    public class ResultsJournal
    {
        private static readonly object ms_Locker = new object();
        private AfterSyncProfilesRoutine FAfterSyncProfilesRoutine;

        #region InsertCmds

        private const string SELECT_GROUPS_CMD =
            "SELECT DISTINCT G.GROUP_NAME FROM GROUPS G, DEVICES D WHERE G.GROUP_ID = D.GROUP_ID AND D.TS > @TS_FROM AND D.TS <= @TS_TO ORDER BY G.GROUP_NAME ASC";

        private const string SELECT_PROFILES_CMD = "SELECT P.PROF_NAME FROM PROFILES P";

        private const string SELECT_DEVICES_CMD =
            "SELECT D.DEV_ID, D.CODE, D.SIL_N_1, D.SIL_N_2, D.TS, D.USR, D.POS, P.PROF_NAME FROM DEVICES D, GROUPS G, PROFILES P WHERE D.GROUP_ID = G.GROUP_ID AND D.PROFILE_ID = P.PROF_ID AND G.GROUP_NAME = @GROUP_NAME ORDER BY D.CODE ASC";

        private const string SELECT_DEV_PARAMS_CMD =
            "SELECT P.PARAM_NAME AS P_NID, P.PARAM_NAME_LOCAL AS P_NAME, DP.VALUE AS P_VALUE, P.PARAM_IS_HIDE AS IS_HIDE FROM PARAMS P, DEV_PARAM DP WHERE DP.PARAM_ID = P.PARAM_ID AND DP.DEV_ID = @DEV_ID ORDER BY P.PARAM_ID ASC";

        private const string SELECT_DEV_NORM_CMD =
            "SELECT P.PARAM_NAME AS P_NID, PP.MIN_VAL AS P_MIN, PP.MAX_VAL AS P_MAX FROM PARAMS P, PROF_PARAM PP, DEVICES D WHERE PP.PARAM_ID = P.PARAM_ID AND PP.PROF_ID = D.PROFILE_ID AND D.DEV_ID = @DEV_ID";

        private const string SELECT_DEV_CONDS_CMD =
            "SELECT C.COND_NAME AS C_NID, C.COND_NAME_LOCAL AS C_NAME, PC.VALUE AS C_VALUE, C.COND_IS_TECH AS IS_TECH FROM CONDITIONS C, PROF_COND PC, DEVICES D WHERE PC.COND_ID = C.COND_ID AND PC.PROF_ID = D.PROFILE_ID AND D.DEV_ID = @DEV_ID ORDER BY C.COND_ID ASC";

        private const string SELECT_DEV_ERR_CMD =
            "SELECT E.ERR_CODE FROM ERRORS E, DEV_ERR DE WHERE E.ERR_ID = DE.ERR_ID AND DE.DEV_ID = @DEV_ID";

        private const string GROUP_DELETE_CMD = "DELETE FROM GROUPS G WHERE G.GROUP_NAME = @GROUP_NAME";
        private const string X_SELECT_CMD = "SELECT * FROM {0} T WHERE T.ROWID >= @KEY ORDER BY T.ROWID ASC LIMIT {1}";
        private const string TABLES_SELECT_CMD = "SELECT name FROM sqlite_master WHERE type = 'table'";
        private const string TABLES_INSERT_CMD = "INSERT INTO {0} VALUES({1})";



        private SQLiteConnection m_Connection;

        private SQLiteCommand m_GroupDeleteCommand;

        private SQLiteCommand m_SelectGroupsCommand,
                              m_SelectProfilesCommand,
                              m_SelectDevicesCommand,
                              m_SelectDevParamsCommand,
                              m_SelectDevCondsCommand,
                              m_SelectDevNormCommand,
                              m_SelectDevErrCommand;


        #endregion


        private IProfilesService _profilesService;
        private IResultsService _resultsService;
        private ISyncService _syncService;

        public void Open(string databasePath, string databaseOptions, string mmeCode = null)
        {
            if (!String.IsNullOrWhiteSpace(databasePath))
            {
                var connectionString = $"data source={databasePath};{databaseOptions}";
                _profilesService = new SQLiteProfilesService(connectionString);
                _resultsService = new SQLiteResultsServiceLocal(connectionString);
                _syncService = new SyncService(connectionString, mmeCode);

                m_Connection = new SQLiteConnection(connectionString, false);
                m_Connection.Open();

                PrepareWriteCommands();
                PrepareReadCommands();
            }
        }

        public void Close()
        {
            if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                m_Connection.Close();
            _profilesService.Dispose();
            _resultsService.Dispose();
            _syncService.Dispose();
        }

        #region Write implementation

        public void WriteResult(ResultItem result, IEnumerable<string> errors)
        {
            lock (ms_Locker)
            {
                _resultsService.WriteResults(result, errors);
            }
        }

        public void SaveProfiles(List<ProfileItem> profilesItems, string mmeCode)
        {
            lock (ms_Locker)
            {
                _profilesService.SaveProfilesFromMme(profilesItems, mmeCode);
            }
        }

        private void AfterSyncResultsHandler(string Error)
        {
            _syncService.SyncProfiles(this.FAfterSyncProfilesRoutine);
        }

        public void SyncWithServer(AfterSyncProfilesRoutine afterSyncProfilesRoutine)
        {
            //запоминаем что нам надо вызвать после того как будет выполнена синхронизация результатов измерений
            this.FAfterSyncProfilesRoutine = afterSyncProfilesRoutine;

            //последовательно синхронизируем данные: сначала вызываем синхронизацию результатов измерений, которая после своего исполнения вызовет синхронизацию профилей
            _syncService.SyncResults(AfterSyncResultsHandler);
        }

        public List<ProfileItem> GetProfiles()
        {
            try
            {
                return _profilesService.GetProfileItems();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Read report implementation

        public List<string> ReadGroups(DateTime? From, DateTime? To)
        {
            lock (ms_Locker)
            {
                var list = new List<string>();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {
                    m_SelectGroupsCommand.Parameters["@TS_FROM"].Value = From.HasValue
                        ? From.Value.ToString(@"yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                        : DateTime.MinValue.ToString(@"yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    m_SelectGroupsCommand.Parameters["@TS_TO"].Value = To.HasValue
                        ? To.Value.ToString(@"yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                        : DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);


                    using (var reader = m_SelectGroupsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(reader.GetString(0));
                    }
                }

                return list;
            }
        }

        public List<string> ReadProfiles()
        {
            lock (ms_Locker)
            {
                var list = new List<string>();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {
                    using (var reader = m_SelectProfilesCommand.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(reader.GetString(0));
                    }
                }

                return list;
            }
        }

        public List<DeviceItem> ReadDevices(string Group)
        {
            lock (ms_Locker)
            {
                var list = new List<DeviceItem>();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {
                    m_SelectDevicesCommand.Parameters["@GROUP_NAME"].Value = Group;

                    using (var reader = m_SelectDevicesCommand.ExecuteReader())
                    {
                        var ordID = reader.GetOrdinal("DEV_ID");
                        var ordCode = reader.GetOrdinal("CODE");
                        var ordSn1 = reader.GetOrdinal("SIL_N_1");
                        var ordSn2 = reader.GetOrdinal("SIL_N_2");
                        var ordPos = reader.GetOrdinal("POS");
                        var ordUser = reader.GetOrdinal("USR");
                        var ordTs = reader.GetOrdinal("TS");
                        var ordProf = reader.GetOrdinal("PROF_NAME");

                        while (reader.Read())
                            list.Add(new DeviceItem
                            {
                                InternalID = reader.GetInt64(ordID),
                                Code = reader.GetString(ordCode),
                                StructureOrd = reader.GetString(ordSn1),
                                StructureID = reader.GetString(ordSn2),
                                Position = reader.GetBoolean(ordPos) ? 2 : 1,
                                User = reader.GetString(ordUser),
                                Timestamp = DateTime.Parse(reader.GetString(ordTs), CultureInfo.InvariantCulture),
                                ProfileName = reader.GetString(ordProf)
                            });
                    }
                }

                return list;
            }
        }

        public List<ParameterItem> ReadDeviceParameters(long InternalID)
        {
            lock (ms_Locker)
            {
                var list = new List<ParameterItem>();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {
                    m_SelectDevParamsCommand.Parameters["@DEV_ID"].Value = InternalID;

                    using (var reader = m_SelectDevParamsCommand.ExecuteReader())
                    {
                        var ordNid = reader.GetOrdinal("P_NID");
                        var ordName = reader.GetOrdinal("P_NAME");
                        var ordValue = reader.GetOrdinal("P_VALUE");
                        var ordHide = reader.GetOrdinal("IS_HIDE");

                        while (reader.Read())
                            list.Add(new ParameterItem
                            {
                                Name = reader.GetString(ordNid),
                                NameLocal = reader.GetString(ordName),
                                Value = reader.GetFloat(ordValue),
                                IsHide = reader.GetBoolean(ordHide)
                            });
                    }
                }

                return list;
            }
        }

        public List<ConditionItem> ReadDeviceConditions(long InternalID)
        {
            lock (ms_Locker)
            {
                var list = new List<ConditionItem>();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {

                    m_SelectDevCondsCommand.Parameters["@DEV_ID"].Value = InternalID;

                    using (var reader = m_SelectDevCondsCommand.ExecuteReader())
                    {
                        var ordNid = reader.GetOrdinal("C_NID");
                        var ordName = reader.GetOrdinal("C_NAME");
                        var ordValue = reader.GetOrdinal("C_VALUE");
                        var ordIt = reader.GetOrdinal("IS_TECH");

                        while (reader.Read())
                            list.Add(new ConditionItem
                            {
                                Name = reader.GetString(ordNid),
                                NameLocal = reader.GetString(ordName),
                                Value = reader.GetString(ordValue),
                                IsTech = reader.GetBoolean(ordIt)
                            });
                    }
                }

                return list;
            }
        }

        public List<ParameterNormativeItem> ReadDeviceNormatives(long InternalID)
        {
            lock (ms_Locker)
            {
                var list = new List<ParameterNormativeItem>();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {
                    m_SelectDevNormCommand.Parameters["@DEV_ID"].Value = InternalID;

                    using (var reader = m_SelectDevNormCommand.ExecuteReader())
                    {
                        var ordNid = reader.GetOrdinal("P_NID");
                        var ordMin = reader.GetOrdinal("P_MIN");
                        var ordMax = reader.GetOrdinal("P_MAX");

                        while (reader.Read())
                            list.Add(new ParameterNormativeItem
                            {
                                Name = reader.GetString(ordNid),
                                Min = reader.IsDBNull(ordMin) ? (float?)null : reader.GetFloat(ordMin),
                                Max = reader.IsDBNull(ordMax) ? (float?)null : reader.GetFloat(ordMax)
                            });
                    }
                }

                return list;
            }
        }

        public int? ReadDeviceRTClass(string devCode, string profName)
        {
            //нет смысла вычислять класс по данным одной локально расположенной базы данных
            return null;
        }

        public List<int> ReadDeviceErrors(long InternalID)
        {
            lock (ms_Locker)
            {
                var list = new List<int>();

                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {
                    m_SelectDevErrCommand.Parameters["@DEV_ID"].Value = InternalID;

                    using (var reader = m_SelectDevErrCommand.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add((int)reader[0]);
                    }
                }

                return list;
            }
        }

        #endregion

        #region Maintenance implementation

        public void RemoveGroup(string GroupName)
        {
            lock (ms_Locker)
            {
                if (m_Connection != null && m_Connection.State == ConnectionState.Open)
                {
                    m_GroupDeleteCommand.Parameters["@GROUP_NAME"].Value = GroupName;
                    m_GroupDeleteCommand.ExecuteNonQuery();
                }
            }
        }

        public List<string> GetTableNamesList()
        {
            var res = new List<string>();

            if (m_Connection != null && m_Connection.State == ConnectionState.Open)
            {
                using (var cmd = new SQLiteCommand(TABLES_SELECT_CMD, m_Connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            res.Add(reader.GetString(0));
                    }
                }
            }

            return res;
        }

        public List<GeneralTableRecord> GetTableData(string TableName, long LastID, int TransferSize)
        {
            var res = new List<GeneralTableRecord>();

            if (m_Connection != null && m_Connection.State == ConnectionState.Open)
            {
                using (var cmd = new SQLiteCommand(String.Format(X_SELECT_CMD, TableName, TransferSize), m_Connection))
                {
                    m_SelectDevNormCommand.Parameters["@KEY"].Value = LastID;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new GeneralTableRecord();

                            for (var i = 0; i < reader.FieldCount; ++i)
                                row.Values.Add(reader[i].ToString());

                            res.Add(row);
                        }
                    }
                }
            }

            return res;
        }

        public void InsertTableData(string TableName, IEnumerable<GeneralTableRecord> Data)
        {
            if (m_Connection != null && m_Connection.State == ConnectionState.Open)
            {
                var cmd = m_Connection.CreateCommand();

                foreach (var record in Data)
                {
                    var builder = new StringBuilder();

                    foreach (var val in record.Values)
                        builder.Append(String.Format(@"'{0}', ", val));

                    if (builder.Length > 0)
                        builder.Remove(builder.Length - 2, 2);

                    cmd.CommandText = String.Format(TABLES_INSERT_CMD, TableName, builder);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Private methods


        private void PrepareWriteCommands()
        {

            m_GroupDeleteCommand = new SQLiteCommand(GROUP_DELETE_CMD, m_Connection);
            m_GroupDeleteCommand.Parameters.Add("@GROUP_NAME", DbType.StringFixedLength);
            m_GroupDeleteCommand.Prepare();


        }

        private void PrepareReadCommands()
        {
            m_SelectGroupsCommand = new SQLiteCommand(SELECT_GROUPS_CMD, m_Connection);
            m_SelectGroupsCommand.Parameters.Add("@TS_FROM", DbType.String);
            m_SelectGroupsCommand.Parameters.Add("@TS_TO", DbType.String);
            m_SelectGroupsCommand.Prepare();

            m_SelectProfilesCommand = new SQLiteCommand(SELECT_PROFILES_CMD, m_Connection);
            m_SelectProfilesCommand.Prepare();

            m_SelectDevicesCommand = new SQLiteCommand(SELECT_DEVICES_CMD, m_Connection);
            m_SelectDevicesCommand.Parameters.Add("@GROUP_NAME", DbType.StringFixedLength);
            m_SelectDevicesCommand.Prepare();

            m_SelectDevParamsCommand = new SQLiteCommand(SELECT_DEV_PARAMS_CMD, m_Connection);
            m_SelectDevParamsCommand.Parameters.Add("@DEV_ID", DbType.Int64);
            m_SelectDevParamsCommand.Prepare();

            m_SelectDevCondsCommand = new SQLiteCommand(SELECT_DEV_CONDS_CMD, m_Connection);
            m_SelectDevCondsCommand.Parameters.Add("@DEV_ID", DbType.Int64);
            m_SelectDevCondsCommand.Prepare();

            m_SelectDevNormCommand = new SQLiteCommand(SELECT_DEV_NORM_CMD, m_Connection);
            m_SelectDevNormCommand.Parameters.Add("@DEV_ID", DbType.Int64);
            m_SelectDevNormCommand.Prepare();

             m_SelectDevErrCommand = new SQLiteCommand(SELECT_DEV_ERR_CMD, m_Connection);
            m_SelectDevErrCommand.Parameters.Add("@DEV_ID", DbType.Int64);
            m_SelectDevErrCommand.Prepare();
        }

        #endregion

        public List<ProfileItem> GetProfiles(string mmeCode)
        {
            try
            {
                return _profilesService.GetProfileItemsByMme(mmeCode);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found)
        {
            try
            {
                return _profilesService.GetProfileByProfName(profName, mmmeCode, ref Found);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}