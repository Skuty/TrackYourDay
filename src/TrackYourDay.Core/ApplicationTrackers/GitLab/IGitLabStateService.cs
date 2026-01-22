using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab;

/// <summary>
/// Service for managing GitLab state snapshots.
/// </summary>
public interface IGitLabStateService
{
    /// <summary>
    /// Captures the current state of GitLab work items (issues and merge requests).
    /// </summary>
    Task<GitLabStateSnapshot> CaptureCurrentStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent state snapshot.
    /// </summary>
    Task<GitLabStateSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all state snapshots within a date range.
    /// </summary>
    Task<List<GitLabStateSnapshot>> GetSnapshotsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
