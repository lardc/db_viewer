using System.Collections.Generic;

namespace SCME.Types.Interfaces
{
    public interface ISaveProfileService
    {
        long SaveProfileItem(ProfileItem profileItem);

        void DeleteProfiles(List<ProfileItem> profilesToDelete);

        void DeleteProfiles(List<ProfileItem> profilesToDelete, string mmeCode);

        void SaveProfileItem(ProfileItem profileItem, string mmeCode);
    }
}
