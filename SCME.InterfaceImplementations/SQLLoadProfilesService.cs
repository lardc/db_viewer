using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Forms;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.BVT;
using SCME.Types.Commutation;
using SCME.Types.dVdt;
using SCME.Types.Interfaces;
using SCME.Types.Profiles;
using SCME.Types.SL;
using TestParameters = SCME.Types.Gate.TestParameters;

namespace SCME.InterfaceImplementations
{
    public class SQLLoadProfilesService : ILoadProfilesService
    {
        private readonly SqlConnection _connection;
        private SqlCommand _childsCmd;
        private SqlCommand _condCmd;
        private SqlCommand _profileSelect;
        private SqlCommand _orderSelect;
        private SqlCommand _condSelect;
        private SqlCommand _paramSelect;
        private SqlCommand _profileSingleSelect;

        private int a = 0;

        public SQLLoadProfilesService(SqlConnection connection)
        {
            _connection = connection;
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            PrepareQueries();
        }

        private void PrepareQueries()
        {
            _childsCmd =
                new SqlCommand(
                    @"SELECT [PROF_ID], [PROF_NAME], [PROF_GUID], [PROF_TS], [PROF_VERS] FROM [dbo].[PROFILES] WHERE [PROF_NAME] = @PROF_NAME ORDER BY [PROF_TS] DESC",
                    _connection);
            _childsCmd.Parameters.Add("@PROF_NAME", SqlDbType.NVarChar, 32);
            _childsCmd.Prepare();

            _condCmd =
                new SqlCommand(
                    @"SELECT [PTT_ID], [TEST_TYPE_ID] FROM [dbo].[PROF_TEST_TYPE] WHERE [PROF_ID] = @PROF_ID",
                    _connection);
            _condCmd.Parameters.Add("@PROF_ID", SqlDbType.Int);
            _condCmd.Prepare();

            /*
            _profileSelect =
                new SqlCommand(@"SELECT P.[PROF_ID], P.[PROF_NAME], P.[PROF_GUID], PP.MAX_PROF_TS, P.[PROF_VERS] 
                                 FROM (SELECT [PROF_NAME], MAX([PROF_TS]) AS MAX_PROF_TS
                                       FROM [dbo].[PROFILES]
                                       GROUP BY [PROF_NAME]) PP 
                                  INNER JOIN [dbo].[PROFILES] P ON PP.PROF_NAME = P.PROF_NAME AND PP.MAX_PROF_TS = P.PROF_TS 
                                  JOIN [dbo].[MME_CODES_TO_PROFILES] MCP ON MCP.[PROFILE_ID] = P.[PROF_ID]
                                  JOIN [dbo].[MME_CODES] MC ON MC.[MME_CODE_ID] = MCP.[MME_CODE_ID]
                                 WHERE MC.[MME_CODE] = @MME_CODE",
                    _connection);
            */

            _profileSelect =
                new SqlCommand(@"SELECT P.PROF_ID, P.PROF_NAME, P.PROF_GUID, P.PROF_TS, P.PROF_VERS
                                 FROM (
                                        SELECT MAX(PR.PROF_ID) AS MAX_PROF_ID
                                        FROM PROFILES PR
                                        WHERE (ISNULL(PR.IS_DELETED, 0)=0)
                                        GROUP BY PR.PROF_NAME
	                                  ) PP
                                  INNER JOIN PROFILES P ON (P.PROF_ID=PP.MAX_PROF_ID)
	                              INNER JOIN MME_CODES_TO_PROFILES MCP ON (MCP.PROFILE_ID=P.PROF_ID)
                                  INNER JOIN MME_CODES MC ON (
                                                              (MC.MME_CODE_ID=MCP.MME_CODE_ID) AND
                                                              (MC.MME_CODE=@MME_CODE)
                                                             )", _connection);

            _profileSelect.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
            _profileSelect.Prepare();

            _orderSelect = new SqlCommand(@"SELECT [ORD] FROM [dbo].[PROF_TEST_TYPE] WHERE [PTT_ID] = @TEST_TYPE_ID",
                _connection);
            _orderSelect.Parameters.Add("@TEST_TYPE_ID", SqlDbType.Int);
            _orderSelect.Prepare();

            _condSelect =
                new SqlCommand(
                    "SELECT C.[COND_NAME], PC.[VALUE] FROM [dbo].[PROF_COND] PC LEFT JOIN [dbo].[CONDITIONS] C on C.[COND_ID] = PC.[COND_ID] WHERE PC.[PROF_TESTTYPE_ID] = @TEST_TYPE_ID",
                    _connection);
            _condSelect.Parameters.Add("@TEST_TYPE_ID", SqlDbType.Int);
            _condSelect.Prepare();

            _paramSelect =
                new SqlCommand(
                    "SELECT P.[PARAM_NAME], PP.[MIN_VAL], PP.[MAX_VAL] FROM [dbo].[PROF_PARAM] PP LEFT JOIN [dbo].[PARAMS] P on P.[PARAM_ID] = PP.[PARAM_ID] WHERE PP.[PROF_TESTTYPE_ID] = @TEST_TYPE_ID",
                    _connection);
            _paramSelect.Parameters.Add("@TEST_TYPE_ID", SqlDbType.Int);
            _paramSelect.Prepare();

            //чтение одного профиля последней редакции по его PROF_NAME и MME_CODE
            _profileSingleSelect = new SqlCommand(@"SELECT P.PROF_ID, P.PROF_NAME, P.PROF_GUID, P.PROF_TS
                                                    FROM (
                                                           SELECT MAX(PR.PROF_ID) AS MAX_PROF_ID
                                                           FROM PROFILES PR
                                                           WHERE (
                                                                  (PR.PROF_NAME=@ProfName) AND
                                                                  (ISNULL(PR.IS_DELETED, 0)=0)
                                                                 )
                                                         ) PP
                                                     INNER JOIN PROFILES P ON (P.PROF_ID=PP.MAX_PROF_ID)
                                                     INNER JOIN MME_CODES_TO_PROFILES MCP ON (MCP.PROFILE_ID=P.PROF_ID)
                                                     INNER JOIN MME_CODES MC ON (
                                                                                 (MC.MME_CODE_ID=MCP.MME_CODE_ID) AND
                                                                                 (MC.MME_CODE=@MmmeCode)
                                                                                )", _connection);

            _profileSingleSelect.Parameters.Add("@ProfName", SqlDbType.NVarChar, 32);
            _profileSingleSelect.Parameters.Add("@MmmeCode", SqlDbType.NVarChar, 64);
            _profileSingleSelect.Prepare();
        }

        public List<ProfileItem> GetProfileItems()
        {
            try
            {
                var profilesList = new List<ProfileItem>();
                const string profilesSelect = "SELECT P.[PROF_ID], P.[PROF_NAME], P.[PROF_GUID], PP.MAX_PROF_TS " +
                                              " FROM (SELECT [PROF_NAME], MAX([PROF_TS]) AS MAX_PROF_TS FROM [dbo].[PROFILES] WHERE (ISNULL([IS_DELETED], 0)=0) GROUP BY [PROF_NAME]) PP " +
                                              " INNER JOIN [dbo].[PROFILES] P ON PP.PROF_NAME = P.PROF_NAME AND PP.MAX_PROF_TS = P.PROF_TS";
                var profilesDict = new List<Tuple<int, string, Guid, DateTime>>();

                using (var condCmd = _connection.CreateCommand())
                {
                    condCmd.CommandText = profilesSelect;

                    a++;
                    using (var reader = condCmd.ExecuteReader())
                    {
                        while (reader.Read())
                            profilesDict.Add(new Tuple<int, string, Guid, DateTime>((int)reader[0], (string)reader[1], (Guid)reader[2], (DateTime)reader[3]));
                    }
                }

                foreach (var prof in profilesDict)
                {
                    var profile = Profile(prof);

                    var profilesChildsDict = new List<Tuple<int, string, Guid, DateTime>>();

                    _childsCmd.Parameters["@PROF_NAME"].Value = prof.Item2;

                    a++;
                    using (var reader = _childsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                            profilesChildsDict.Add(new Tuple<int, string, Guid, DateTime>((int)reader[0], (string)reader[1], (Guid)reader[2], (DateTime)reader[3]));
                    }

                    var profileItem = new ProfileItem
                    {
                        ProfileId = prof.Item1,
                        ProfileName = profile.Name,
                        ProfileKey = profile.Key,
                        ProfileTS = profile.Timestamp,
                        GateTestParameters = new List<TestParameters>(),
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
                        Temperature = profile.Temperature,
                        ChildProfileItems = new List<ProfileItem>(profilesChildsDict.Count - 1)
                    };

                    AddParameters(profile, profileItem);
                    for (int i = 0; i < profilesChildsDict.Count; i++)
                    {
                        if (i == 0)
                            continue;
                        var childProfile = Profile(profilesChildsDict[i]);

                        var childProfileItem = new ProfileItem
                        {
                            ProfileId = profilesChildsDict[i].Item1,
                            ProfileName = childProfile.Name,
                            ProfileKey = childProfile.Key,
                            ProfileTS = childProfile.Timestamp,
                            GateTestParameters = new List<TestParameters>(),
                            VTMTestParameters = new List<Types.SL.TestParameters>(),
                            BVTTestParameters = new List<Types.BVT.TestParameters>(),
                            DvDTestParameterses = new List<Types.dVdt.TestParameters>(),
                            ATUTestParameters = new List<Types.ATU.TestParameters>(),
                            QrrTqTestParameters = new List<Types.QrrTq.TestParameters>(),
                            RACTestParameters = new List<Types.RAC.TestParameters>(),
                            CommTestParameters = childProfile.ParametersComm,
                            IsHeightMeasureEnabled = childProfile.IsHeightMeasureEnabled,
                            ParametersClamp = childProfile.ParametersClamp,
                            Height = childProfile.Height,
                            Temperature = childProfile.Temperature
                        };
                        AddParameters(childProfile, childProfileItem);

                        profileItem.ChildProfileItems.Add(childProfileItem);
                    }

                    profilesList.Add(profileItem);
                }

                //                MessageBox.Show(a.ToString());

                return profilesList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void AddParameters(Profile profile, ProfileItem profileItem)
        {
            foreach (var baseTestParametersAndNormativese in profile.TestParametersAndNormatives)
            {
                var gate = baseTestParametersAndNormativese as TestParameters;
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
        }

        private Profile Profile(Tuple<int, string, Guid, DateTime> prof)
        {
            var profile = new Profile(prof.Item2, prof.Item3, prof.Item4);

            var testTypes = new Dictionary<long, long>(5);

            _condCmd.Parameters["@PROF_ID"].Value = prof.Item1;

            a++;
            using (var reader = _condCmd.ExecuteReader())
            {
                while (reader.Read())
                    testTypes.Add((int)reader[0], (int)reader[1]);
            }

            foreach (var testType in testTypes)
                FillParameters(profile, testType.Key, testType.Value);

            return profile;
        }

        public List<ProfileItem> GetProfileItems(string mmeCode)
        {
            try
            {
                var profilesList = new List<ProfileItem>();
                var profilesDict = new List<Tuple<int, string, Guid, DateTime>>();

                _profileSelect.Parameters["@MME_CODE"].Value = mmeCode;

                a++;
                using (var reader = _profileSelect.ExecuteReader())
                {
                    while (reader.Read())
                        profilesDict.Add(new Tuple<int, string, Guid, DateTime>((int)reader[0], (string)reader[1], (Guid)reader[2], (DateTime)reader[3]));
                }

                foreach (var prof in profilesDict)
                {
                    var profile = new Profile(prof.Item2, prof.Item3, prof.Item4);

                    var testTypes = new Dictionary<long, long>(5);

                    _condCmd.Parameters["@PROF_ID"].Value = prof.Item1;

                    a++;
                    using (var reader = _condCmd.ExecuteReader())
                    {
                        while (reader.Read())
                            testTypes.Add((int)reader[0], (int)reader[1]);
                    }

                    foreach (var testType in testTypes)
                    {
                        FillParameters(profile, testType.Key, testType.Value);
                    }

                    var profileItem = new ProfileItem
                    {
                        ProfileId = prof.Item1,
                        ProfileName = profile.Name,
                        ProfileKey = profile.Key,
                        ProfileTS = profile.Timestamp,
                        GateTestParameters = new List<TestParameters>(),
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
                        var gate = baseTestParametersAndNormativese as TestParameters;
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

                    profilesList.Add(profileItem);
                }

                return profilesList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void FillParameters(Profile profile, long testTypeId, long testParametersType)
        {
            switch (testParametersType)
            {
                case (int)TestParametersType.Gate:
                    var gatePars = FillGateConditions(testTypeId);
                    FillGateParameters(gatePars, testTypeId);
                    profile.TestParametersAndNormatives.Add(gatePars);
                    break;

                case (int)TestParametersType.Bvt:
                    var bvtPars = FillBvtConditions(testTypeId);
                    FillBvtParameters(bvtPars, testTypeId);
                    profile.TestParametersAndNormatives.Add(bvtPars);
                    break;

                case (int)TestParametersType.StaticLoses:
                    var slParams = FillSlConditions(testTypeId);
                    FillSlParameters(slParams, testTypeId);
                    profile.TestParametersAndNormatives.Add(slParams);
                    break;

                case (int)TestParametersType.Dvdt:
                    var dVdtParams = FillDvdtConditions(testTypeId);
                    profile.TestParametersAndNormatives.Add(dVdtParams);
                    break;

                case (int)TestParametersType.ATU:
                    var atuParams = FillAtuConditions(testTypeId);
                    profile.TestParametersAndNormatives.Add(atuParams);
                    break;

                case (int)TestParametersType.QrrTq:
                    var qrrTqParams = FillQrrTqConditions(testTypeId);
                    profile.TestParametersAndNormatives.Add(qrrTqParams);
                    break;

                case (int)TestParametersType.RAC:
                    var racParams = FillRACConditions(testTypeId);
                    profile.TestParametersAndNormatives.Add(racParams);
                    break;

                case (int)TestParametersType.Clamping:
                    FillClampConditions(profile, testTypeId);
                    break;

                case (int)TestParametersType.Commutation:
                    FillComutationConditions(profile, testTypeId);
                    break;
            }
        }

        private Types.dVdt.TestParameters FillDvdtConditions(long testTypeId)
        {
            var results = new Dictionary<string, object>(3);
            var testParams = new Types.dVdt.TestParameters() { IsEnabled = true, TestTypeId = testTypeId };

            FillOrder(testTypeId, testParams);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "DVDT_En":
                        testParams.IsEnabled = Boolean.Parse(result.Value.ToString());
                        break;
                    case "DVDT_Mode":
                        testParams.Mode = (DvdtMode)Enum.Parse(typeof(DvdtMode), result.Value.ToString());
                        break;
                    case "DVDT_Voltage":
                        testParams.Voltage = UInt16.Parse(result.Value.ToString());
                        break;
                    case "DVDT_VoltageRate":
                        testParams.VoltageRate = (VoltageRate)Enum.Parse(typeof(VoltageRate), result.Value.ToString());
                        break;
                    case "DVDT_ConfirmationCount":
                        testParams.ConfirmationCount = UInt16.Parse(result.Value.ToString());
                        break;
                    case "DVDT_VoltageRateLimit":
                        testParams.VoltageRateLimit = UInt16.Parse(result.Value.ToString());
                        break;
                    case "DVDT_VoltageRateOffSet":
                        testParams.VoltageRateOffSet = UInt16.Parse(result.Value.ToString());
                        break;
                }
            }

            return testParams;
        }

        private Types.ATU.TestParameters FillAtuConditions(long testTypeId)
        {
            var results = new Dictionary<string, object>(3);
            var testParams = new Types.ATU.TestParameters() { IsEnabled = true, TestTypeId = testTypeId };

            FillOrder(testTypeId, testParams);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "ATU_En":
                        testParams.IsEnabled = Boolean.Parse(result.Value.ToString());
                        break;

                    case "ATU_PrePulseValue":
                        testParams.PrePulseValue = UInt16.Parse(result.Value.ToString());
                        break;

                    case "ATU_PowerValue":
                        testParams.PowerValue = float.Parse(result.Value.ToString());
                        break;
                }
            }

            return testParams;
        }

        private Types.QrrTq.TestParameters FillQrrTqConditions(long testTypeId)
        {
            var results = new Dictionary<string, object>(8);
            var testParams = new Types.QrrTq.TestParameters() { IsEnabled = true, TestTypeId = testTypeId };

            FillOrder(testTypeId, testParams);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "QrrTq_En":
                        testParams.IsEnabled = Boolean.Parse(result.Value.ToString());
                        break;

                    case "QrrTq_Mode":
                        testParams.Mode = (Types.QrrTq.TMode)Enum.Parse(typeof(Types.QrrTq.TMode), result.Value.ToString());
                        break;

                    case "QrrTq_TrrMeasureBy9050Method":
                        testParams.TrrMeasureBy9050Method = bool.Parse(result.Value.ToString());
                        break;

                    case "QrrTq_DirectCurrent":
                        testParams.DirectCurrent = UInt16.Parse(result.Value.ToString());
                        break;

                    case "QrrTq_DCPulseWidth":
                        testParams.DCPulseWidth = UInt16.Parse(result.Value.ToString());
                        break;

                    case "QrrTq_DCRiseRate":
                        testParams.DCRiseRate = float.Parse(result.Value.ToString());
                        break;

                    case "QrrTq_DCFallRate":
                        testParams.DCFallRate = (Types.QrrTq.TDcFallRate)Enum.Parse(typeof(Types.QrrTq.TDcFallRate), result.Value.ToString());
                        break;

                    case "QrrTq_OffStateVoltage":
                        testParams.OffStateVoltage = UInt16.Parse(result.Value.ToString());
                        break;

                    case "QrrTq_OsvRate":
                        testParams.OsvRate = (Types.QrrTq.TOsvRate)Enum.Parse(typeof(Types.QrrTq.TOsvRate), result.Value.ToString());
                        break;
                }
            }

            return testParams;
        }

        private Types.RAC.TestParameters FillRACConditions(long testTypeId)
        {
            var results = new Dictionary<string, object>(2);
            var testParams = new Types.RAC.TestParameters() { IsEnabled = true, TestTypeId = testTypeId };

            FillOrder(testTypeId, testParams);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "RAC_En":
                        testParams.IsEnabled = Boolean.Parse(result.Value.ToString());
                        break;

                    case "RAC_ResVoltage":
                        testParams.ResVoltage = UInt16.Parse(result.Value.ToString());
                        break;
                }
            }

            return testParams;
        }

