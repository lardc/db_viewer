using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.Interfaces;
using SCME.Types.Profiles;

namespace SCME.InterfaceImplementations
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IProfilesService _profilesService;

        #region Properties
        private const string InsertConditionCmdTemplate =
            "INSERT INTO CONDITIONS(COND_ID, COND_NAME, COND_NAME_LOCAL, COND_IS_TECH) VALUES(NULL, @COND_NAME, @COND_NAME_LOCAL, @COND_IS_TECH)";

        private const string InsertParamCmdTemplate =
            "INSERT INTO PARAMS(PARAM_ID, PARAM_NAME, PARAM_NAME_LOCAL, PARAM_IS_HIDE) VALUES(NULL, @PARAM_NAME, @PARAM_NAME_LOCAL, @PARAM_IS_HIDE)";

        private const string InsertTestTypeCmdTemplate =
          "INSERT INTO TEST_TYPE(ID, NAME) VALUES(@ID, @NAME)";

        private const string InsertErrorCmdTemplate =
            "INSERT INTO ERRORS(ERR_ID, ERR_NAME, ERR_NAME_LOCAL, ERR_CODE) VALUES(NULL, @ERR_NAME, @ERR_NAME_LOCAL, @ERR_CODE)";

        private readonly string[] _mDbTablesList =
            {
                "CONDITIONS", "DEVICES", "GROUPS", "PARAMS", "PROFILES", "DEV_PARAM"
                , "PROF_COND", "PROF_PARAM", "ERRORS", "DEV_ERR", "TEST_TYPE","PROF_TEST_TYPE","MME_CODES","MME_CODES_TO_PROFILES"
            };

        private readonly Tuple<string, string, bool>[] _mConditionsList = new[]
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
                new Tuple<string, string, bool>("RAC_ResVoltage", "RAC_ResVoltage", true)
            };

        private readonly Tuple<string, string, bool>[] _mParamsList = new[]
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
                new Tuple<string, string, bool>("RAC_ResVoltage", "RAC_ResVoltage, МОм", false)
            };

        private readonly Tuple<string, string, int>[] _mDefectCodes = new[]
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

        private readonly Tuple<int, string>[] m_TestTypes = new[]
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

        private readonly string[] _mmeCodes = {
            "MME002",
            "MME005",
            "MME006",
            "MME007",
            "MME008",
            "MME009"
        };

        private readonly SQLiteConnection _mConnection;
        #endregion

        public DatabaseService(string databasePath, string dbSettings = "synchronous=Full;journal mode=Truncate;failifmissing=True")
        {
            _mConnection =
                new SQLiteConnection(
                    $"data source={databasePath};{dbSettings}", false);
            _profilesService = new SQLiteProfilesService($"data source={databasePath};{dbSettings}");
        }

        public void ImportProfiles(string filePath)
        {
            var dictionary = new ProfileDictionary(filePath);
            var profileList = dictionary.PlainCollection.ToList();
            var profileItems = new List<ProfileItem>(profileList.Count);
            foreach (var profile in profileList)
            {
                var profileItem = new ProfileItem
                {
                    ProfileName = profile.Name,
                    ProfileKey = profile.Key,
                    ProfileTS = profile.Timestamp,
                    GateTestParameters = new List<Types.Gate.TestParameters>(),
                    VTMTestParameters = new List<Types.SL.TestParameters>(),
                    BVTTestParameters = new List<Types.BVT.TestParameters>(),
                    DvDTestParameterses = new List<Types.dVdt.TestParameters>(),
                    ATUTestParameters = new List<Types.ATU.TestParameters>(),
                    QrrTqTestParameters = new List<Types.QrrTq.TestParameters>(),
                    RACTestParameters = new List<Types.RAC.TestParameters>(),
                    CommTestParameters = profile.ParametersComm,
                    IsHeightMeasureEnabled = profile.IsHeightMeasureEnabled,
                    ParametersClamp = profile.ParametersClamp,
                    Height = profile.Height,
                    Temperature = profile.Temperature
                };
                foreach (var baseTestParametersAndNormativese in profile.TestParametersAndNormatives)
                {
                    var gate = baseTestParametersAndNormativese as Types.Gate.TestParameters;
                    if (gate != null)
                    {
                        profileItem.GateTestParameters.Add(gate);
                        continue;
                    }
                    var sl = baseTestParametersAndNormativese as Types.SL.TestParameters;
                    if (sl != null)
                    {
                        profileItem.VTMTestParameters.Add(sl);
                        continue;
                    }
                    var bvt = baseTestParametersAndNormativese as Types.BVT.TestParameters;
                    if (bvt != null)
                        profileItem.BVTTestParameters.Add(bvt);

                    var dvdt = baseTestParametersAndNormativese as Types.dVdt.TestParameters;
                    if (dvdt != null)
                        profileItem.DvDTestParameterses.Add(dvdt);

                    var atu = baseTestParametersAndNormativese as Types.ATU.TestParameters;
                    if (atu != null)
                        profileItem.ATUTestParameters.Add(atu);

                    var qrrTq = baseTestParametersAndNormativese as Types.QrrTq.TestParameters;
                    if (qrrTq != null)
                        profileItem.QrrTqTestParameters.Add(qrrTq);

                    var rac = baseTestParametersAndNormativese as Types.RAC.TestParameters;
                    if (rac != null)
                        profileItem.RACTestParameters.Add(rac);
                }
                profileItems.Add(profileItem);

            }
            if (State == ConnectionState.Open)
                _profilesService.SaveProfiles(profileItems);
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
                            deleteCmd.ExecuteNonQuery();
                        }
                    }

                    using (var insertCmd = _mConnection.CreateCommand())
                    {
                        insertCmd.CommandText = InsertConditionCmdTemplate;
                        insertCmd.Parameters.Add("@COND_NAME", DbType.AnsiStringFixedLength);
                        insertCmd.Parameters.Add("@COND_NAME_LOCAL", DbType.String);
                        insertCmd.Parameters.Add("@COND_IS_TECH", DbType.Boolean);
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
                        insertCmd.Parameters.Add("@ID", DbType.Int32);
                        insertCmd.Parameters.Add("@NAME", DbType.String);
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
                        insertCmd.Parameters.Add("@PARAM_NAME", DbType.AnsiStringFixedLength);
                        insertCmd.Parameters.Add("@PARAM_NAME_LOCAL", DbType.String);
                        insertCmd.Parameters.Add("@PARAM_IS_HIDE", DbType.Boolean);
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
                        insertCmd.Parameters.Add("@ERR_NAME", DbType.AnsiStringFixedLength);
                        insertCmd.Parameters.Add("@ERR_NAME_LOCAL", DbType.String);
                        insertCmd.Parameters.Add("@ERR_CODE", DbType.Int32);
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
                        insertCmd.Parameters.Add("@MME_CODE", DbType.String);
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