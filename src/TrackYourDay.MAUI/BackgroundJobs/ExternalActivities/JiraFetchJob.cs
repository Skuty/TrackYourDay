using Microsoft.Extensions.Logging;
using Quartz;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.MAUI.BackgroundJobs.ExternalActivities
{
    /// <summary>
    /// Quartz job wrapper for Jira activity tracking.
    /// Delegates all logic to JiraTracker for consolidation.
    /// </summary>
    internal sealed class JiraFetchJob : IJob
    {
        private readonly JiraTracker _tracker;
        private readonly IJiraIssueRepository _issueRepository;
        private readonly IJiraRestApiClient _restApiClient;
        private readonly ILogger<JiraFetchJob> _logger;
        private JiraUser? _currentUser;

        public JiraFetchJob(
            JiraTracker tracker,
            IJiraIssueRepository issueRepository,
            IJiraRestApiClient restApiClient,
            ILogger<JiraFetchJob> logger)
        {
            _tracker = tracker;
            _issueRepository = issueRepository;
            _restApiClient = restApiClient;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Jira fetch job started");
                
                _currentUser ??= await _restApiClient.GetCurrentUser().ConfigureAwait(false);

                // Recognize activities via tracker
                var newActivityCount = await _tracker.RecognizeActivitiesAsync(context.CancellationToken).ConfigureAwait(false);

                // Fetch and update current issues (separate concern)
                var syncStartTime = DateTime.UtcNow;
                var issues = await _restApiClient.GetUserIssues(_currentUser, syncStartTime.AddDays(-7)).ConfigureAwait(false);
                var currentIssues = issues.Select(MapToJiraIssue).ToList();
                await _issueRepository.UpdateCurrentStateAsync(currentIssues, context.CancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Jira fetch job completed: {NewCount} new activities, {IssueCount} current issues", 
                    newActivityCount, currentIssues.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Jira fetch job failed");
                throw;
            }
        }

        private static JiraIssueState MapToJiraIssue(JiraIssueResponse response)
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

        internal static IJobDetail CreateJobDetail()
        {
            return JobBuilder.Create<JiraFetchJob>()
                .WithIdentity("JiraFetch", "ExternalActivities")
                .Build();
        }

        internal static ITrigger CreateTrigger(int intervalMinutes)
        {
            return TriggerBuilder.Create()
                .WithIdentity("JiraFetch", "ExternalActivities")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(intervalMinutes)
                    .RepeatForever())
                .Build();
        }
    }
}
