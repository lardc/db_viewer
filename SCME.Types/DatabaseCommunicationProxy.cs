using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace SCME.Types
{
    public class DatabaseCommunicationProxy : ClientBase<IDatabaseCommunicationService>, IDatabaseCommunicationService
    {
        public DatabaseCommunicationProxy(string ServerEndpointConfigurationName)
            : base(ServerEndpointConfigurationName)
        {
        }

        public DatabaseCommunicationProxy(string ServerEndpointConfigurationName, string RemoteAddress)
            : base(ServerEndpointConfigurationName, RemoteAddress)
        {
        }

        public void Check()
        {
            Channel.Check();
        }

        public List<LogItem> ReadLogs(long Tail, long Count)
        {
            return Channel.ReadLogs(Tail, Count);
        }

        public List<string> ReadGroups(DateTime? From, DateTime? To)
        {
            return Channel.ReadGroups(From, To);
        }

        public List<string> ReadProfiles()
        {
            return Channel.ReadProfiles();
        }

        public List<DeviceItem> ReadDevices(string Group)
        {
            return Channel.ReadDevices(Group);
        }

        public List<ParameterItem> ReadDeviceParameters(long InternalID)
        {
            return Channel.ReadDeviceParameters(InternalID);
        }

        public List<ConditionItem> ReadDeviceConditions(long InternalID)
        {
            return Channel.ReadDeviceConditions(InternalID);
        }

        public List<ParameterNormativeItem> ReadDeviceNormatives(long InternalID)
        {
            return Channel.ReadDeviceNormatives(InternalID);
        }

        public int? ReadDeviceRTClass(string devCode, string profName)
        {
            return Channel.ReadDeviceRTClass(devCode, profName);
        }

        public List<int> ReadDeviceErrors(long InternalID)
        {
            return Channel.ReadDeviceErrors(InternalID);
        }

        public List<ProfileItem> GetProfileItems()
        {
            return Channel.GetProfileItems();
        }

        public List<ProfileItem> GetProfileItemsByMmeCode(string mmeCode)
        {
            return Channel.GetProfileItemsByMmeCode(mmeCode);
        }

        public ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found)
        {
            return Channel.GetProfileByProfName(profName, mmmeCode, ref Found);
        }

    }
}