        private void FillComutationConditions(Profile profile, long testTypeId)
        {
            var results = new Dictionary<string, object>(2);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                profile.ParametersComm =
                    (ModuleCommutationType)Enum.Parse(typeof(ModuleCommutationType), result.Value.ToString());
            }
        }

        private void FillClampConditions(Profile profile, long testTypeId)
        {
            var results = new Dictionary<string, object>(1);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "CLAMP_HeightMeasure":
                        profile.IsHeightMeasureEnabled = Boolean.Parse(result.Value.ToString());
                        break;
                    case "CLAMP_HeightValue":
                        profile.Height = ushort.Parse(result.Value.ToString());
                        break;
                    case "CLAMP_Force":
                        profile.ParametersClamp = long.Parse(result.Value.ToString());
                        break;
                    case "CLAMP_Temperature":
                        profile.Temperature = ushort.Parse(result.Value.ToString());
                        break;
                }
            }
        }

        private TestParameters FillGateConditions(long testTypeId)
        {
            var results = new Dictionary<string, object>(3);
            var testParams = new TestParameters() { IsEnabled = true, TestTypeId = testTypeId };

            FillOrder(testTypeId, testParams);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "Gate_IHEn":
                        testParams.IsIhEnabled = Boolean.Parse(result.Value.ToString());
                        break;
                    case "Gate_ILEn":
                        testParams.IsIlEnabled = Boolean.Parse(result.Value.ToString());
                        break;
                    case "Gate_EnableCurrent":
                        testParams.IsCurrentEnabled = Boolean.Parse(result.Value.ToString());
                        break;
                    case "Gate_EnableIHStrike":
                        testParams.IsIhStrikeCurrentEnabled = Boolean.Parse(result.Value.ToString());
                        break;
                }
            }

            return testParams;
        }

        private void FillOrder(long testTypeId, BaseTestParametersAndNormatives testParams)
        {
            _orderSelect.Parameters["@TEST_TYPE_ID"].Value = testTypeId;

            a++;
            var order = _orderSelect.ExecuteScalar().ToString();
            testParams.Order = int.Parse(order);
        }

        private void FillGateParameters(TestParameters parameters, long testTypeId)
        {
            var results = new List<Tuple<string, float?, float?>>();
            FillParametersResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Item1)
                {
                    case "RG":
                        if (result.Item3.HasValue)
                            parameters.Resistance = result.Item3.Value;
                        break;
                    case "IGT":
                        if (result.Item3.HasValue)
                            parameters.IGT = result.Item3.Value;
                        break;
                    case "VGT":
                        if (result.Item3.HasValue)
                            parameters.VGT = result.Item3.Value;
                        break;
                    case "IH":
                        if (parameters.IsIhEnabled && result.Item3.HasValue)
                            parameters.IH = result.Item3.Value;
                        break;
                    case "IL":
                        if (parameters.IsIlEnabled && result.Item3.HasValue)
                            parameters.IL = result.Item3.Value;
                        break;
                }
            }
        }

        private Types.BVT.TestParameters FillBvtConditions(long testTypeId)
        {
            var results = new Dictionary<string, object>(9);
            var testParams = new Types.BVT.TestParameters() { IsEnabled = true, TestTypeId = testTypeId };

            FillOrder(testTypeId, testParams);

            FillConditionsResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "BVT_Type":
                        testParams.TestType = (BVTTestType)(Enum.Parse(typeof(BVTTestType), result.Value.ToString()));
                        break;

                    case "BVT_I":
                        testParams.CurrentLimit = float.Parse(result.Value.ToString());
                        break;

                    case "BVT_RumpUp":
                        testParams.RampUpVoltage = float.Parse(result.Value.ToString());
                        break;

                    case "BVT_StartV":
                        testParams.StartVoltage = UInt16.Parse(result.Value.ToString());
                        break;

                    case "BVT_F":
                        testParams.VoltageFrequency = UInt16.Parse(result.Value.ToString());
                        break;

                    case "BVT_FD":
                        testParams.FrequencyDivisor = UInt16.Parse(result.Value.ToString());
                        break;

                    case "BVT_Mode":
                        testParams.MeasurementMode =
                            (BVTMeasurementMode)(Enum.Parse(typeof(BVTMeasurementMode), result.Value.ToString()));
                        break;

                    case "BVT_UseUdsmUrsm":
                        testParams.UseUdsmUrsm = bool.Parse(result.Value.ToString());
                        break;

                    case "BVT_VR":
                        switch (testParams.TestType)
                        {
                            case BVTTestType.Both:
                            case BVTTestType.Reverse:
                                testParams.VoltageLimitR = UInt16.Parse(result.Value.ToString());
                                break;
                        }
                        break;

                    case "BVT_VD":
                        switch (testParams.TestType)
                        {
                            case BVTTestType.Both:
                            case BVTTestType.Direct:
                                testParams.VoltageLimitD = UInt16.Parse(result.Value.ToString());
                                break;
                        }
                        break;

                    case "BVT_PlateTime":
                        testParams.PlateTime = UInt16.Parse(result.Value.ToString());
                        break;
                }
            }

            return testParams;
        }

        private void FillBvtParameters(Types.BVT.TestParameters parameters, long testTypeId)
        {
            var results = new List<Tuple<string, float?, float?>>();
            FillParametersResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Item1)
                {
                    case "VRRM":
                        if (result.Item2.HasValue)
                            parameters.VRRM = Convert.ToUInt16(result.Item2.Value);
                        break;
                    case "VDRM":
                        if (result.Item2.HasValue)
                            parameters.VDRM = Convert.ToUInt16(result.Item2.Value);
                        break;
                    case "IRRM":
                        if (result.Item3.HasValue)
                            parameters.IRRM = Convert.ToUInt16(result.Item3.Value);
                        break;
                    case "IDRM":
                        if (result.Item3.HasValue)
                            parameters.IDRM = Convert.ToUInt16(result.Item3.Value);
                        break;
                }
            }
        }

        private Types.SL.TestParameters FillSlConditions(long testTypeId)
        {
            var results = new Dictionary<string, object>(9);
            var testParams = new Types.SL.TestParameters() { IsEnabled = true, TestTypeId = testTypeId };

            FillOrder(testTypeId, testParams);

            FillConditionsResults(testTypeId, results);

            #region switchSL

            foreach (var result in results)
            {
                switch (result.Key)
                {
                    case "SL_Type":
                        testParams.TestType = (VTMTestType)(Enum.Parse(typeof(VTMTestType), result.Value.ToString()));
                        break;
                    case "SL_FS":
                        testParams.UseFullScale = Boolean.Parse(result.Value.ToString());
                        break;
                    case "SL_N":
                        testParams.Count = UInt16.Parse(result.Value.ToString());
                        break;
                    case "SL_ITM":
                        switch (testParams.TestType)
                        {
                            case VTMTestType.Ramp:
                                testParams.RampCurrent = UInt16.Parse(result.Value.ToString());
                                break;
                            case VTMTestType.Sinus:
                                testParams.SinusCurrent = UInt16.Parse(result.Value.ToString());
                                break;
                            case VTMTestType.Curve:
                                testParams.CurveCurrent = UInt16.Parse(result.Value.ToString());
                                break;
                        }
                        break;
                    case "SL_Time":
                        switch (testParams.TestType)
                        {
                            case VTMTestType.Ramp:
                                testParams.RampTime = UInt16.Parse(result.Value.ToString());
                                break;
                            case VTMTestType.Sinus:
                                testParams.SinusTime = UInt16.Parse(result.Value.ToString());
                                break;
                            case VTMTestType.Curve:
                                testParams.CurveTime = UInt16.Parse(result.Value.ToString());
                                break;
                        }
                        break;
                    case "SL_OpenEn":
                        testParams.IsRampOpeningEnabled = Boolean.Parse(result.Value.ToString());
                        break;
                    case "SL_OpenI":
                        testParams.RampOpeningCurrent = UInt16.Parse(result.Value.ToString());
                        break;
                    case "SL_TimeEx":
                        switch (testParams.TestType)
                        {
                            case VTMTestType.Ramp:
                                testParams.RampOpeningTime = UInt16.Parse(result.Value.ToString());
                                break;
                            case VTMTestType.Curve:
                                testParams.CurveAddTime = UInt16.Parse(result.Value.ToString());
                                break;
                        }
                        break;
                    case "SL_Factor":
                        testParams.CurveFactor = UInt16.Parse(result.Value.ToString());
                        break;
                }
            }
            #endregion

            return testParams;
        }

        private void FillSlParameters(Types.SL.TestParameters parameters, long testTypeId)
        {
            var results = new List<Tuple<string, float?, float?>>();
            FillParametersResults(testTypeId, results);

            foreach (var result in results)
            {
                switch (result.Item1)
                {
                    case "VTM":
                        if (result.Item3.HasValue)
                            parameters.VTM = result.Item3.Value;
                        break;
                }
            }
        }

        private void FillConditionsResults(long testTypeId, IDictionary<string, object> results)
        {
            _condSelect.Parameters["@TEST_TYPE_ID"].Value = testTypeId;

            a++;
            using (var reader = _condSelect.ExecuteReader())
            {
                while (reader.Read())
                    results.Add(((string)reader[0]).Trim(), reader[1]);
            }
        }

        private void FillParametersResults(long testTypeId, ICollection<Tuple<string, float?, float?>> results)
        {
            _paramSelect.Parameters["@TEST_TYPE_ID"].Value = testTypeId;

            a++;
            using (var reader = _paramSelect.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader[0].ToString().Trim();
                    float minVal;
                    var minValParsed = float.TryParse(reader[1].ToString(), out minVal);
                    float maxVal;
                    var maxValParsed = float.TryParse(reader[2].ToString(), out maxVal);

                    results.Add(new Tuple<string, float?, float?>(name, minValParsed ? minVal : (float?)null, maxValParsed ? maxVal : (float?)null));
                }
            }
        }

        public ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found)
        {
            //читаем один единственный профиль с принятым profName и MmmeCode
            try
            {
                ProfileItem Result = null;

                Tuple<int, string, Guid, DateTime> profileDict = null;

                _profileSingleSelect.Parameters["@ProfName"].Value = profName;
                _profileSingleSelect.Parameters["@MmmeCode"].Value = mmmeCode;

                using (var reader = _profileSingleSelect.ExecuteReader())
                {
                    while (reader.Read())
                        profileDict = new Tuple<int, string, Guid, DateTime>((int)reader[0], (string)reader[1], (Guid)reader[2], (DateTime)reader[3]);
                }

                if (profileDict == null)
                {
                    Found = false;
                    return Result;
                }
                else
                {
                    Found = true;

                    var testTypes = new Dictionary<long, long>(5);

                    _condCmd.Parameters["@PROF_ID"].Value = profileDict.Item1;

                    using (var reader = _condCmd.ExecuteReader())
                    {
                        while (reader.Read())
                            testTypes.Add((int)reader[0], (int)reader[1]);
                    }

                    Profile profile = new Profile(profileDict.Item2, profileDict.Item3, profileDict.Item4);

                    foreach (var testType in testTypes)
                        FillParameters(profile, testType.Key, testType.Value);

                    Result = new ProfileItem
                    {
                        ProfileId = profileDict.Item1,
                        ProfileName = profileDict.Item2,
                        ProfileKey = profileDict.Item3,
                        ProfileTS = profileDict.Item4,
                        GateTestParameters = new List<TestParameters>(),
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
                        var gate = baseTestParametersAndNormativese as TestParameters;
                        if (gate != null)
                            Result.GateTestParameters.Add(gate);

                        var sl = baseTestParametersAndNormativese as Types.SL.TestParameters;
                        if (sl != null)
                            Result.VTMTestParameters.Add(sl);

                        var bvt = baseTestParametersAndNormativese as Types.BVT.TestParameters;
                        if (bvt != null)
                            Result.BVTTestParameters.Add(bvt);

                        var dvdt = baseTestParametersAndNormativese as Types.dVdt.TestParameters;
                        if (dvdt != null)
                            Result.DvDTestParameterses.Add(dvdt);

                        var atu = baseTestParametersAndNormativese as Types.ATU.TestParameters;
                        if (atu != null)
                            Result.ATUTestParameters.Add(atu);

                        var qrrTq = baseTestParametersAndNormativese as Types.QrrTq.TestParameters;
                        if (qrrTq != null)
                            Result.QrrTqTestParameters.Add(qrrTq);

                        var rac = baseTestParametersAndNormativese as Types.RAC.TestParameters;
                        if (rac != null)
                            Result.RACTestParameters.Add(rac);
                    }
                }

                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
