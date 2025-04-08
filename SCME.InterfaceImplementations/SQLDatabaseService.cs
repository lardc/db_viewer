using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.Interfaces;
using SCME.Types.Profiles;
using System.Windows.Forms;

namespace SCME.InterfaceImplementations
{
    public class SQLDatabaseService : IDatabaseService
    {
        private readonly IProfilesService _profilesService;

        #region Properties

        private const string InsertConditionCmdTemplate =
            "INSERT INTO CONDITIONS(COND_NAME, COND_NAME_LOCAL, COND_IS_TECH) VALUES(@COND_NAME, @COND_NAME_LOCAL, @COND_IS_TECH)";

        private const string InsertParamCmdTemplate =
            "INSERT INTO PARAMS(PARAM_NAME, PARAM_NAME_LOCAL, PARAM_IS_HIDE) VALUES(@PARAM_NAME, @PARAM_NAME_LOCAL, @PARAM_IS_HIDE)";

        private const string InsertTestTypeCmdTemplate = "INSERT INTO TEST_TYPE(TEST_TYPE_ID, TEST_TYPE_NAME) VALUES(@ID, @NAME)";

        private const string InsertErrorCmdTemplate =
            "INSERT INTO ERRORS(ERR_NAME, ERR_NAME_LOCAL, ERR_CODE) VALUES(@ERR_NAME, @ERR_NAME_LOCAL, @ERR_CODE)";

        private readonly string[] _mDbTablesList =
            {
                "MME_CODES_TO_PROFILES", "PROF_COND", "PROF_PARAM", "PROF_TEST_TYPE",
                "DEV_PARAM",  "DEV_ERR", "GROUPS", "DEVICES", "CONDITIONS", "PARAMS", "PROFILES",
                "ERRORS", "TEST_TYPE", "MME_CODES"
            };

        private readonly string[] _mDbTablesListReseed =
            {
                "PROF_TEST_TYPE", "GROUPS", "DEVICES", "CONDITIONS", "PARAMS", "PROFILES",
                "ERRORS", "MME_CODES", "DEV_PARAM"
            };

