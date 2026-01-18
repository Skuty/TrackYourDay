using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    /// <summary>
    /// Synchronizes current state of assigned Jira issues.
    /// </summary>
    public sealed class JiraCurrentStateService : IJiraCurrentStateService
    {
        private readonly IJiraRestApiClient _restApiClient;
        private readonly IJiraIssueRepository _issueRepository;
        private readonly IClock _clock;
        private readonly ILogger<JiraCurrentStateService> _logger;
        private JiraUser? _currentUser;

        public JiraCurrentStateService(
            IJiraRestApiClient restApiClient,
            IJiraIssueRepository issueRepository,
            IClock clock,
            ILogger<JiraCurrentStateService> logger)
        {
            _restApiClient = restApiClient;
            _issueRepository = issueRepository;
            _clock = clock;
            _logger = logger;
        }

        public async Task<int> SyncCurrentStateAsync(CancellationToken cancellationToken)
        {
            _currentUser ??= await _restApiClient.GetCurrentUser().ConfigureAwait(false);

            var syncStartTime = _clock.Now.ToUniversalTime();
            var lookbackDays = 7;
            var issues = await _restApiClient
                .GetUserIssues(_currentUser, syncStartTime.AddDays(-lookbackDays))
                .ConfigureAwait(false);

            var currentIssues = issues.Select(MapToJiraIssueState).ToList();
            
            await _issueRepository
                .UpdateCurrentStateAsync(currentIssues, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Synchronized {IssueCount} Jira issues for user {User}",
                currentIssues.Count,
                _currentUser.DisplayName);

            return currentIssues.Count;
        }

        private static JiraIssueState MapToJiraIssueState(JiraIssueResponse response)
        {
            return new JiraIssueState
            {
                Key = response.Key,
                Id = response.Id,
                Summary = response.Fields.Summary ?? string.Empty,
                Status = response.Fields.Status?.Name ?? "Unknown",
                IssueType = response.Fields.IssueType?.Name ?? "Unknown",
                ProjectKey = response.Fields.Project?.Key ?? "Unknown",
                Updated = response.Fields.Updated,
                Created = response.Fields.Created,
                AssigneeDisplayName = response.Fields.Assignee?.DisplayName
            };
        }
    }
}
