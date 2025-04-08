using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.BVT;
using SCME.Types.dVdt;
using SCME.Types.Interfaces;
using SCME.Types.SL;
using TestParameters = SCME.Types.Gate.TestParameters;

namespace SCME.InterfaceImplementations
{
    public class SQLSaveProfileService : ISaveProfileService
    {
        private readonly SqlConnection _connection;

        private SqlCommand _profileDeleteCommand;
        private SqlCommand _condDeleteCommand;
        private SqlCommand _mmeSelectCommand;
        private SqlCommand _mmeInsertCommand;
        private SqlCommand _codesInsertCommand;
        private SqlCommand _profileVersionSelect;
        private SqlCommand _profileInsertCommand;
        private SqlCommand _connectionsUpdateCommand;
        private SqlCommand _profileSelectCommand;
        private SqlCommand _profTestTypeInsertCommand;
        private SqlCommand _profCondInsertCommand;
        private SqlCommand _paramInsertCommand;

        public SQLSaveProfileService(SqlConnection connection)
        {
            _connection = connection;
            _testTypes = new Dictionary<string, long>(5);
            _conditions = new Dictionary<string, long>(64);
            _params = new Dictionary<string, long>(64);

            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            PrepareQueries();
            PopulateDictionaries();
        }

        private void PrepareQueries()
        {
            _profileDeleteCommand = new SqlCommand("UPDATE [dbo].[PROFILES] SET [IS_DELETED] = 1 WHERE [PROF_ID] = @PROF_ID",
                _connection);
            _profileDeleteCommand.Parameters.Add("@PROF_ID", SqlDbType.Int);
            _profileDeleteCommand.Prepare();

            _condDeleteCommand =
                new SqlCommand(
                    "DELETE FROM [dbo].[MME_CODES_TO_PROFILES] WHERE [PROFILE_ID] = @PROF_ID AND [MME_CODE_ID] = @MME_CODE_ID",
                    _connection);
            _condDeleteCommand.Parameters.Add("@PROF_ID", SqlDbType.Int);
            _condDeleteCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _condDeleteCommand.Prepare();

            _mmeSelectCommand = new SqlCommand("SELECT [MME_CODE_ID] FROM [dbo].[MME_CODES] WHERE [MME_CODE] = @MME_CODE",
                _connection);
            _mmeSelectCommand.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
            _mmeSelectCommand.Prepare();

            _mmeInsertCommand =
                new SqlCommand("INSERT INTO [dbo].[MME_CODES] (MME_CODE) OUTPUT INSERTED.MME_CODE_ID VALUES (@MME_CODE)",
                    _connection);
            _mmeInsertCommand.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
            _mmeInsertCommand.Prepare();

            _codesInsertCommand =
                new SqlCommand(
                    "INSERT INTO [dbo].[MME_CODES_TO_PROFILES] (MME_CODE_ID, PROFILE_ID) VALUES (@MME_CODE_ID, @PROFILE_ID)",
                    _connection);
            _codesInsertCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _codesInsertCommand.Parameters.Add("@PROFILE_ID", SqlDbType.Int);
            _codesInsertCommand.Prepare();

            _profileVersionSelect =
                new SqlCommand("SELECT P.[PROF_VERS] FROM [dbo].[PROFILES] P WHERE P.[PROF_ID] = @PROF_ID", _connection);
            _profileVersionSelect.Parameters.Add("@PROF_ID", SqlDbType.Int);
            _profileVersionSelect.Prepare();

            _profileInsertCommand =
                new SqlCommand(
                    "INSERT INTO [dbo].[PROFILES](PROF_NAME, PROF_GUID, PROF_TS, PROF_VERS) OUTPUT INSERTED.PROF_ID VALUES (@PROF_NAME, @PROF_GUID, @PROF_TS, @VERSION)",
                    _connection);
            _profileInsertCommand.Parameters.Add("@PROF_GUID", SqlDbType.UniqueIdentifier);
            _profileInsertCommand.Parameters.Add("@PROF_TS", SqlDbType.DateTime);
            _profileInsertCommand.Parameters.Add("@PROF_NAME", SqlDbType.NVarChar, 32);
            _profileInsertCommand.Parameters.Add("@VERSION", SqlDbType.Int);
            _profileInsertCommand.Prepare();

            _connectionsUpdateCommand =
                new SqlCommand(
                    "UPDATE [dbo].[MME_CODES_TO_PROFILES] SET [PROFILE_ID] = @NEW_PROFILE_ID WHERE [PROFILE_ID] = @PROFILE_ID",
                    _connection);
            _connectionsUpdateCommand.Parameters.Add("@NEW_PROFILE_ID", SqlDbType.Int);
            _connectionsUpdateCommand.Parameters.Add("@PROFILE_ID", SqlDbType.Int);
            _connectionsUpdateCommand.Prepare();

            _profileSelectCommand = new SqlCommand(
                "SELECT P.[PROF_ID] FROM [dbo].[PROFILES] P WHERE P.[PROF_GUID] = @PROF_GUID", _connection);
            _profileSelectCommand.Parameters.Add("@PROF_GUID", SqlDbType.UniqueIdentifier);
            _profileSelectCommand.Prepare();

            _profTestTypeInsertCommand =
                new SqlCommand(
                    "INSERT INTO [dbo].[PROF_TEST_TYPE] (PROF_ID, TEST_TYPE_ID, [ORD]) OUTPUT INSERTED.PTT_ID VALUES (@PROF_ID, @TEST_TYPE_ID, @ORD)",
                    _connection);
            _profTestTypeInsertCommand.Parameters.Add("@PROF_ID", SqlDbType.Int);
            _profTestTypeInsertCommand.Parameters.Add("@TEST_TYPE_ID", SqlDbType.Int);
            _profTestTypeInsertCommand.Parameters.Add("@ORD", SqlDbType.Int);
            _profTestTypeInsertCommand.Prepare();

            _profCondInsertCommand =
                new SqlCommand(
                    "INSERT INTO [dbo].[PROF_COND](PROF_TESTTYPE_ID, PROF_ID, COND_ID, VALUE) VALUES(@PROF_TESTTYPE_ID, @PROF_ID, @COND_ID, @VALUE)",
                    _connection);
            _profCondInsertCommand.Parameters.Add("@PROF_TESTTYPE_ID", SqlDbType.Int);
            _profCondInsertCommand.Parameters.Add("@PROF_ID", SqlDbType.Int);
            _profCondInsertCommand.Parameters.Add("@COND_ID", SqlDbType.Int);
            _profCondInsertCommand.Parameters.Add("@VALUE", SqlDbType.NChar, 16);
            _profCondInsertCommand.Prepare();

            _paramInsertCommand =
                new SqlCommand(
                    "INSERT INTO [dbo].[PROF_PARAM](PROF_TESTTYPE_ID, PROF_ID, PARAM_ID, MIN_VAL, MAX_VAL) VALUES(@PROF_TESTTYPE_ID, @PROF_ID, @PARAM_ID, @MIN_VAL, @MAX_VAL)",
                    _connection);
            _paramInsertCommand.Parameters.Add("@PROF_TESTTYPE_ID", SqlDbType.Int);
            _paramInsertCommand.Parameters.Add("@PROF_ID", SqlDbType.Int);
            _paramInsertCommand.Parameters.Add("@PARAM_ID", SqlDbType.Int);
            _paramInsertCommand.Parameters.Add("@MIN_VAL", SqlDbType.Float);
            _paramInsertCommand.Parameters.Add("@MAX_VAL", SqlDbType.Float);
            _paramInsertCommand.Prepare();
        }

