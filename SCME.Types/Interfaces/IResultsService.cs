using System;
using System.Collections.Generic;
using SCME.Types.DataContracts;

namespace SCME.Types.Interfaces
{
    public interface IResultsService : IDisposable
    {
        /// <summary>
        /// Saving results
        /// </summary>
        /// <param name="result"></param>
        /// <param name="errors"></param>
        void WriteResults(ResultItem result, IEnumerable<string> errors);

        /// <summary>
        /// Return groups
        /// </summary>
        /// <param name="from">Date from</param>
        /// <param name="to">Date to</param>
        /// <returns></returns>
        List<string> GetGroups(DateTime? @from, DateTime? to);

        /// <summary>
        /// Return group devices
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        List<DeviceItem> GetDevices(string @group);
        
        List<int> ReadDeviceErrors(long internalId);

        List<ParameterItem> ReadDeviceParameters(long internalId);

        List<ConditionItem> ReadDeviceConditions(long internalId);

        List<ParameterNormativeItem> ReadDeviceNormatives(long internalId);

        int? ReadDeviceRTClass(string devCode, string profName);

        int? ReadDeviceClass(string devCode, string profName);

        List<DeviceLocalItem> GetUnsendedDevices();

        /// <summary>
        /// Sets IsSendedToServer=1 to device
        /// </summary>
        /// <param name="deviceId"></param>
        void SetResultSended(long deviceId);

        /// <summary>
        /// Save results from localDevice
        /// </summary>
        /// <param name="localDevice"></param>
        /// <returns></returns>
        bool SaveResults(DeviceLocalItem localDevice);
    }
}
