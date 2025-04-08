using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.Interfaces;
using System;

namespace SCME.InterfaceImplementations
{
    public class SQLProfilesService : IProfilesService
    {
        private readonly SqlConnection _connection;
        private readonly ISaveProfileService _saveProfileService;
        private readonly ILoadProfilesService _loadProfilesService;

        public SQLProfilesService(string connectionString)
        {
            _connection = new SqlConnection(connectionString);

            _connection.Open();

            _saveProfileService = new SQLSaveProfileService(_connection);
            _loadProfilesService = new SQLLoadProfilesService(_connection);
        }

        public SQLProfilesService(ISaveProfileService saveProfileService, ILoadProfilesService loadProfilesService)
        {
            _saveProfileService = saveProfileService;
            _loadProfilesService = loadProfilesService;
        }

        public void SaveProfiles(List<ProfileItem> profileItems)
        {
            var dbProfiles = GetProfileItems();

            var profilesToDelete = GetProfilesToDelete(profileItems, dbProfiles);
            _saveProfileService.DeleteProfiles(profilesToDelete);

            var profilesToSave = GetProfilesToChange(profileItems, dbProfiles);
            foreach (var profileItem in profilesToSave)
            {
                _saveProfileService.SaveProfileItem(profileItem);
            }
        }

        public void SaveProfilesFromMme(List<ProfileItem> profileItems, string mmeCode)
        {
            var dbProfiles = GetProfileItemsByMme(mmeCode);

            var profilesToDelete = GetProfilesToDelete(profileItems, dbProfiles);
            _saveProfileService.DeleteProfiles(profilesToDelete, mmeCode);

            dbProfiles = GetProfileItemsByMme(mmeCode);

            var profilesToSave = GetProfilesToChange(profileItems, dbProfiles);
            foreach (var profileItem in profilesToSave)
            {
                _saveProfileService.SaveProfileItem(profileItem, mmeCode);
            }
        }

        private static IEnumerable<ProfileItem> GetProfilesToChange(IEnumerable<ProfileItem> profilesToSave, List<ProfileItem> dbProfileItems)
        {
            var profilesToChange = new List<ProfileItem>();
            foreach (var profileItem in profilesToSave)
            {
                var oldProfile = dbProfileItems.Find(db => db.Equals(profileItem));
                if (oldProfile == null)
                    profilesToChange.Add(profileItem);
                else
                {
                    if (profileItem.HasChanges(oldProfile))
                        profilesToChange.Add(profileItem);
                }
            }

            return profilesToChange;
        }

        private static List<ProfileItem> GetProfilesToDelete(IEnumerable<ProfileItem> profilesToSave, IEnumerable<ProfileItem> dbProfileItems)
        {
            var profilesToDelete = dbProfileItems.Except(profilesToSave);
            return profilesToDelete.ToList();
        }

        public List<ProfileItem> GetProfileItems()
        {
            return _loadProfilesService.GetProfileItems();
        }

        public List<ProfileItem> GetProfileItemsByMme(string mmeCode)
        {
            return _loadProfilesService.GetProfileItems(mmeCode);
        }

        public ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found)
        {
            return _loadProfilesService.GetProfileByProfName(profName, mmmeCode, ref Found);
        }

        public void Dispose()
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }
    }
}
