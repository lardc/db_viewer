using System.Collections.Generic;
using System.ServiceModel;
using SCME.Types;
using SCME.Types.Gate;
using SCME.Types.Profiles;
using SCME.UI.CustomControl;
using SCME.UI.PagesTech;
using SCME.UI.PagesUser;

namespace SCME.UI.IO
{
    internal class ProfilesDbLogic
    {
        public static void ImportProfilesFromDb()
        {
            var seviceConnected = false;
            List<ProfileItem> profileItems;
            try
            {
                profileItems = Cache.Net.GetProfilesFromServerDb(Cache.Main.MmeCode);
                Cache.Main.State = "online";
                seviceConnected = true;
            }
            catch (CommunicationObjectFaultedException)
            {
                profileItems = Cache.Net.GetProfilesFromLocalDb(Cache.Main.MmeCode, ref seviceConnected);
                Cache.Main.State = "offline";
            }

            if (profileItems == null || profileItems.Count <= 0 && !seviceConnected)
                return;
            if (profileItems.Count <= 0)
            {
                var dialog = new DialogWindow("Сообщение", "Нет активных профилей");
                dialog.ButtonConfig(DialogWindow.EbConfig.OK);
                dialog.ShowDialog();
            }

            var profiles = new List<Profile>();
            foreach (var profileItem in profileItems)
            {

                var profile = new Profile(profileItem.ProfileName, profileItem.ProfileKey, profileItem.ProfileTS)
                {
                    IsHeightMeasureEnabled = profileItem.IsHeightMeasureEnabled,
                    ParametersClamp = profileItem.ParametersClamp,
                    Height = profileItem.Height,
                    Temperature = profileItem.Temperature
                };

                foreach (var g in profileItem.GateTestParameters) profile.TestParametersAndNormatives.Add(g);
                foreach (var b in profileItem.BVTTestParameters) profile.TestParametersAndNormatives.Add(b);
                foreach (var v in profileItem.VTMTestParameters) profile.TestParametersAndNormatives.Add(v);
                foreach (var d in profileItem.DvDTestParameterses) profile.TestParametersAndNormatives.Add(d);


                profiles.Add(profile);
            }
            var dictionary = new ProfileDictionary(profiles);
            Cache.Main.IsProfilesParsed = true;

            Cache.ProfileEdit = new ProfilePage(dictionary);
            Cache.ProfileSelection = new ProfileSelectionPage(dictionary);
            Cache.ProfileSelection.SetNextButtonVisibility(Cache.Main.Param);
        }

        public static void SaveProfilesListToDb(IList<Profile> profiles)
        {
            var profileItems = new List<ProfileItem>(profiles.Count);
            foreach (var profile in profiles)
            {
                var profileItem = new ProfileItem
                {
                    ProfileName = profile.Name,
                    ProfileKey = profile.Key,
                    ProfileTS = profile.Timestamp,
                    GateTestParameters = new List<TestParameters>(),
                    VTMTestParameters = new List<Types.SL.TestParameters>(),
                    BVTTestParameters = new List<Types.BVT.TestParameters>(),
                    DvDTestParameterses = new List<Types.dVdt.TestParameters>(),
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
                    if(dvdt != null)
                        profileItem.DvDTestParameterses.Add(dvdt);

                }
                profileItems.Add(profileItem);
            }

            if(Cache.Main.State == "offline")
                Cache.Net.SaveProfilesToLocal(profileItems);
            else
                Cache.Net.SaveProfilesToServer(profileItems);
        }


        public static void SaveProfilesToDb(IList<Profile> profiles)
        {
            SaveProfilesListToDb(profiles);
        }

    }


}
