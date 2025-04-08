using System;
using System.Collections.Generic;

namespace SCME.Types.DatabaseServer
{
    public interface IProfilesService: IDisposable
    {
        
        void SaveProfiles(List<ProfileItem> profileItems);

        
        List<ProfileItem> GetProfileItems();

        
        List<ProfileItem> GetProfileItemsByMme(string mmeCode);

        
        void SaveProfilesFromMme(List<ProfileItem> profileItems, string mmeCode);


        ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found);
    }
}
