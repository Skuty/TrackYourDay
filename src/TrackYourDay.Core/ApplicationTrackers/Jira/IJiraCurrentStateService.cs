using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    /// <summary>
    /// Synchronizes current state of assigned Jira issues.
    /// </summary>
    public interface IJiraCurrentStateService
    {
        /// <summary>
        /// Fetches current user issues and updates the repository state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SyncStateFromRemoteService(CancellationToken cancellationToken);
    }
}