        private readonly Tuple<string, string, bool>[] _mConditionsList =
            {
                new Tuple<string, string, bool>("Gate_En", "Gate_En", true),
                new Tuple<string, string, bool>("Gate_EnableCurrent", "Gate_EnableCurrent", false),
                new Tuple<string, string, bool>("Gate_IHEn", "Gate_IHEn", true),
                new Tuple<string, string, bool>("Gate_ILEn", "Gate_ILEn", true),
                new Tuple<string, string, bool>("Gate_EnableIHStrike", "Gate_EnableIHStrike", true),
                new Tuple<string, string, bool>("SL_En", "SL_En", true),
                new Tuple<string, string, bool>("SL_Type", "SL_TestType", false),
                new Tuple<string, string, bool>("SL_ITM", "SL_ITM", false),
                new Tuple<string, string, bool>("SL_Time", "SL_Time", false),
                new Tuple<string, string, bool>("SL_OpenEn", "SL_OpenEn", true),
                new Tuple<string, string, bool>("SL_OpenI", "SL_OpenI", false),
                new Tuple<string, string, bool>("SL_Factor", "SL_Factor", true),
                new Tuple<string, string, bool>("SL_TimeEx", "SL_TimeEx", true),
                new Tuple<string, string, bool>("SL_FS", "SL_FullScale", true),
                new Tuple<string, string, bool>("SL_N", "SL_N", true),
                new Tuple<string, string, bool>("SL_HeatEn", "SL_HeatEn", false),
                new Tuple<string, string, bool>("SL_RampHeatCurrent", "SL_RampHeatCurrent", false),
                new Tuple<string, string, bool>("SL_RampHeatTime", "SL_RampHeatTime", false),
                new Tuple<string, string, bool>("BVT_En", "BVT_En", true),
                new Tuple<string, string, bool>("BVT_Type", "BVT_Type", false),
                new Tuple<string, string, bool>("BVT_I", "BVT_I", false),
                new Tuple<string, string, bool>("BVT_VD", "BVT_VD", false),
                new Tuple<string, string, bool>("BVT_VR", "BVT_VR", false),
                new Tuple<string, string, bool>("BVT_RumpUp", "BVT_RumpUp", true),
                new Tuple<string, string, bool>("BVT_StartV", "BVT_StartV", true),
                new Tuple<string, string, bool>("BVT_F", "BVT_F", false),
                new Tuple<string, string, bool>("BVT_FD", "BVT_FD", false),
                new Tuple<string, string, bool>("BVT_Mode", "BVT_Mode", false),
                new Tuple<string, string, bool>("BVT_PlateTime", "BVT_PlateTime", false),
                new Tuple<string, string, bool>("BVT_UseUdsmUrsm", "BVT_UseUdsmUrsm", false),
                new Tuple<string, string, bool>("COMM_Type", "COMM_Type", true),
                new Tuple<string, string, bool>("CLAMP_Type", "CLAMP_Type", true),
                new Tuple<string, string, bool>("CLAMP_Force", "CLAMP_Force", true),
                new Tuple<string, string, bool>("CLAMP_HeightMeasure", "CLAMP_HeightMeasure", true),
                new Tuple<string, string, bool>("CLAMP_HeightValue", "CLAMP_HeightValue", true),
                new Tuple<string, string, bool>("CLAMP_Temperature", "CLAMP_Temperature", true),
                new Tuple<string, string, bool>("DVDT_En", "DVDT_En", true),
                new Tuple<string, string, bool>("DVDT_Mode", "DVDT_Mode", true),
                new Tuple<string, string, bool>("DVDT_Voltage", "DVDT_Voltage", true),
                new Tuple<string, string, bool>("DVDT_VoltageRate", "DVDT_VoltageRate", true),
                new Tuple<string, string, bool>("DVDT_ConfirmationCount", "DVDT_ConfirmationCount", true),
                new Tuple<string, string, bool>("DVDT_VoltageRateLimit", "DVDT_VoltageRateLimit", true),
                new Tuple<string, string, bool>("ATU_En", "ATU_En", true),
                new Tuple<string, string, bool>("ATU_PrePulseValue", "ATU_PrePulseValue", true),
                new Tuple<string, string, bool>("ATU_PowerValue", "ATU_PowerValue", true),
                new Tuple<string, string, bool>("QrrTq_En", "QrrTq_En", true),
                new Tuple<string, string, bool>("QrrTq_Mode", "QrrTq_Mode", true),
                new Tuple<string, string, bool>("QrrTq_TrrMeasureBy9050Method", "QrrTq_TrrMeasureBy9050Method", true),
                new Tuple<string, string, bool>("QrrTq_DirectCurrent", "QrrTq_DirectCurrent", true),
                new Tuple<string, string, bool>("QrrTq_DCPulseWidth", "QrrTq_DCPulseWidth", true),
                new Tuple<string, string, bool>("QrrTq_DCRiseRate", "QrrTq_DCRiseRate", true),
                new Tuple<string, string, bool>("QrrTq_DCFallRate", "QrrTq_DCFallRate", true),
                new Tuple<string, string, bool>("QrrTq_OffStateVoltage", "QrrTq_OffStateVoltage", true),
                new Tuple<string, string, bool>("QrrTq_OsvRate", "QrrTq_OsvRate", true),
                new Tuple<string, string, bool>("RAC_En", "RAC_En", true),
                new Tuple<string, string, bool>("RAC_ResVoltage", "RAC_ResVoltage", true),
            };