        private readonly Dictionary<string, long> _testTypes;
        private readonly Dictionary<string, long> _conditions;
        private readonly Dictionary<string, long> _params;
        private int orderNew;

        /// <summary>
        /// Populate dictionaries from db
        /// </summary>
        private void PopulateDictionaries()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _testTypes.Clear();
                _conditions.Clear();
                _params.Clear();

                using (var condCmd = new SqlCommand("SELECT C.[COND_ID], RTRIM(C.[COND_NAME]) FROM [dbo].[CONDITIONS] C", _connection))
                {
                    using (var reader = condCmd.ExecuteReader())
                    {
                        while (reader.Read())
                            _conditions.Add(((string)reader[1]), (int)reader[0]);
                    }
                }

                using (var paramCmd = new SqlCommand("SELECT P.[PARAM_ID], RTRIM(P.[PARAM_NAME]) FROM [dbo].[PARAMS] P", _connection))
                {
                    using (var reader = paramCmd.ExecuteReader())
                    {
                        while (reader.Read())
                            _params.Add(((string)reader[1]), (int)reader[0]);
                    }
                }

                using (var typesCmd = new SqlCommand("SELECT T.[TEST_TYPE_ID], RTRIM(T.[TEST_TYPE_NAME]) FROM [dbo].[TEST_TYPE] T", _connection))
                {
                    using (var reader = typesCmd.ExecuteReader())
                    {
                        while (reader.Read())
                            _testTypes.Add(((string)reader[1]), (int)reader[0]);
                    }
                }
            }
        }

        /// <summary>
        /// Saving profile to Db
        /// </summary>
        /// <param name="profileItem"></param>
        public long SaveProfileItem(ProfileItem profileItem)
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                var trans = _connection.BeginTransaction();

                try
                {
                    orderNew = 0;
                    var profileId = InsertProfile(profileItem, trans);

                    var commutationTestTypeId = InsertCommutationTestType(profileId, trans);
                    InsertCommutationConditions(profileItem, commutationTestTypeId, profileId, trans);

                    var clampingTestTypeId = InsertClampingTestType(profileId, trans);
                    InsertClampingConditions(profileItem, clampingTestTypeId, profileId, trans);

                    foreach (var gateTestParameter in profileItem.GateTestParameters)
                    {
                        var testTypeId = InsertGateTestType(profileId, gateTestParameter.Order, trans);
                        gateTestParameter.IsEnabled = true;
                        InsertConditions(gateTestParameter, testTypeId, profileId, trans);
                        InsertParameters(gateTestParameter, testTypeId, profileId, trans);
                    }

                    foreach (var vtmTestParameter in profileItem.VTMTestParameters)
                    {
                        var testTypeId = InsertSlTestType(profileId, vtmTestParameter.Order, trans);
                        vtmTestParameter.IsEnabled = true;
                        InsertConditions(vtmTestParameter, testTypeId, profileId, trans);
                        InsertParameters(vtmTestParameter, testTypeId, profileId, trans);
                    }

                    foreach (var bvtTestParameter in profileItem.BVTTestParameters)
                    {
                        var testTypeId = InsertBvtTestType(profileId, bvtTestParameter.Order, trans);
                        bvtTestParameter.IsEnabled = true;
                        InsertConditions(bvtTestParameter, testTypeId, profileId, trans);
                        InsertParameters(bvtTestParameter, testTypeId, profileId, trans);
                    }

                    foreach (var dvDTestParameter in profileItem.DvDTestParameterses)
                    {
                        var testTypeId = InsertDvdtTestType(profileId, dvDTestParameter.Order, trans);
                        dvDTestParameter.IsEnabled = true;
                        InsertConditions(dvDTestParameter, testTypeId, profileId, trans);
                        InsertParameters(dvDTestParameter, testTypeId, profileId, trans);
                    }

                    foreach (var atuTestParameter in profileItem.ATUTestParameters)
                    {
                        var testTypeId = InsertAtuTestType(profileId, atuTestParameter.Order, trans);
                        atuTestParameter.IsEnabled = true;
                        InsertConditions(atuTestParameter, testTypeId, profileId, trans);
                        InsertParameters(atuTestParameter, testTypeId, profileId, trans);
                    }

                    foreach (var qrrTqTestParameter in profileItem.QrrTqTestParameters)
                    {
                        var testTypeId = InsertQrrTqTestType(profileId, qrrTqTestParameter.Order, trans);
                        qrrTqTestParameter.IsEnabled = true;
                        InsertConditions(qrrTqTestParameter, testTypeId, profileId, trans);
                        InsertParameters(qrrTqTestParameter, testTypeId, profileId, trans);
                    }

                    foreach (var racTestParameter in profileItem.RACTestParameters)
                    {
                        var testTypeId = InsertRacTestType(profileId, racTestParameter.Order, trans);
                        racTestParameter.IsEnabled = true;
                        InsertConditions(racTestParameter, testTypeId, profileId, trans);
                        InsertParameters(racTestParameter, testTypeId, profileId, trans);
                    }

                    trans.Commit();

                    return profileId;
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }

            return 0;
        }

        public void DeleteProfiles(List<ProfileItem> profilesToDelete)
        {
            foreach (var profileItem in profilesToDelete)
            {
                try
                {
                    var profileId = GetProfileId(profileItem.ProfileKey, null);

                    _profileDeleteCommand.Parameters["@PROF_ID"].Value = profileId;
                    _profileDeleteCommand.ExecuteNonQuery();
                }
                catch (ArgumentException)
                {
                }
            }
        }

        public void DeleteProfiles(List<ProfileItem> profilesToDelete, string mmeCode)
        {
            foreach (var profileItem in profilesToDelete)
            {
                try
                {
                    var profileId = GetProfileId(profileItem.ProfileKey, null);
                    var mmeCodeId = GetMmeCodeId(mmeCode);

                    _condDeleteCommand.Parameters["@PROF_ID"].Value = profileId;
                    _condDeleteCommand.Parameters["@MME_CODE_ID"].Value = mmeCodeId;
                    _condDeleteCommand.ExecuteNonQuery();
                }
                catch (ArgumentException)
                {
                }
            }
        }

        private long GetMmeCodeId(string mmeCode)
        {
            _mmeSelectCommand.Parameters["@MME_CODE"].Value = mmeCode;
            var posibleMmeCode = _mmeSelectCommand.ExecuteScalar();

            if (posibleMmeCode != null)
                return (int)posibleMmeCode;

            _mmeInsertCommand.Parameters["@MME_CODE"].Value = mmeCode;
            return (int)_mmeInsertCommand.ExecuteScalar();
        }

        public void SaveProfileItem(ProfileItem profileItem, string mmeCode)
        {
            var profileId = SaveProfileItem(profileItem);
            var mmeCodeId = GetMmeCodeId(mmeCode);

            _codesInsertCommand.Parameters["@MME_CODE_ID"].Value = mmeCodeId;
            _codesInsertCommand.Parameters["@PROFILE_ID"].Value = profileId;
            _codesInsertCommand.ExecuteScalar();
        }

        #region ProfileHelpers

        private long InsertProfile(ProfileItem profile, SqlTransaction trans)
        {
            try
            {
                var profileId = GetProfileId(profile.ProfileKey, trans);

                _profileVersionSelect.Parameters["@PROF_ID"].Value = profileId;
                _profileVersionSelect.Transaction = trans;
                var version = _profileVersionSelect.ExecuteScalar();

                _profileInsertCommand.Parameters["@PROF_GUID"].Value = Guid.NewGuid();
                _profileInsertCommand.Parameters["@VERSION"].Value = (int)version + 1;
                _profileInsertCommand.Parameters["@PROF_TS"].Value = DateTime.Now;
                _profileInsertCommand.Parameters["@PROF_NAME"].Value = profile.ProfileName;
                _profileInsertCommand.Transaction = trans;
                var insertedId = (int)_profileInsertCommand.ExecuteScalar();

                UpdateConnections(profileId, insertedId, trans);

                return insertedId;
            }
            catch (ArgumentException)
            {
                _profileInsertCommand.Parameters["@PROF_NAME"].Value = profile.ProfileName;
                _profileInsertCommand.Parameters["@PROF_GUID"].Value = profile.ProfileKey;
                _profileInsertCommand.Parameters["@PROF_TS"].Value = DateTime.Now;
                _profileInsertCommand.Parameters["@VERSION"].Value = 1;
                _profileInsertCommand.Transaction = trans;

                return (int)_profileInsertCommand.ExecuteScalar();
            }
        }

        private void UpdateConnections(long profileId, long insertedId, SqlTransaction trans)
        {
            _connectionsUpdateCommand.Parameters["@NEW_PROFILE_ID"].Value = insertedId;
            _connectionsUpdateCommand.Parameters["@PROFILE_ID"].Value = profileId;
            _connectionsUpdateCommand.Transaction = trans;
            _connectionsUpdateCommand.ExecuteNonQuery();
        }

        private long GetProfileId(Guid profileKey, SqlTransaction trans)
        {
            _profileSelectCommand.Parameters["@PROF_GUID"].Value = profileKey;
            _profileSelectCommand.Transaction = trans;
            var possibleProfileId = _profileSelectCommand.ExecuteScalar();

            if (possibleProfileId == null)
                throw new ArgumentException(@"No such baseTestParametersAndNormatives has been found", nameof(profileKey));

            return (int)possibleProfileId;
        }

        #endregion

        #region TestTypeHelpers

        private long InsertCommutationTestType(long profileId, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["Commutation"], profileId, trans);
        }

        private long InsertClampingTestType(long profileId, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["Clamping"], profileId, trans);
        }

        private long InsertGateTestType(long profileId, int order, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["Gate"], profileId, trans, order);
        }

        private long InsertBvtTestType(long profileId, int order, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["BVT"], profileId, trans, order);
        }

        private long InsertSlTestType(long profileId, int order, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["SL"], profileId, trans, order);
        }

        private long InsertDvdtTestType(long profileId, int order, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["Dvdt"], profileId, trans, order);
        }

        private long InsertAtuTestType(long profileId, int order, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["ATU"], profileId, trans, order);
        }

        private long InsertQrrTqTestType(long profileId, int order, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["QrrTq"], profileId, trans, order);
        }

        private long InsertRacTestType(long profileId, int order, SqlTransaction trans)
        {
            return InsertTestType(_testTypes["RAC"], profileId, trans, order);
        }

        private long InsertTestType(long typeId, long profileId, SqlTransaction trans, int order = 0)
        {
            orderNew++;
            _profTestTypeInsertCommand.Parameters["@PROF_ID"].Value = profileId;
            _profTestTypeInsertCommand.Parameters["@TEST_TYPE_ID"].Value = typeId;
            _profTestTypeInsertCommand.Parameters["@ORD"].Value = (order == 0) ? orderNew : order;
            _profTestTypeInsertCommand.Transaction = trans;

            return (int)_profTestTypeInsertCommand.ExecuteScalar();
        }

        #endregion

        #region ConditionsHelpers

        private void InsertCommutationConditions(ProfileItem profile, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "COMM_Type", profile.CommTestParameters, trans);
        }

        private void InsertClampingConditions(ProfileItem profile, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "CLAMP_Type", profile.ClampingForce, trans);
            InsertCondition(testTypeId, profileId, "CLAMP_Force", profile.ParametersClamp, trans);
            InsertCondition(testTypeId, profileId, "CLAMP_HeightMeasure", profile.IsHeightMeasureEnabled, trans);
            InsertCondition(testTypeId, profileId, "CLAMP_HeightValue", profile.Height, trans);
            InsertCondition(testTypeId, profileId, "CLAMP_Temperature", profile.Temperature, trans);
        }

        private void InsertConditions(BaseTestParametersAndNormatives baseTestParametersAndNormatives, long testTypeId, long profileId, SqlTransaction trans)
        {
            switch (baseTestParametersAndNormatives.TestParametersType)
            {
                case TestParametersType.Gate:
                    InsertGateConditions(baseTestParametersAndNormatives as TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.StaticLoses:
                    InsertSlConditions(baseTestParametersAndNormatives as Types.SL.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.Bvt:
                    InsertBvtConditions(baseTestParametersAndNormatives as Types.BVT.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.Dvdt:
                    InsertDvdtConditions(baseTestParametersAndNormatives as Types.dVdt.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.ATU:
                    InsertAtuConditions(baseTestParametersAndNormatives as Types.ATU.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.QrrTq:
                    InsertQrrTqConditions(baseTestParametersAndNormatives as Types.QrrTq.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.RAC:
                    InsertRacConditions(baseTestParametersAndNormatives as Types.RAC.TestParameters, testTypeId, profileId, trans);
                    break;
            }
        }

        private void InsertGateConditions(TestParameters profile, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "Gate_En", profile.IsEnabled, trans);
            InsertCondition(testTypeId, profileId, "Gate_EnableCurrent", profile.IsCurrentEnabled, trans);
            InsertCondition(testTypeId, profileId, "Gate_IHEn", profile.IsIhEnabled, trans);
            InsertCondition(testTypeId, profileId, "Gate_ILEn", profile.IsIlEnabled, trans);
            InsertCondition(testTypeId, profileId, "Gate_EnableIHStrike", profile.IsIhStrikeCurrentEnabled, trans);
        }

        private void InsertSlConditions(Types.SL.TestParameters profile, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "SL_En", profile.IsEnabled, trans);
            InsertCondition(testTypeId, profileId, "SL_Type", profile.TestType, trans);
            InsertCondition(testTypeId, profileId, "SL_FS", profile.UseFullScale, trans);
            InsertCondition(testTypeId, profileId, "SL_N", profile.Count, trans);

            switch (profile.TestType)
            {
                case VTMTestType.Ramp:
                    InsertCondition(testTypeId, profileId, "SL_ITM", profile.RampCurrent, trans);
                    InsertCondition(testTypeId, profileId, "SL_Time", profile.RampTime, trans);
                    InsertCondition(testTypeId, profileId, "SL_OpenEn", profile.IsRampOpeningEnabled, trans);
                    InsertCondition(testTypeId, profileId, "SL_OpenI", profile.RampOpeningCurrent, trans);
                    InsertCondition(testTypeId, profileId, "SL_TimeEx", profile.RampOpeningTime, trans);
                    break;
                case VTMTestType.Sinus:
                    InsertCondition(testTypeId, profileId, "SL_ITM", profile.SinusCurrent, trans);
                    InsertCondition(testTypeId, profileId, "SL_Time", profile.SinusTime, trans);
                    break;
                case VTMTestType.Curve:
                    InsertCondition(testTypeId, profileId, "SL_ITM", profile.CurveCurrent, trans);
                    InsertCondition(testTypeId, profileId, "SL_Time", profile.CurveTime, trans);
                    InsertCondition(testTypeId, profileId, "SL_Factor", profile.CurveFactor, trans);
                    InsertCondition(testTypeId, profileId, "SL_TimeEx", profile.CurveAddTime, trans);
                    break;
            }
        }

        private void InsertBvtConditions(Types.BVT.TestParameters profile, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "BVT_En", profile.IsEnabled, trans);
            InsertCondition(testTypeId, profileId, "BVT_Type", profile.TestType, trans);
            InsertCondition(testTypeId, profileId, "BVT_I", profile.CurrentLimit, trans);
            InsertCondition(testTypeId, profileId, "BVT_RumpUp", profile.RampUpVoltage, trans);
            InsertCondition(testTypeId, profileId, "BVT_StartV", profile.StartVoltage, trans);
            InsertCondition(testTypeId, profileId, "BVT_F", profile.VoltageFrequency, trans);
            InsertCondition(testTypeId, profileId, "BVT_FD", profile.FrequencyDivisor, trans);
            InsertCondition(testTypeId, profileId, "BVT_Mode", profile.MeasurementMode, trans);
            InsertCondition(testTypeId, profileId, "BVT_PlateTime", profile.PlateTime, trans);
            InsertCondition(testTypeId, profileId, "BVT_UseUdsmUrsm", profile.UseUdsmUrsm, trans);

            switch (profile.TestType)
            {
                case BVTTestType.Both:
                    InsertCondition(testTypeId, profileId, "BVT_VD", profile.VoltageLimitD, trans);
                    InsertCondition(testTypeId, profileId, "BVT_VR", profile.VoltageLimitR, trans);
                    break;
                case BVTTestType.Direct:
                    InsertCondition(testTypeId, profileId, "BVT_VD", profile.VoltageLimitD, trans);
                    break;
                case BVTTestType.Reverse:
                    InsertCondition(testTypeId, profileId, "BVT_VR", profile.VoltageLimitR, trans);
                    break;
            }

        }

        private void InsertDvdtConditions(Types.dVdt.TestParameters testParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "DVDT_En", testParameters.IsEnabled, trans);
            InsertCondition(testTypeId, profileId, "DVDT_Mode", testParameters.Mode, trans);
            switch (testParameters.Mode)
            {
                case DvdtMode.Confirmation:
                    InsertCondition(testTypeId, profileId, "DVDT_Voltage", testParameters.Voltage, trans);
                    InsertCondition(testTypeId, profileId, "DVDT_VoltageRate", testParameters.VoltageRate, trans);
                    InsertCondition(testTypeId, profileId, "DVDT_ConfirmationCount", testParameters.ConfirmationCount, trans);
                    break;
                case DvdtMode.Detection:
                    InsertCondition(testTypeId, profileId, "DVDT_Voltage", testParameters.Voltage, trans);
                    InsertCondition(testTypeId, profileId, "DVDT_VoltageRateLimit", testParameters.VoltageRateLimit, trans);
                    InsertCondition(testTypeId, profileId, "DVDT_VoltageRateOffSet", testParameters.VoltageRateOffSet, trans);
                    break;
            }
        }

        private void InsertAtuConditions(Types.ATU.TestParameters testParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "ATU_En", testParameters.IsEnabled, trans);
            InsertCondition(testTypeId, profileId, "ATU_PrePulseValue", testParameters.PrePulseValue, trans);
            InsertCondition(testTypeId, profileId, "ATU_PowerValue", testParameters.PowerValue, trans);
        }

        private void InsertQrrTqConditions(Types.QrrTq.TestParameters testParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "QrrTq_En", testParameters.IsEnabled, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_Mode", testParameters.Mode, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_TrrMeasureBy9050Method", testParameters.TrrMeasureBy9050Method, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_DirectCurrent", testParameters.DirectCurrent, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_DCPulseWidth", testParameters.DCPulseWidth, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_DCRiseRate", testParameters.DCRiseRate, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_DCFallRate", (uint)testParameters.DCFallRate, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_OffStateVoltage", testParameters.OffStateVoltage, trans);
            InsertCondition(testTypeId, profileId, "QrrTq_OsvRate", (uint)testParameters.OsvRate, trans);
        }

        private void InsertRacConditions(Types.RAC.TestParameters testParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertCondition(testTypeId, profileId, "RAC_En", testParameters.IsEnabled, trans);
            InsertCondition(testTypeId, profileId, "RAC_ResVoltage", testParameters.ResVoltage, trans);
        }

        private void InsertCondition(long testTypeId, long profileId, string name, object value, SqlTransaction trans)
        {
            _profCondInsertCommand.Parameters["@PROF_TESTTYPE_ID"].Value = testTypeId;
            _profCondInsertCommand.Parameters["@PROF_ID"].Value = profileId;
            _profCondInsertCommand.Parameters["@COND_ID"].Value = _conditions[name];
            _profCondInsertCommand.Parameters["@VALUE"].Value = value.ToString();
            _profCondInsertCommand.Transaction = trans;

            _profCondInsertCommand.ExecuteNonQuery();
        }

        #endregion

        #region ParametersHelpers

        private void InsertParameters(BaseTestParametersAndNormatives baseTestParametersAndNormatives, long testTypeId, long profileId, SqlTransaction trans)
        {
            switch (baseTestParametersAndNormatives.TestParametersType)
            {
                case TestParametersType.Gate:
                    InsertGateParameters(baseTestParametersAndNormatives as TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.StaticLoses:
                    InsertVtmParameters(baseTestParametersAndNormatives as Types.SL.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.Bvt:
                    InsertBvtParameters(baseTestParametersAndNormatives as Types.BVT.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.ATU:
                    InsertAtuParameters(baseTestParametersAndNormatives as Types.ATU.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.QrrTq:
                    InsertQrrTqParameters(baseTestParametersAndNormatives as Types.QrrTq.TestParameters, testTypeId, profileId, trans);
                    break;
                case TestParametersType.RAC:
                    InsertRacParameters(baseTestParametersAndNormatives as Types.RAC.TestParameters, testTypeId, profileId, trans);
                    break;
            }
        }

        private void InsertGateParameters(TestParameters gateTestParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertParameter(testTypeId, profileId, "RG", DBNull.Value, gateTestParameters.Resistance, trans);
            InsertParameter(testTypeId, profileId, "IGT", DBNull.Value, gateTestParameters.IGT, trans);
            InsertParameter(testTypeId, profileId, "VGT", DBNull.Value, gateTestParameters.VGT, trans);

            if (gateTestParameters.IsIhEnabled)
                InsertParameter(testTypeId, profileId, "IH", DBNull.Value, gateTestParameters.IH, trans);
            if (gateTestParameters.IsIlEnabled)
                InsertParameter(testTypeId, profileId, "IL", DBNull.Value, gateTestParameters.IL, trans);
        }

        private void InsertVtmParameters(Types.SL.TestParameters vtmTestParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertParameter(testTypeId, profileId, "VTM", DBNull.Value, vtmTestParameters.VTM, trans);
        }

        private void InsertBvtParameters(Types.BVT.TestParameters bvtTestParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            if (bvtTestParameters.MeasurementMode == BVTMeasurementMode.ModeV)
            {
                InsertParameter(testTypeId, profileId, "VRRM", bvtTestParameters.VRRM, DBNull.Value, trans);

                if (bvtTestParameters.TestType != BVTTestType.Reverse)
                    InsertParameter(testTypeId, profileId, "VDRM", bvtTestParameters.VDRM, DBNull.Value, trans);
            }
            else
            {
                InsertParameter(testTypeId, profileId, "IRRM", DBNull.Value, bvtTestParameters.IRRM, trans);

                if (bvtTestParameters.TestType != BVTTestType.Reverse)
                    InsertParameter(testTypeId, profileId, "IDRM", DBNull.Value, bvtTestParameters.IDRM, trans);
            }
        }

        private void InsertAtuParameters(Types.ATU.TestParameters atuTestParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertParameter(testTypeId, profileId, "UBR", atuTestParameters.UBR, DBNull.Value, trans);
            InsertParameter(testTypeId, profileId, "UPRSM", atuTestParameters.UPRSM, DBNull.Value, trans);
            InsertParameter(testTypeId, profileId, "IPRSM", atuTestParameters.IPRSM, DBNull.Value, trans);
            InsertParameter(testTypeId, profileId, "PRSM", atuTestParameters.PRSM, DBNull.Value, trans);
        }

        private void InsertQrrTqParameters(Types.QrrTq.TestParameters qrrTqTestParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertParameter(testTypeId, profileId, "IDC", qrrTqTestParameters.Idc, DBNull.Value, trans);
            InsertParameter(testTypeId, profileId, "QRR", qrrTqTestParameters.Qrr, DBNull.Value, trans);
            InsertParameter(testTypeId, profileId, "IRR", qrrTqTestParameters.Irr, DBNull.Value, trans);
            InsertParameter(testTypeId, profileId, "TRR", qrrTqTestParameters.Trr, DBNull.Value, trans);
            InsertParameter(testTypeId, profileId, "DCFactFallRate", qrrTqTestParameters.DCFactFallRate, DBNull.Value, trans);

            if (qrrTqTestParameters.Mode == Types.QrrTq.TMode.QrrTq)
                InsertParameter(testTypeId, profileId, "TQ", qrrTqTestParameters.Tq, DBNull.Value, trans);
        }

        private void InsertRacParameters(Types.RAC.TestParameters racTestParameters, long testTypeId, long profileId, SqlTransaction trans)
        {
            InsertParameter(testTypeId, profileId, "ResultR", racTestParameters.ResultR, DBNull.Value, trans);
        }

        private void InsertParameter(long testTypeId, long profileId, string name, object min, object max, SqlTransaction trans)
        {
            _paramInsertCommand.Parameters["@PROF_TESTTYPE_ID"].Value = testTypeId;
            _paramInsertCommand.Parameters["@PROF_ID"].Value = profileId;
            _paramInsertCommand.Parameters["@PARAM_ID"].Value = _params[name];
            _paramInsertCommand.Parameters["@MIN_VAL"].Value = min;
            _paramInsertCommand.Parameters["@MAX_VAL"].Value = max;
            _paramInsertCommand.Transaction = trans;

            _paramInsertCommand.ExecuteNonQuery();
        }

        #endregion
    }
}
