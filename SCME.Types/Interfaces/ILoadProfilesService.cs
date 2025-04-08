using System;
using System.Collections.Generic;

namespace SCME.Types.Interfaces
{
    public interface ILoadProfilesService
    {
        List<ProfileItem> GetProfileItems();
        List<ProfileItem> GetProfileItems(string mmeCode);
        ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found);
    }
}
