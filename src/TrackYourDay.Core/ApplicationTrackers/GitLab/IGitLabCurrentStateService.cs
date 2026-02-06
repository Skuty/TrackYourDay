namespace TrackYourDay.Core.ApplicationTrackers.GitLab;

/// <summary>
/// Synchronizes current state of assigned GitLab work items.
/// </summary>
public interface IGitLabCurrentStateService
{
    /// <summary>
    /// Fetches current user work items and updates the repository state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SyncStateFromRemoteService(CancellationToken cancellationToken);
}
