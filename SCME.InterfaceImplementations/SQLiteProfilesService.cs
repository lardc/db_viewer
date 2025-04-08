using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using SCME.Types;
using SCME.Types.DatabaseServer;
using SCME.Types.Interfaces;
using System;

namespace SCME.InterfaceImplementations
{
    public class SQLiteProfilesService : IProfilesService
    {
        private readonly SQLiteConnection _connection;
        private readonly ISaveProfileService _saveProfileService;
        private readonly ILoadProfilesService _loadProfilesService;

        public SQLiteProfilesService(string databasePath)
        {
            _connection = new SQLiteConnection(databasePath, false);
            _connection.Open();

            _saveProfileService = new SQLiteSaveProfileService(_connection);
            _loadProfilesService = new SQLiteLoadProfilesService(_connection);

        }

        public SQLiteProfilesService(ISaveProfileService saveProfileService, ILoadProfilesService loadProfilesService)
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
            List<ProfileItem> dbProfiles = null;

            //флаг: в локальной базе данных КИП обнаружены профили с такими GUID, которых нет в центральной базе данных. синхронизация таких профилей не имеет смысла и результаты измерений, хранящиеся в локальной базе данных по таким профилям не имеет смысла передавать в центральную базу данных
            bool garbageProfilesExist = true;

            while (garbageProfilesExist)
            {
                dbProfiles = GetProfileItemsByMme(mmeCode);

                var profilesToDelete = GetProfilesToDelete(profileItems, dbProfiles);
                garbageProfilesExist = (profilesToDelete.Count != 0);

                if (garbageProfilesExist)
                    _saveProfileService.DeleteProfiles(profilesToDelete, mmeCode);
            }

            var profilesToSave = GetProfilesToChange(profileItems, dbProfiles);

            foreach (var profileItem in profilesToSave)
                _saveProfileService.SaveProfileItem(profileItem, mmeCode);
        }

        private static IEnumerable<ProfileItem> GetProfilesToChange(IEnumerable<ProfileItem> profilesToSave, List<ProfileItem> dbProfileItems)
        {
            var profilesToChange = new List<ProfileItem>();
            foreach (var profileItem in profilesToSave)
            {
                var oldProfile = dbProfileItems.Find(db => db.Equals(profileItem));
                if (oldProfile == null)
                {
                    profileItem.ExistsBeforeSync = false;
                    profilesToChange.Add(profileItem);
                }
                else if (profileItem.HasChanges(oldProfile))
                {
                    profileItem.ExistsBeforeSync = true;
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
