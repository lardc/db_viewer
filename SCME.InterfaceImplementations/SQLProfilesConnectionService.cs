using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using SCME.Types.DatabaseServer;
using SCME.Types.DataContracts;

namespace SCME.InterfaceImplementations
{
    public class SQLProfilesConnectionService : IProfilesConnectionService
    {
        private readonly IProfilesService _profilesService;
        private readonly SqlConnection _connection;
        private readonly object _locker = new object();

        private SqlCommand _codesSelectCommand;
        private SqlCommand _mmeConnectedProfsDelete;
        private SqlCommand _mmeDeleteCommand;
        private SqlCommand _mmeSelectCommand;
        private SqlCommand _mmeInsertCommand;
        private SqlCommand _codesInsertCommand;
        private SqlCommand _codesDeleteCommand;

        public SQLProfilesConnectionService(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();

            _profilesService = new SQLProfilesService(connectionString);

            PrepareQueries();
        }

        public SQLProfilesConnectionService(string connectionString, IProfilesService profileService)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();

            _profilesService = profileService;

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

            _mmeDeleteCommand = new SqlCommand("DELETE FROM [dbo].[MME_CODES] WHERE [MME_CODE_ID] = @MME_CODE_ID",
                _connection);
            _mmeDeleteCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _mmeDeleteCommand.Prepare();

            _mmeSelectCommand = new SqlCommand("SELECT MME_CODE_ID FROM [dbo].[MME_CODES] WHERE [MME_CODE] = @MME_CODE",
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

            _codesDeleteCommand = new SqlCommand(
                "DELETE FROM [dbo].[MME_CODES_TO_PROFILES] WHERE [MME_CODE_ID] = @MME_CODE_ID AND [PROFILE_ID] = @PROFILE_ID",
                _connection);
            _codesDeleteCommand.Parameters.Add("@MME_CODE_ID", SqlDbType.Int);
            _codesDeleteCommand.Parameters.Add("@PROFILE_ID", SqlDbType.Int);
            _codesDeleteCommand.Prepare();
        }

        public IEnumerable<MmeCode> GetMmeCodes()
        {
            lock (_locker)
            {
                var list = new List<MmeCode>(10);
                if (_connection == null || _connection.State != ConnectionState.Open)
                    return list;

                var codesSelectCommand = new SqlCommand("SELECT [MME_CODE_ID], [MME_CODE] FROM [dbo].[MME_CODES]", _connection);
                using (var reader = codesSelectCommand.ExecuteReader())
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
                    mmeCode.ProfileMmes = GetMmeProfiles(mmeCode.Id).ToList();

                return list;
            }
        }

        public IEnumerable<ProfileMme> GetMmeProfiles(long mmeCodeId)
        {
            lock (_locker)
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
        }

        public void SaveConnections(List<MmeCode> mmeCodes)
        {
            var dbMmeCodes = GetMmeCodes();
            var mmeCodesToDelete = dbMmeCodes.Except(mmeCodes).ToList();

            foreach (var mmeCode in mmeCodesToDelete)
            {
                DeletMme(mmeCode.Id);
            }

            foreach (var mmeCode in mmeCodes)
            {
                SaveConnections(mmeCode.Name,mmeCode.ProfileMmes.Where(p=>p.IsSelected).Select(p=>p.Id));
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

        public void DeletMme(long id)
        {
            _mmeConnectedProfsDelete.Parameters["@MME_CODE_ID"].Value = id;
            _mmeConnectedProfsDelete.ExecuteNonQuery();

            _mmeDeleteCommand.Parameters["@MME_CODE_ID"].Value = id;
            _mmeDeleteCommand.ExecuteNonQuery();
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

        public void Dispose()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }
    }
}
