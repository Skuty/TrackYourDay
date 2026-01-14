namespace TrackYourDay.Core.ApplicationTrackers.Persistence
{
    /// <summary>
    /// Manages the current state of assigned Jira issues.
    /// </summary>
    public interface IJiraIssueRepository
    {
        /// <summary>
        /// Replaces the current set of assigned issues with the fresh fetch.
        /// </summary>
        Task UpdateCurrentStateAsync(IEnumerable<JiraIssueState> currentIssues, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all currently assigned issues.
        /// </summary>
        Task<IReadOnlyCollection<JiraIssueState>> GetCurrentIssuesAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents the current state of a Jira issue.
    /// </summary>
    public record JiraIssueState
    {
        public required string Key { get; init; }
        public required string Id { get; init; }
        public required string Summary { get; init; }
        public required string Status { get; init; }
        public required string IssueType { get; init; }
        public required string ProjectKey { get; init; }
        public required DateTimeOffset Updated { get; init; }
        public DateTimeOffset? Created { get; init; }
        public string? AssigneeDisplayName { get; init; }
    }
}
