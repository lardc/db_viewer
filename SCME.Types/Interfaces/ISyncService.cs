using System;

namespace SCME.Types.Interfaces
{
    public delegate void AfterSyncResultsRoutine(string Error);
    public delegate void AfterSyncProfilesRoutine(string Error);

    public interface ISyncService : IDisposable
    {
        /// <summary>
        /// Send local results to central db
        /// </summary>
        void SyncResults(AfterSyncResultsRoutine afterSyncResultsRoutine);

        /// <summary>
        /// Refresh local profiles from central db
        /// </summary>
        void SyncProfiles(AfterSyncProfilesRoutine afterSyncProfilesRoutine);
    }
}
