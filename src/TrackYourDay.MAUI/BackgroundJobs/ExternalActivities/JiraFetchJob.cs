using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.MAUI.BackgroundJobs.ExternalActivities
{
    internal sealed class JiraFetchJob : IJob
    {
        private readonly IJiraActivityService _activityService;
        private readonly IJiraActivityRepository _activityRepository;
        private readonly IJiraIssueRepository _issueRepository;
        private readonly IJiraRestApiClient _restApiClient;
        private readonly IJiraSettingsService _settingsService;
        private readonly IPublisher _publisher;
        private readonly ILogger<JiraFetchJob> _logger;
        private JiraUser? _currentUser;

        public JiraFetchJob(
            IJiraActivityService activityService,
            IJiraActivityRepository activityRepository,
            IJiraIssueRepository issueRepository,
            IJiraRestApiClient restApiClient,
            IJiraSettingsService settingsService,
            IPublisher publisher,
            ILogger<JiraFetchJob> logger)
        {
            _activityService = activityService;
            _activityRepository = activityRepository;
            _issueRepository = issueRepository;
            _restApiClient = restApiClient;
            _settingsService = settingsService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var syncStartTime = DateTime.UtcNow;
                var startDate = _settingsService.GetSyncStartDate();
                
                _logger.LogInformation("Jira fetch job started, syncing from {StartDate}", startDate);

                _currentUser ??= await _restApiClient.GetCurrentUser().ConfigureAwait(false);

                var activities = await _activityService.GetActivitiesUpdatedAfter(startDate).ConfigureAwait(false);

                var newActivityCount = 0;
                foreach (var activity in activities)
                {
                    var isNew = await _activityRepository.TryAppendAsync(activity, context.CancellationToken).ConfigureAwait(false);
                    if (isNew)
                    {
                        newActivityCount++;
                    }
                }

                var issues = await _restApiClient.GetUserIssues(_currentUser, startDate).ConfigureAwait(false);
                var currentIssues = issues.Select(MapToJiraIssue).ToList();
                await _issueRepository.UpdateCurrentStateAsync(currentIssues, context.CancellationToken).ConfigureAwait(false);

                _settingsService.UpdateLastSyncTimestamp(syncStartTime);
                _settingsService.PersistSettings();

                _logger.LogInformation("Jira fetch job completed: {ActivityCount} activities ({NewCount} new), {IssueCount} current issues, watermark updated to {SyncTime}",
                    activities.Count, newActivityCount, currentIssues.Count, syncStartTime);
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
