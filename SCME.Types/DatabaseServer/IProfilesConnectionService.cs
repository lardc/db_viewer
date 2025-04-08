using System;
using System.Collections.Generic;
using SCME.Types.DataContracts;

namespace SCME.Types.DatabaseServer
{
    /// <summary>
    /// Service works with connections between profiles and MME codes
    /// </summary>
    public interface IProfilesConnectionService : IDisposable
    {
        /// <summary>
        /// Return all mme codes
        /// </summary>
        /// <returns></returns>
        IEnumerable<MmeCode> GetMmeCodes();

        IEnumerable<ProfileMme> GetMmeProfiles(long mmeCodeId);

        /// <summary>
        /// Save connections between code and profiles
        /// </summary>
        void SaveConnections(List<MmeCode> mmeCodes);
    }
}
