using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using SCME.Types;

namespace SCME.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
        Namespace = @"http://proton-electrotex.com/SCME",
        ConcurrencyMode = ConcurrencyMode.Single)]
    public class DatabaseServer : IDatabaseCommunicationService
    {
        internal DatabaseServer()
        {
        }

        #region Interface implementation

        void IDatabaseCommunicationService.Check()
        {
        }

        List<LogItem> IDatabaseCommunicationService.ReadLogs(long Tail, long Count)
        {
            try
            {
                return SystemHost.Journal.ReadFromEnd(Tail, Count);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        List<string> IDatabaseCommunicationService.ReadGroups(DateTime? From, DateTime? To)
        {
            try
            {
                return SystemHost.Results.ReadGroups(From, To);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        List<string> IDatabaseCommunicationService.ReadProfiles()
        {
            try
            {
                return SystemHost.Results.ReadProfiles();
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        List<DeviceItem> IDatabaseCommunicationService.ReadDevices(string Group)
        {
            try
            {
                return SystemHost.Results.ReadDevices(Group);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        List<ParameterItem> IDatabaseCommunicationService.ReadDeviceParameters(long InternalID)
        {
            try
            {
                return SystemHost.Results.ReadDeviceParameters(InternalID);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        List<ConditionItem> IDatabaseCommunicationService.ReadDeviceConditions(long InternalID)
        {
            try
            {
                return SystemHost.Results.ReadDeviceConditions(InternalID);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        List<ParameterNormativeItem> IDatabaseCommunicationService.ReadDeviceNormatives(long InternalID)
        {
            try
            {
                return SystemHost.Results.ReadDeviceNormatives(InternalID);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        int? IDatabaseCommunicationService.ReadDeviceRTClass(string devCode, string profName)
        {
            try
            {
                return SystemHost.Results.ReadDeviceRTClass(devCode, profName);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        List<int> IDatabaseCommunicationService.ReadDeviceErrors(long InternalID)
        {
            try
            {
                return SystemHost.Results.ReadDeviceErrors(InternalID);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        public List<ProfileItem> GetProfileItems()
        {
            try
            {
                return SystemHost.Results.GetProfiles();
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        public List<ProfileItem> GetProfileItemsByMmeCode(string mmeCode)
        {
            if (Properties.Settings.Default.DisableResultDB)
                return null;

            try
            {
                return SystemHost.Results.GetProfiles(mmeCode);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        public ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found)
        {
            if (Properties.Settings.Default.DisableResultDB)
                return null;

            try
            {
                return SystemHost.Results.GetProfileByProfName(profName, mmmeCode, ref Found);
            }
            catch (Exception ex)
            {
                throw new FaultException<FaultData>(
                    new FaultData { Device = ComplexParts.Database, Message = ex.Message, TimeStamp = DateTime.Now },
                    String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

        }

        #endregion
    }
}