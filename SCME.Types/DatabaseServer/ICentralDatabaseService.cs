using System;
using System.Collections.Generic;
using System.ServiceModel;
using SCME.Types.DataContracts;

namespace SCME.Types.DatabaseServer
{
    [ServiceContract(Namespace = "http://proton-electrotex.com/SCME", SessionMode = SessionMode.Required)]
    public interface ICentralDatabaseService : IProfilesService, IProfilesConnectionService
    {
        #region IProfilesService service implementation
        /// <summary>
        /// Save profiles in central db
        /// </summary>
        /// <param name="profileItems"></param>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new void SaveProfiles(List<ProfileItem> profileItems);

        /// <summary>
        /// Return profiles from db
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new List<ProfileItem> GetProfileItems();

        /// <summary>
        /// Return profiles from central db
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new List<ProfileItem> GetProfileItemsByMme(string mmeCode);

        /// <summary>
        /// Save profiles in central db with mmeCode
        /// </summary>
        /// <param name="profileItems"></param>
        /// <param name="mmeCode"></param>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new void SaveProfilesFromMme(List<ProfileItem> profileItems, string mmeCode);

        /// <summary>
        /// Get profile by profName
        /// </summary>
        /// <param name="profName"></param>
        /// <param name="mmmeCode"></param>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new ProfileItem GetProfileByProfName(string profName, string mmmeCode, ref bool Found);
        
        #endregion


        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new IEnumerable<MmeCode> GetMmeCodes();

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new IEnumerable<ProfileMme> GetMmeProfiles(long mmeCodeId);

        [OperationContract]
        [FaultContract(typeof(FaultData))]
        new void SaveConnections(List<MmeCode> mmeCodes);
        

        /// <summary>
        /// Saving result in central db
        /// </summary>
        /// <param name="results"></param>
        /// <param name="errors"></param>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        void SaveResults(ResultItem results, List<string> errors);

        
        /// <summary>
        /// Get groups from central db
        /// </summary>
        /// <param name="from">Date from</param>
        /// <param name="to">Date to</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        List<string> GetGroups(DateTime? from, DateTime? to);

        /// <summary>
        /// Get devices from central db
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        List<DeviceItem> GetDevices(string @group);

        /// <summary>
        /// Get device errors from central db
        /// </summary>
        /// <param name="internalId"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        List<int> ReadDeviceErrors(long internalId);

        /// <summary>
        /// Get device parameters
        /// </summary>
        /// <param name="internalId">Dev Id</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        List<ParameterItem> ReadDeviceParameters(long internalId);
        
        /// <summary>
        /// Get device conditions
        /// </summary>
        /// <param name="internalId">Dev Id</param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        List<ConditionItem> ReadDeviceConditions(long internalId);

        /// <summary>
        /// Get device normatives
        /// </summary>
        /// <param name="internalId"></param>
        /// <returns></returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        List<ParameterNormativeItem> ReadDeviceNormatives(long internalId);

        /// <summary>
        /// Write result to central db
        /// </summary>
        /// <param name="localDevice">Local result</param>
        /// <returns>Result of inserting</returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        bool SendResultToServer(DeviceLocalItem localDevice);

        /// <summary>
        /// Check
        /// </summary>
        /// <returns>Result of inserting</returns>
        [OperationContract(IsOneWay = true)]
        void Check();

        /// <summary>
        /// Read Device RT Class
        /// </summary>
        /// <returns>RT Class</returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        int? ReadDeviceRTClass(string devCode, string profileName);

        /// <summary>
        /// Read Device fact Class
        /// </summary>
        /// <returns>RT Class</returns>
        [OperationContract]
        [FaultContract(typeof(FaultData))]
        int? ReadDeviceClass(string devCode, string profileName);
    }
}