        private readonly Tuple<string, string, bool>[] _mParamsList =
            {
                new Tuple<string, string, bool>("K", "K", true),
                new Tuple<string, string, bool>("RG", "RG, Ohm", false),
                new Tuple<string, string, bool>("IGT", "IGT, mA", false),
                new Tuple<string, string, bool>("VGT", "VGT, V", false),
                new Tuple<string, string, bool>("IH", "IH, mA", false),
                new Tuple<string, string, bool>("IL", "IL, mA", false),
                new Tuple<string, string, bool>("VTM", "VTM, V", false),
                new Tuple<string, string, bool>("VDRM", "VDRM, V", false),
                new Tuple<string, string, bool>("VRRM", "VRRM, V", false),
                new Tuple<string, string, bool>("IDRM", "IDRM, mA", false),
                new Tuple<string, string, bool>("IRRM", "IRRM, mA", false),
                new Tuple<string, string, bool>("VDSM", "VDSM, V", false),
                new Tuple<string, string, bool>("VRSM", "VRSM, V", false),
                new Tuple<string, string, bool>("IDSM", "IDSM, mA", false),
                new Tuple<string, string, bool>("IRSM", "IRSM, mA", false),
                new Tuple<string, string, bool>("IsHeightOk", "IsHeightOk", false),
                new Tuple<string, string, bool>("UBR", "UBR, V", false),
                new Tuple<string, string, bool>("UPRSM", "UPRSM, V", false),
                new Tuple<string, string, bool>("IPRSM", "IPRSM, A", false),
                new Tuple<string, string, bool>("PRSM", "PRSM, kW", false),
                new Tuple<string, string, bool>("IDC", "IDC, A", false),
                new Tuple<string, string, bool>("QRR", "QRR, uC", false),
                new Tuple<string, string, bool>("IRR", "IRR, A", false),
                new Tuple<string, string, bool>("TRR", "TRR, us", false),
                new Tuple<string, string, bool>("DCFactFallRate", "dIDC/dt, A/us", false),
                new Tuple<string, string, bool>("TQ", "TQ, us", false),
                new Tuple<string, string, bool>("ResultR", "ResultR, MOhm", false)
            };

        private readonly Tuple<string, string, int>[] _mDefectCodes =
            {
                new Tuple<string, string, int>("ERR_KELVIN", "Error connection", 11),
                new Tuple<string, string, int>("ERR_RG", "RG out of range", 12),
                new Tuple<string, string, int>("ERR_IGT", "IGT out of range", 13),
                new Tuple<string, string, int>("ERR_VGT", "VGT out of range", 14),
                new Tuple<string, string, int>("ERR_IH", "IH out of range", 15),
                new Tuple<string, string, int>("ERR_IL", "IL out of range", 16),
                new Tuple<string, string, int>("ERR_IHL_PROBLEM", "IH-IL problem", 17),
                new Tuple<string, string, int>("ERR_GT_PROBLEM", "IGT-VGT problem", 18),
                new Tuple<string, string, int>("ERR_VTM", "VTM out of range", 20),
                new Tuple<string, string, int>("ERR_ITM_PROBLEM", "ITM problem", 21),
                new Tuple<string, string, int>("ERR_VTM_PROBLEM", "VTM problem", 22),
                new Tuple<string, string, int>("ERR_VDRM", "VDRM out of range", 31),
                new Tuple<string, string, int>("ERR_VRRM", "VRRM out of range", 32),
                new Tuple<string, string, int>("ERR_IDRM", "IDRM out of range", 33),
                new Tuple<string, string, int>("ERR_IRRM", "IRRM out of range", 34),
                new Tuple<string, string, int>("ERR_RM_OVERLOAD", "RM overload", 35),
                new Tuple<string, string, int>("ERR_UBR", "UBR out of range", 36),
                new Tuple<string, string, int>("ERR_UPRSM", "UPRSM out of range", 37),
                new Tuple<string, string, int>("ERR_IPRSM", "IPRSM out of range", 38),
                new Tuple<string, string, int>("ERR_PRSM", "PRSM out of range", 39)
            };

        private readonly Tuple<int, string>[] m_TestTypes =
            {
                new Tuple<int, string>(1, "Gate"),
                new Tuple<int, string>(2, "SL"),
                new Tuple<int, string>(3, "BVT"),
                new Tuple<int, string>(4, "Commutation"),
                new Tuple<int, string>(5, "Clamping"),
                new Tuple<int, string>(6, "Dvdt"),
                new Tuple<int, string>(7, "SCTU"),
                new Tuple<int, string>(8, "ATU"),
                new Tuple<int, string>(9, "QrrTq"),
                new Tuple<int, string>(10, "RAC")
            };

        private readonly string[] _mmeCodes = { };

        private readonly SqlConnection _mConnection;

        #endregion

        public SQLDatabaseService(string connectionString)
        {
            _mConnection =
                new SqlConnection(connectionString);

            _profilesService = new SQLProfilesService(connectionString);
        }

        public void ImportProfiles(string filePath)
        {
        }

