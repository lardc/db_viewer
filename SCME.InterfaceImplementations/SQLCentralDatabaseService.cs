using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.DataContracts;
using SCME.Types.Interfaces;

namespace SCME.InterfaceImplementations
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        Namespace = "http://proton-electrotex.com/SCME",
        ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    public class SQLCentralDatabaseService : ICentralDatabaseService, IErrorHandler
    {
        private readonly IProfilesService _profilesService;
        private readonly IResultsService _resultsService;
        private readonly SqlConnection _connection;

        private SqlCommand _codesSelectCommand;
        private SqlCommand _mmeConnectedProfsDelete;
        private SqlCommand _mmeDeleteCommand;
        private SqlCommand _mmeSelectCommand;
        private SqlCommand _mmeInsertCommand;
        private SqlCommand _codesInsertCommand;
        private SqlCommand _codesDeleteCommand;

        public SQLCentralDatabaseService(ApplicationSettingsBase Settings)
        {
            var connectionString = Convert.ToBoolean(Settings["DBIntegratedSecurity"])
                ? $"Server={Settings["DbPath"]}; Database={Settings["DBName"]}; Integrated Security=true;"
                : $"Server={Settings["DbPath"]}; Database={Settings["DBName"]}; User Id={Settings["DBUser"]}; Password={Settings["DBPassword"]};";

            _connection = new SqlConnection(connectionString);
            _connection.Open();
            _profilesService = new SQLProfilesService(connectionString);
            _resultsService = new SQLResultsServiceServer(connectionString);

            PrepareQueries();
        }

        private void PrepareQueries()
        {
            _codesSelectCommand =
                new SqlCommand("SELECT [PROFILE_ID] FROM [dbo].[MME_CODES_TO_PROFILES] WHERE [MME_CODE_ID] = @MME_CODE_ID",
                    _connection);
            _codesSelectCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _codesSelectCommand.Prepare();

            _mmeConnectedProfsDelete =
                new SqlCommand("DELETE FROM [dbo].[MME_CODES_TO_PROFILES] WHERE [MME_CODE_ID] = @MME_CODE_ID", _connection);
            _mmeConnectedProfsDelete.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _mmeConnectedProfsDelete.Prepare();

            _mmeDeleteCommand = new SqlCommand("DELETE FROM [dbo].[MME_CODES] WHERE [MME_CODE_ID] = @MME_CODE_ID", _connection);
            _mmeDeleteCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _mmeDeleteCommand.Prepare();

            _mmeSelectCommand = new SqlCommand("SELECT [MME_CODE_ID] FROM MME_CODES WHERE [MME_CODE] = @MME_CODE", _connection);
            _mmeSelectCommand.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
            _mmeSelectCommand.Prepare();

            _mmeInsertCommand =
                new SqlCommand("INSERT INTO [dbo].[MME_CODES]([MME_CODE]) OUTPUT INSERTED.MME_CODE_ID VALUES (@MME_CODE)",
                    _connection);
            _mmeInsertCommand.Parameters.Add("@MME_CODE", SqlDbType.NVarChar, 64);
            _mmeInsertCommand.Prepare();

            _codesInsertCommand =
                new SqlCommand(
                    "INSERT INTO [dbo].[MME_CODES_TO_PROFILES] ([MME_CODE_ID], [PROFILE_ID]) VALUES (@MME_CODE_ID, @PROFILE_ID)",
                    _connection);
            _codesInsertCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _codesInsertCommand.Parameters.Add("@PROFILE_ID", SqlDbType.Int);
            _codesInsertCommand.Prepare();

            _codesDeleteCommand =
                new SqlCommand(
                    "DELETE FROM [dbo].[MME_CODES_TO_PROFILES] WHERE [MME_CODE_ID] = @MME_CODE_ID AND [PROFILE_ID] = @PROFILE_ID",
                    _connection);
            _codesDeleteCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _codesDeleteCommand.Parameters.Add("@PROFILE_ID", SqlDbType.Int);
            _codesDeleteCommand.Prepare();
        }

        public SQLCentralDatabaseService(IProfilesService profilesService = null, IResultsService resultsService = null)
        {
            _profilesService = profilesService;
            _resultsService = resultsService;
        }

        public void Check()
        {
        }

        public void SaveResults(ResultItem results, List<string> errors)
        {
            _resultsService.WriteResults(results, errors);
        }

        public List<ProfileItem> GetProfileItems()
        {
            return _profilesService.GetProfileItems();
        }

        public List<ProfileItem> GetProfileItemsByMme(string mmeCode)
        {
            return _profilesService.GetProfileItemsByMme(mmeCode);
        }

        public ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found)
        {
            return _profilesService.GetProfileByProfName(profName, mmmeCode, ref Found);
        }

        public void SaveProfiles(List<ProfileItem> profileItems)
        {
            _profilesService.SaveProfiles(profileItems);
        }

        public void SaveProfilesFromMme(List<ProfileItem> profileItems, string mmeCode)
        {
            _profilesService.SaveProfilesFromMme(profileItems, mmeCode);
        }

        public List<string> GetGroups(DateTime? from, DateTime? to)
        {
            return _resultsService.GetGroups(from, to);
        }

        public List<DeviceItem> GetDevices(string group)
        {
            return _resultsService.GetDevices(group);
        }

        public List<int> ReadDeviceErrors(long internalId)
        {
            return _resultsService.ReadDeviceErrors(internalId);
        }

        public List<ParameterItem> ReadDeviceParameters(long internalId)
        {
            return _resultsService.ReadDeviceParameters(internalId);
        }

        public List<ConditionItem> ReadDeviceConditions(long internalId)
        {
            return _resultsService.ReadDeviceConditions(internalId);
        }

        public List<ParameterNormativeItem> ReadDeviceNormatives(long internalId)
        {
            return _resultsService.ReadDeviceNormatives(internalId);
        }
        
        public int? ReadDeviceRTClass(string devCode, string profName)
        {
            return _resultsService.ReadDeviceRTClass(devCode, profName);
        }

        public int? ReadDeviceClass(string devCode, string profName)
        {
            return _resultsService.ReadDeviceClass(devCode, profName);
        }

        public bool SendResultToServer(DeviceLocalItem localDevice)
        {
            return _resultsService.SaveResults(localDevice);
        }

        public void Dispose()
        {
            _profilesService?.Dispose();
            _resultsService?.Dispose();

            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {

        }

        public bool HandleError(Exception error)
        {
            return true;
        }

        public IEnumerable<MmeCode> GetMmeCodes()
        {
            var list = new List<MmeCode>(10);
            if (_connection == null || _connection.State != ConnectionState.Open)
                return list;

            var selectCommand = new SqlCommand("SELECT [MME_CODE_ID], [MME_CODE] FROM [dbo].[MME_CODES]", _connection);
            using (var reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new MmeCode
                    {
                        Id = (int)reader[0],
                        Name = reader[1].ToString()
                    });
                }
            }

            foreach (var mmeCode in list)
            {
                mmeCode.ProfileMmes = GetMmeProfiles(mmeCode.Id).ToList();
            }

            return list;
        }

        public IEnumerable<ProfileMme> GetMmeProfiles(long mmeCodeId)
        {
            var list = new List<ProfileMme>();
            if (_connection == null || _connection.State != ConnectionState.Open)
                return list;

            list = _profilesService.GetProfileItems().Select(p => new ProfileMme { Id = p.ProfileId, Name = p.ProfileName }).ToList();

            _codesSelectCommand.Parameters["@MME_CODE_ID"].Value = mmeCodeId;
            using (var reader = _codesSelectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = (int)reader[0];
                    var item = list.FirstOrDefault(p => p.Id.Equals(id));
                    if (!ReferenceEquals(item, null))
                        item.IsSelected = true;
                }
            }

            return list;
        }

        public void SaveConnections(List<MmeCode> mmeCodes)
        {
            var dbMmeCodes = GetMmeCodes().Select(c => c.Id);
            var mmeCodesToDelete = dbMmeCodes.Except(mmeCodes.Select(c => c.Id)).ToList();

            foreach (var mmeCode in mmeCodesToDelete)
            {
                DeleteMme(mmeCode);
            }

            foreach (var mmeCode in mmeCodes)
            {
                SaveConnections(mmeCode.Name, mmeCode.ProfileMmes.Where(p => p.IsSelected).Select(p => p.Id));
            }
        }

        private void SaveConnections(string mmeCode, IEnumerable<long> profileIds)
        {
            var mmeCodeId = GetMmeCodeId(mmeCode);
            var profilesIntoDb = GetMmeProfiles(mmeCodeId).Where(p => p.IsSelected).Select(p => p.Id).ToList();

            var profilesToDelete = profilesIntoDb.Except(profileIds).ToList();
            DeleteProfiles(mmeCodeId, profilesToDelete);

            profilesIntoDb = GetMmeProfiles(mmeCodeId).Where(p => p.IsSelected).Select(p => p.Id).ToList();

            var profilesToInsert = profileIds.Except(profilesIntoDb).ToList();
            InsertNewProfiles(mmeCodeId, profilesToInsert);
        }

        private void DeleteMme(long id)
        {
            var trans = _connection.BeginTransaction();

            try
            {
                _mmeConnectedProfsDelete.Parameters["@MME_CODE_ID"].Value = id;
                _mmeConnectedProfsDelete.Transaction = trans;
                _mmeConnectedProfsDelete.ExecuteNonQuery();

                _mmeDeleteCommand.Parameters["@MME_CODE_ID"].Value = id;
                _mmeDeleteCommand.Transaction = trans;
                _mmeDeleteCommand.ExecuteNonQuery();

                trans.Commit();
            }
            catch (Exception)
            {
                trans.Rollback();
                throw;
            }
        }

        private long GetMmeCodeId(string mmeCode)
        {
            _mmeSelectCommand.Parameters["@MME_CODE"].Value = mmeCode;
            var posibleMmeCode = _mmeSelectCommand.ExecuteScalar();

            if (posibleMmeCode != null)
                return (int)posibleMmeCode;

            _mmeInsertCommand.Parameters["@MME_CODE"].Value = mmeCode;
            var id = _mmeInsertCommand.ExecuteScalar();

            return (int)id;
        }

        private void InsertNewProfiles(long mmeCode, IEnumerable<long> profileIds)
        {
            _codesInsertCommand.Parameters["@MME_CODE_ID"].Value = mmeCode;

            foreach (var profileId in profileIds)
            {
                _codesInsertCommand.Parameters["@PROFILE_ID"].Value = profileId;
                _codesInsertCommand.ExecuteScalar();
            }
        }

        private void DeleteProfiles(long mmeCode, IEnumerable<long> profileIds)
        {
            _codesDeleteCommand.Parameters["@MME_CODE_ID"].Value = mmeCode;

            foreach (var profileId in profileIds)
            {
                _codesDeleteCommand.Parameters["@PROFILE_ID"].Value = profileId;
                _codesDeleteCommand.ExecuteScalar();
            }
        }
    }
}
