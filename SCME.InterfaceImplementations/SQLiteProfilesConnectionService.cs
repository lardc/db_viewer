using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using SCME.Types.DatabaseServer;
using SCME.Types.DataContracts;

namespace SCME.InterfaceImplementations
{
    public class SQLiteProfilesConnectionService : IProfilesConnectionService
    {
        private readonly IProfilesService _profilesService;
        private readonly SQLiteConnection _connection;
        private readonly object _locker = new object();

        public SQLiteProfilesConnectionService(string databasePath)
        {
            _connection = new SQLiteConnection(databasePath, false);
            _connection.Open();
            _profilesService = new SQLiteProfilesService(databasePath);
        }

        public IEnumerable<MmeCode> GetMmeCodes()
        {
            lock (_locker)
            {
                var list = new List<MmeCode>(10);
                if (_connection == null || _connection.State != ConnectionState.Open)
                    return list;
                var codesSelectCommand = new SQLiteCommand("SELECT MME_CODE_ID,MME_CODE  FROM MME_CODES", _connection);
                using (var reader = codesSelectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MmeCode
                        {
                            Id = (long)reader[0],
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

        }

        public IEnumerable<ProfileMme> GetMmeProfiles(long mmeCodeId)
        {
            lock (_locker)
            {
                var list = new List<ProfileMme>();
                if (_connection == null || _connection.State != ConnectionState.Open)
                    return list;

                list = _profilesService.GetProfileItems().Select(p => new ProfileMme { Id = p.ProfileId, Name = p.ProfileName }).ToList();

                var codesSelectCommand = new SQLiteCommand("SELECT PROFILE_ID FROM MME_CODES_TO_PROFILES where MME_CODE_ID=@MME_CODE_ID", _connection);
                codesSelectCommand.Parameters.Add("@MME_CODE_ID", DbType.Int64);
                codesSelectCommand.Prepare();
                codesSelectCommand.Parameters["@MME_CODE_ID"].Value = mmeCodeId;
                using (var reader = codesSelectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = (long)reader[0];
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
            var mmeConnectedProfsDelete = new SQLiteCommand("DELETE FROM MME_CODES_TO_PROFILES where MME_CODE_ID=@MME_CODE_ID", _connection);
            mmeConnectedProfsDelete.Parameters.Add("@MME_CODE_ID", DbType.Int64);
            mmeConnectedProfsDelete.Prepare();
            mmeConnectedProfsDelete.Parameters["@MME_CODE_ID"].Value = id;
            mmeConnectedProfsDelete.ExecuteNonQuery();

            var mmeDeleteCommand = new SQLiteCommand("SELECT MME_CODE_ID FROM MME_CODES WHERE MME_CODE_ID = @MME_CODE_ID", _connection);
            mmeDeleteCommand.Parameters.Add("@MME_CODE_ID", DbType.Int64);
            mmeDeleteCommand.Prepare();
            mmeDeleteCommand.Parameters["@MME_CODE_ID"].Value = id;
            mmeDeleteCommand.ExecuteNonQuery();
        }

        private long GetMmeCodeId(string mmeCode)
        {
            var mmeSelectCommand = new SQLiteCommand("SELECT MME_CODE_ID FROM MME_CODES WHERE MME_CODE = @MME_CODE", _connection);
            mmeSelectCommand.Parameters.Add("@MME_CODE", DbType.String);
            mmeSelectCommand.Prepare();
            mmeSelectCommand.Parameters["@MME_CODE"].Value = mmeCode;
            var posibleMmeCode = mmeSelectCommand.ExecuteScalar();

            if (posibleMmeCode != null) return (long)posibleMmeCode;

            var mmeInsertCommand = new SQLiteCommand("INSERT INTO MME_CODES (MME_CODE) VALUES (@MME_CODE)", _connection);
            mmeInsertCommand.Parameters.Add("@MME_CODE", DbType.String);
            mmeInsertCommand.Prepare();
            mmeInsertCommand.Parameters["@MME_CODE"].Value = mmeCode;
            mmeInsertCommand.ExecuteNonQuery();
            return _connection.LastInsertRowId;
        }

        private void InsertNewProfiles(long mmeCode, IEnumerable<long> profileIds)
        {
            var codesInsertCommand =
                new SQLiteCommand(
                    "INSERT INTO MME_CODES_TO_PROFILES (MME_CODE_ID,PROFILE_ID) VALUES (@MME_CODE_ID,@PROFILE_ID)", _connection);
            codesInsertCommand.Parameters.Add("@MME_CODE_ID", DbType.Int64);
            codesInsertCommand.Parameters.Add("@PROFILE_ID", DbType.Int64);
            codesInsertCommand.Prepare();
            codesInsertCommand.Parameters["@MME_CODE_ID"].Value = mmeCode;
            foreach (var profileId in profileIds)
            {
                codesInsertCommand.Parameters["@PROFILE_ID"].Value = profileId;
                codesInsertCommand.ExecuteScalar();
            }
        }

        private void DeleteProfiles(long mmeCode, IEnumerable<long> profileIds)
        {
            var codesDeleteCommand =
                new SQLiteCommand(
                    "DELETE FROM MME_CODES_TO_PROFILES WHERE MME_CODE_ID=@MME_CODE_ID AND PROFILE_ID=@PROFILE_ID", _connection);
            codesDeleteCommand.Parameters.Add("@MME_CODE_ID", DbType.Int64);
            codesDeleteCommand.Parameters.Add("@PROFILE_ID", DbType.Int64);
            codesDeleteCommand.Prepare();
            codesDeleteCommand.Parameters["@MME_CODE_ID"].Value = mmeCode;
            foreach (var profileId in profileIds)
            {
                codesDeleteCommand.Parameters["@PROFILE_ID"].Value = profileId;
                codesDeleteCommand.ExecuteScalar();
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
