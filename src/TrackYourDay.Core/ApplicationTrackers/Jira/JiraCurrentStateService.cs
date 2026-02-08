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
        private readonly IJiraSettingsService _jiraSettings;
        private readonly IClock _clock;
        private readonly ILogger<JiraCurrentStateService> _logger;
        private JiraUser? _currentUser;

        public JiraCurrentStateService(
            IJiraRestApiClient restApiClient,
            IJiraIssueRepository issueRepository,
            IJiraSettingsService jiraSettings,
            IClock clock,
            ILogger<JiraCurrentStateService> logger)
        {
            _restApiClient = restApiClient;
            _issueRepository = issueRepository;
            _jiraSettings = jiraSettings;
            _clock = clock;
            _logger = logger;
        }

        public async Task SyncStateFromRemoteService(CancellationToken cancellationToken)
        {
            _currentUser ??= await _restApiClient.GetCurrentUser().ConfigureAwait(false);

            var syncStartTime = _clock.Now.ToUniversalTime();
            var lookbackDays = 7;
            var issues = await _restApiClient
                .GetUserIssues(_currentUser, syncStartTime.AddDays(-lookbackDays))
                .ConfigureAwait(false);

            var baseUrl = GetJiraBaseUrl();
            var currentIssues = issues.Select(issue => MapToJiraIssueState(issue, baseUrl)).ToList();
            
            await _issueRepository
                .UpdateCurrentStateAsync(currentIssues, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Synchronized {IssueCount} Jira issues for user {User}",
                currentIssues.Count,
                _currentUser.DisplayName);
        }

        private static JiraIssueState MapToJiraIssueState(JiraIssueResponse response, string baseUrl)
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
                AssigneeDisplayName = response.Fields.Assignee?.DisplayName,
                BrowseUrl = ConstructBrowseUrl(baseUrl, response.Key)
            };
        }

        private string GetJiraBaseUrl()
        {
            var settings = _jiraSettings.GetSettings();
            var apiUrl = settings?.ApiUrl;

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                _logger.LogWarning("Jira API URL is not configured, issue URLs will be unavailable");
                return string.Empty;
            }

            try
            {
                var uri = new Uri(apiUrl);
                return $"{uri.Scheme}://{uri.Host}";
            }
            catch (UriFormatException ex)
            {
                _logger.LogWarning(ex, "Invalid Jira API URL format: {ApiUrl}", apiUrl);
                return string.Empty;
            }
        }

        private static string ConstructBrowseUrl(string baseUrl, string issueKey)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(issueKey))
            {
                return "#";
            }

            return $"{baseUrl}/browse/{issueKey}";
        }
    }
}
