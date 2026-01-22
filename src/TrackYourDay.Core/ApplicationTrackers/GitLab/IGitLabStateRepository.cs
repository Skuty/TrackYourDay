using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab;

/// <summary>
/// Repository for persisting GitLab state snapshots.
/// </summary>
public interface IGitLabStateRepository
{
    /// <summary>
    /// Saves a GitLab state snapshot.
    /// </summary>
    Task SaveAsync(GitLabStateSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent snapshot.
    /// </summary>
    Task<GitLabStateSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves snapshots within a date range.
    /// </summary>
    Task<List<GitLabStateSnapshot>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
