namespace TrackYourDay.Core.ApplicationTrackers.Persistence
{
    /// <summary>
    /// Persists GitLab activities as an append-only log.
    /// </summary>
    public interface IGitLabActivityRepository
    {
        /// <summary>
        /// Appends a new activity if it doesn't already exist.
        /// </summary>
        /// <param name="activity">The activity to store.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if added, false if duplicate.</returns>
        Task<bool> TryAppendAsync(GitLab.GitLabActivity activity, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves activities within a date range.
        /// </summary>
        Task<IReadOnlyCollection<GitLab.GitLabActivity>> GetActivitiesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
    }
}