        public void ImportData(string connectionString, bool importProfiles, bool importResults)
        {
            if (_mConnection.State != ConnectionState.Open)
                return;

            try
            {
                using (var sqliteConnection = new SQLiteConnection(connectionString, false))
                {
                    sqliteConnection.Open();

                    if (importProfiles)
                    {
                        ImportSQliteProfiles(sqliteConnection);

                        MessageBox.Show("Profile import successful");
                    }

                    if (importResults)
                    {
                        ImportSQliteResults(sqliteConnection);

                        MessageBox.Show("Result import successful");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Operation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportSQliteResults(SQLiteConnection sqliteConnection)
        {
            var trans = _mConnection.BeginTransaction();

            try
            {
                // COPY GROUPS

                var cmdSelectGroups = new SQLiteCommand("SELECT * FROM GROUPS", sqliteConnection);
                var cmdInsertGroups = new SqlCommand("INSERT INTO [dbo].GROUPS(GROUP_ID, GROUP_NAME) VALUES(@ID, @NAME)", _mConnection);
                cmdInsertGroups.Parameters.Add("@ID", SqlDbType.Int);
                cmdInsertGroups.Parameters.Add("@NAME", SqlDbType.NChar, 32);
                cmdInsertGroups.Transaction = trans;
                cmdInsertGroups.Prepare();

                try
                {
                    new SqlCommand("SET IDENTITY_INSERT GROUPS ON", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    var reader = cmdSelectGroups.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertGroups.Parameters[0].Value = reader[0];
                        cmdInsertGroups.Parameters[1].Value = reader[1];

                        cmdInsertGroups.ExecuteNonQuery();
                    }
                }
                finally
                {
                    new SqlCommand("SET IDENTITY_INSERT GROUPS OFF", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    new SqlCommand("DBCC CHECKIDENT ('GROUPS')", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();
                }

                // COPY DEVICES

                var cmdSelectDevices = new SQLiteCommand("SELECT * FROM DEVICES", sqliteConnection);
                var cmdInsertDevices =
                    new SqlCommand(
                        "INSERT INTO [dbo].[DEVICES](DEV_ID, GROUP_ID, PROFILE_ID, CODE, SIL_N_1, SIL_N_2, TS, USR, POS, MME_CODE) VALUES(@DEV_ID, @GROUP_ID, @PROFILE_ID, @CODE, @SIL_N_1, @SIL_N_2, @TS, @USR, @POS, @MME_CODE)",
                        _mConnection);
                cmdInsertDevices.Parameters.Add("@DEV_ID", SqlDbType.Int);
                cmdInsertDevices.Parameters.Add("@GROUP_ID", SqlDbType.Int);
                cmdInsertDevices.Parameters.Add("@PROFILE_ID", SqlDbType.UniqueIdentifier);
                cmdInsertDevices.Parameters.Add("@CODE", SqlDbType.NVarChar, 32);
                cmdInsertDevices.Parameters.Add("@SIL_N_1", SqlDbType.NVarChar, 64);
                cmdInsertDevices.Parameters.Add("@SIL_N_2", SqlDbType.NVarChar, 64);
                cmdInsertDevices.Parameters.Add("@TS", SqlDbType.DateTime);
                cmdInsertDevices.Parameters.Add("@USR", SqlDbType.VarChar, 32);
                cmdInsertDevices.Parameters.Add("@POS", SqlDbType.Bit);
                cmdInsertDevices.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
                cmdInsertDevices.Transaction = trans;
                cmdInsertDevices.Prepare();

                try
                {
                    new SqlCommand("SET IDENTITY_INSERT DEVICES ON", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    var reader = cmdSelectDevices.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertDevices.Parameters[0].Value = reader[0];
                        cmdInsertDevices.Parameters[1].Value = reader[1];
                        cmdInsertDevices.Parameters[2].Value = reader[2];
                        cmdInsertDevices.Parameters[3].Value = reader[3];
                        cmdInsertDevices.Parameters[4].Value = reader[4];
                        cmdInsertDevices.Parameters[5].Value = reader[5];
                        cmdInsertDevices.Parameters[6].Value = reader[6];
                        cmdInsertDevices.Parameters[7].Value = reader[7];
                        cmdInsertDevices.Parameters[8].Value = reader[8];
                        cmdInsertDevices.Parameters[9].Value = reader[10];

                        cmdInsertDevices.ExecuteNonQuery();
                    }
                }
                finally
                {
                    new SqlCommand("SET IDENTITY_INSERT DEVICES OFF", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    new SqlCommand("DBCC CHECKIDENT ('DEVICES')", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();
                }

                // COPY DEV_PARAM

                var cmdSelectDevParam = new SQLiteCommand("SELECT * FROM DEV_PARAM", sqliteConnection);
                var cmdInsertDevParam =
                    new SqlCommand(
                        "INSERT INTO [dbo].[DEV_PARAM](DEV_ID, PARAM_ID, VALUE, TEST_TYPE_ID) VALUES(@DEV_ID, @PARAM_ID, @VALUE, @TEST_TYPE_ID)",
                        _mConnection);
                cmdInsertDevParam.Parameters.Add("@DEV_ID", SqlDbType.Int);
                cmdInsertDevParam.Parameters.Add("@PARAM_ID", SqlDbType.Int);
                cmdInsertDevParam.Parameters.Add("@VALUE", SqlDbType.Float);
                cmdInsertDevParam.Parameters.Add("@TEST_TYPE_ID", SqlDbType.Int);
                cmdInsertDevParam.Transaction = trans;
                cmdInsertDevParam.Prepare();

                try
                {
                    var reader = cmdSelectDevParam.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertDevParam.Parameters[0].Value = reader[0];
                        cmdInsertDevParam.Parameters[1].Value = reader[1];
                        cmdInsertDevParam.Parameters[2].Value = reader[2];
                        cmdInsertDevParam.Parameters[3].Value = reader[3];

                        cmdInsertDevParam.ExecuteNonQuery();
                    }
                }
                finally
                {
                }

                // COPY DEV_ERR

                var cmdSelectDevErr = new SQLiteCommand("SELECT * FROM DEV_ERR", sqliteConnection);
                var cmdInsertDevErr =
                    new SqlCommand("INSERT INTO [dbo].[DEV_ERR](DEV_ID, ERR_ID) VALUES(@DEV_ID, @ERR_ID)", _mConnection);
                cmdInsertDevErr.Parameters.Add("@DEV_ID", SqlDbType.Int);
                cmdInsertDevErr.Parameters.Add("@ERR_ID", SqlDbType.Int);
                cmdInsertDevErr.Transaction = trans;
                cmdInsertDevErr.Prepare();

                try
                {
                    var reader = cmdSelectDevErr.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertDevErr.Parameters[0].Value = reader[0];
                        cmdInsertDevErr.Parameters[1].Value = reader[1];

                        cmdInsertDevErr.ExecuteNonQuery();
                    }
                }
                finally { }

                trans.Commit();
            }
            catch (Exception)
            {
                trans.Rollback();

                throw;
            }
        }

        private void ImportSQliteProfiles(SQLiteConnection sqliteConnection)
        {
            var trans = _mConnection.BeginTransaction();

            try
            {
                // COPY PROFILES

                var cmdSelectProfiles = new SQLiteCommand("SELECT * FROM PROFILES", sqliteConnection);
                var cmdInsertProfiles = new SqlCommand("INSERT INTO [dbo].[PROFILES](PROF_ID, PROF_NAME, PROF_GUID, PROF_TS, PROF_VERS, IS_DELETED) VALUES (@PROF_ID, @PROF_NAME, @PROF_GUID, @PROF_TS, @VERSION, @IS_DELETED)", _mConnection);
                cmdInsertProfiles.Parameters.Add("@PROF_ID", SqlDbType.Int);
                cmdInsertProfiles.Parameters.Add("@PROF_NAME", SqlDbType.NVarChar, 32);
                cmdInsertProfiles.Parameters.Add("@PROF_GUID", SqlDbType.UniqueIdentifier);
                cmdInsertProfiles.Parameters.Add("@PROF_TS", SqlDbType.DateTime);
                cmdInsertProfiles.Parameters.Add("@VERSION", SqlDbType.Int);
                cmdInsertProfiles.Parameters.Add("@IS_DELETED", SqlDbType.Bit);
                cmdInsertProfiles.Transaction = trans;
                cmdInsertProfiles.Prepare();

                try
                {
                    new SqlCommand("SET IDENTITY_INSERT PROFILES ON", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    var reader = cmdSelectProfiles.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertProfiles.Parameters[0].Value = reader[0];
                        cmdInsertProfiles.Parameters[1].Value = reader[1];
                        cmdInsertProfiles.Parameters[2].Value = reader[2];
                        cmdInsertProfiles.Parameters[3].Value = reader[3];
                        cmdInsertProfiles.Parameters[4].Value = reader[4];
                        cmdInsertProfiles.Parameters[5].Value = reader[5];

                        cmdInsertProfiles.ExecuteNonQuery();
                    }
                }
                finally
                {
                    new SqlCommand("SET IDENTITY_INSERT PROFILES OFF", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    new SqlCommand("DBCC CHECKIDENT ('PROFILES')", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();
                }

                // COPY PROF_TEST_TYPE

                var cmdSelectProfTestType = new SQLiteCommand("SELECT P.ID, P.[PROF_ID], P.[TEST_TYPE_ID], P.[ORDER] AS ORD FROM PROF_TEST_TYPE P", sqliteConnection);
                var cmdInsertProfTestType = new SqlCommand("INSERT INTO [dbo].[PROF_TEST_TYPE] (PTT_ID, PROF_ID, TEST_TYPE_ID, [ORD]) VALUES (@PTT_ID, @PROF_ID, @TEST_TYPE_ID, @ORD)", _mConnection);
                cmdInsertProfTestType.Parameters.Add("@PTT_ID", SqlDbType.Int);
                cmdInsertProfTestType.Parameters.Add("@PROF_ID", SqlDbType.Int);
                cmdInsertProfTestType.Parameters.Add("@TEST_TYPE_ID", SqlDbType.Int);
                cmdInsertProfTestType.Parameters.Add("@ORD", SqlDbType.Int);
                cmdInsertProfTestType.Transaction = trans;
                cmdInsertProfTestType.Prepare();

                try
                {
                    new SqlCommand("SET IDENTITY_INSERT PROF_TEST_TYPE ON", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    var reader = cmdSelectProfTestType.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertProfTestType.Parameters[0].Value = reader[0];
                        cmdInsertProfTestType.Parameters[1].Value = reader[1];
                        cmdInsertProfTestType.Parameters[2].Value = reader[2];
                        cmdInsertProfTestType.Parameters[3].Value = reader[3];

                        cmdInsertProfTestType.ExecuteNonQuery();
                    }
                }
                finally
                {
                    new SqlCommand("SET IDENTITY_INSERT PROF_TEST_TYPE OFF", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    new SqlCommand("DBCC CHECKIDENT ('PROF_TEST_TYPE')", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();
                }

                // COPY PROF_COND

                var cmdSelectProfCond = new SQLiteCommand("SELECT * FROM PROF_COND", sqliteConnection);
                var cmdInsertProfCond = new SqlCommand("INSERT INTO [dbo].[PROF_COND](PROF_TESTTYPE_ID, PROF_ID, COND_ID, VALUE) VALUES(@PROF_TESTTYPE_ID, @PROF_ID, @COND_ID, @VALUE)", _mConnection);
                cmdInsertProfCond.Parameters.Add("@PROF_TESTTYPE_ID", SqlDbType.Int);
                cmdInsertProfCond.Parameters.Add("@PROF_ID", SqlDbType.Int);
                cmdInsertProfCond.Parameters.Add("@COND_ID", SqlDbType.Int);
                cmdInsertProfCond.Parameters.Add("@VALUE", SqlDbType.NChar, 16);
                cmdInsertProfCond.Transaction = trans;
                cmdInsertProfCond.Prepare();

                try
                {
                    var reader = cmdSelectProfCond.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertProfCond.Parameters[0].Value = reader[0];
                        cmdInsertProfCond.Parameters[1].Value = reader[1];
                        cmdInsertProfCond.Parameters[2].Value = reader[2];
                        cmdInsertProfCond.Parameters[3].Value = reader[3];

                        cmdInsertProfCond.ExecuteNonQuery();
                    }
                }
                finally
                {
                }

                // COPY PROF_PARAM

                var cmdSelectProfParam = new SQLiteCommand("SELECT * FROM PROF_PARAM", sqliteConnection);
                var cmdInsertProfParam = new SqlCommand("INSERT INTO [dbo].[PROF_PARAM](PROF_TESTTYPE_ID, PROF_ID, PARAM_ID, MIN_VAL, MAX_VAL) VALUES(@PROF_TESTTYPE_ID, @PROF_ID, @PARAM_ID, @MIN_VAL, @MAX_VAL)", _mConnection);
                cmdInsertProfParam.Parameters.Add("@PROF_TESTTYPE_ID", SqlDbType.Int);
                cmdInsertProfParam.Parameters.Add("@PROF_ID", SqlDbType.Int);
                cmdInsertProfParam.Parameters.Add("@PARAM_ID", SqlDbType.Int);
                cmdInsertProfParam.Parameters.Add("@MIN_VAL", SqlDbType.Float);
                cmdInsertProfParam.Parameters.Add("@MAX_VAL", SqlDbType.Float);
                cmdInsertProfParam.Transaction = trans;
                cmdInsertProfParam.Prepare();

                try
                {
                    var reader = cmdSelectProfParam.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertProfParam.Parameters[0].Value = reader[0];
                        cmdInsertProfParam.Parameters[1].Value = reader[1];
                        cmdInsertProfParam.Parameters[2].Value = reader[2];
                        cmdInsertProfParam.Parameters[3].Value = reader[3];
                        cmdInsertProfParam.Parameters[4].Value = reader[4];

                        cmdInsertProfParam.ExecuteNonQuery();
                    }
                }
                finally
                {
                }

                // COPY MME_CODES

                var cmdSelectMme = new SQLiteCommand("SELECT * FROM MME_CODES", sqliteConnection);
                var cmdInsertMme = new SqlCommand("INSERT INTO [dbo].[MME_CODES] (MME_CODE_ID, MME_CODE) VALUES (@MME_CODE_ID, @MME_CODE)", _mConnection);
                cmdInsertMme.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
                cmdInsertMme.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
                cmdInsertMme.Transaction = trans;
                cmdInsertMme.Prepare();

                try
                {
                    new SqlCommand("SET IDENTITY_INSERT MME_CODES ON", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    var reader = cmdSelectMme.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertMme.Parameters[0].Value = reader[0];
                        cmdInsertMme.Parameters[1].Value = reader[1];

                        cmdInsertMme.ExecuteNonQuery();
                    }
                }
                finally
                {
                    new SqlCommand("SET IDENTITY_INSERT MME_CODES OFF", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();

                    new SqlCommand("DBCC CHECKIDENT ('MME_CODES')", _mConnection)
                    {
                        Transaction = trans
                    }.ExecuteNonQuery();
                }

                // COPY MME_CODES_TO_PROFILES

                var cmdSelectMmeCtoP = new SQLiteCommand("SELECT * FROM MME_CODES_TO_PROFILES", sqliteConnection);
                var cmdInsertMmeCtoP = new SqlCommand("INSERT INTO [dbo].[MME_CODES_TO_PROFILES] (MME_CODE_ID, PROFILE_ID) VALUES (@MME_CODE_ID, @PROFILE_ID)", _mConnection);
                cmdInsertMmeCtoP.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
                cmdInsertMmeCtoP.Parameters.Add("@PROFILE_ID", SqlDbType.Int);
                cmdInsertMmeCtoP.Transaction = trans;
                cmdInsertMmeCtoP.Prepare();

                try
                {
                    var reader = cmdSelectMmeCtoP.ExecuteReader();
                    while (reader.Read())
                    {
                        cmdInsertMmeCtoP.Parameters[0].Value = reader[0];
                        cmdInsertMmeCtoP.Parameters[1].Value = reader[1];

                        cmdInsertMmeCtoP.ExecuteNonQuery();
                    }
                }
                finally
                {
                }

                trans.Commit();
            }
            catch (Exception)
            {
                trans.Rollback();

                throw;
            }
        }

        public void Open()
        {
            _mConnection.Open();
        }

        public void Close()
        {
            if (_mConnection.State == ConnectionState.Open)
                _mConnection.Close();
        }

        public ConnectionState State
        {
            get { return _mConnection.State; }
            set { throw new NotImplementedException(); }
        }

        public void ResetContent()
        {
            if (_mConnection.State == ConnectionState.Open)
            {
                var trans = _mConnection.BeginTransaction();

                try
                {
                    using (var deleteCmd = _mConnection.CreateCommand())
                    {
                        foreach (var table in _mDbTablesList)
                        {
                            deleteCmd.CommandText = $"DELETE FROM {table}";
                            deleteCmd.Transaction = trans;
                            deleteCmd.ExecuteNonQuery();
                        }
                    }

                    using (var reseedCmd = _mConnection.CreateCommand())
                    {
                        foreach (var table in _mDbTablesListReseed)
                        {
                            reseedCmd.CommandText = $"DBCC CHECKIDENT ({table}, RESEED, 0)";
                            reseedCmd.Transaction = trans;
                            reseedCmd.ExecuteNonQuery();
                        }
                    }

                    using (var insertCmd = _mConnection.CreateCommand())
                    {
                        insertCmd.CommandText = InsertConditionCmdTemplate;
                        insertCmd.Parameters.Add("@COND_NAME", SqlDbType.Char, 32);
                        insertCmd.Parameters.Add("@COND_NAME_LOCAL", SqlDbType.NVarChar, 64);
                        insertCmd.Parameters.Add("@COND_IS_TECH", SqlDbType.Bit);
                        insertCmd.Transaction = trans;
                        insertCmd.Prepare();

                        foreach (var condition in _mConditionsList)
                        {
                            insertCmd.Parameters["@COND_NAME"].Value = condition.Item1;
                            insertCmd.Parameters["@COND_NAME_LOCAL"].Value = condition.Item2;
                            insertCmd.Parameters["@COND_IS_TECH"].Value = condition.Item3;

                            insertCmd.ExecuteNonQuery();
                        }
                    }

                    using (var insertCmd = _mConnection.CreateCommand())
                    {
                        insertCmd.CommandText = InsertTestTypeCmdTemplate;
                        insertCmd.Parameters.Add("@ID", SqlDbType.Int);
                        insertCmd.Parameters.Add("@NAME", SqlDbType.NVarChar, 32);
                        insertCmd.Transaction = trans;
                        insertCmd.Prepare();

                        foreach (var testType in m_TestTypes)
                        {
                            insertCmd.Parameters["@ID"].Value = testType.Item1;
                            insertCmd.Parameters["@NAME"].Value = testType.Item2;

                            insertCmd.ExecuteNonQuery();
                        }
                    }

                    using (var insertCmd = _mConnection.CreateCommand())
                    {
                        insertCmd.CommandText = InsertParamCmdTemplate;
                        insertCmd.Parameters.Add("@PARAM_NAME", SqlDbType.Char, 16);
                        insertCmd.Parameters.Add("@PARAM_NAME_LOCAL", SqlDbType.NVarChar, 64);
                        insertCmd.Parameters.Add("@PARAM_IS_HIDE", SqlDbType.Bit);
                        insertCmd.Transaction = trans;
                        insertCmd.Prepare();

                        foreach (var param in _mParamsList)
                        {
                            insertCmd.Parameters["@PARAM_NAME"].Value = param.Item1;
                            insertCmd.Parameters["@PARAM_NAME_LOCAL"].Value = param.Item2;
                            insertCmd.Parameters["@PARAM_IS_HIDE"].Value = param.Item3;

                            insertCmd.ExecuteNonQuery();
                        }
                    }

                    using (var insertCmd = _mConnection.CreateCommand())
                    {
                        insertCmd.CommandText = InsertErrorCmdTemplate;
                        insertCmd.Parameters.Add("@ERR_NAME", SqlDbType.Char, 20);
                        insertCmd.Parameters.Add("@ERR_NAME_LOCAL", SqlDbType.NVarChar, 32);
                        insertCmd.Parameters.Add("@ERR_CODE", SqlDbType.Int);
                        insertCmd.Transaction = trans;
                        insertCmd.Prepare();

                        foreach (var err in _mDefectCodes)
                        {
                            insertCmd.Parameters["@ERR_NAME"].Value = err.Item1;
                            insertCmd.Parameters["@ERR_NAME_LOCAL"].Value = err.Item2;
                            insertCmd.Parameters["@ERR_CODE"].Value = err.Item3;

                            insertCmd.ExecuteNonQuery();
                        }
                    }

                    using (var insertCmd = _mConnection.CreateCommand())
                    {
                        insertCmd.CommandText = "INSERT INTO MME_CODES (MME_CODE) VALUES (@MME_CODE)";
                        insertCmd.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
                        insertCmd.Transaction = trans;
                        insertCmd.Prepare();

                        foreach (var code in _mmeCodes)
                        {
                            insertCmd.Parameters["@MME_CODE"].Value = code;
                            insertCmd.ExecuteNonQuery();
                        }
                    }

                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
        }
    }
}