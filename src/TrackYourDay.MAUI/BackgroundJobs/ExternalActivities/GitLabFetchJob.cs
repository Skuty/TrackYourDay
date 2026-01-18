using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents;
using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.MAUI.BackgroundJobs.ExternalActivities
{
    internal sealed class GitLabFetchJob : IJob
    {
        private readonly IGitLabActivityService _activityService;
        private readonly IGitLabActivityRepository _repository;
        private readonly IGitLabSettingsService _settingsService;
        private readonly IPublisher _publisher;
        private readonly ILogger<GitLabFetchJob> _logger;

        public GitLabFetchJob(
            IGitLabActivityService activityService,
            IGitLabActivityRepository repository,
            IGitLabSettingsService settingsService,
            IPublisher publisher,
            ILogger<GitLabFetchJob> logger)
        {
            _activityService = activityService;
            _repository = repository;
            _settingsService = settingsService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var syncStartTime = DateTime.UtcNow;
                var settings = _settingsService.GetSettings();
                var startDate = settings.LastSyncTimestamp ?? syncStartTime;
                
                _logger.LogInformation("GitLab fetch job started, syncing from {StartDate}", startDate);

                var activities = await _activityService.GetActivitiesUpdatedAfter(startDate, context.CancellationToken).ConfigureAwait(false);

                var newActivityCount = 0;
                foreach (var activity in activities)
                {
                    var isNew = await _repository.TryAppendAsync(activity, context.CancellationToken).ConfigureAwait(false);
                    if (isNew)
                    {
                        newActivityCount++;
                        await _publisher.Publish(new GitLabActivityDiscoveredEvent(activity.Guid, activity), context.CancellationToken).ConfigureAwait(false);
                    }
                }

                _settingsService.UpdateLastSyncTimestamp(syncStartTime);
                _settingsService.PersistSettings();

                _logger.LogInformation("GitLab fetch job completed: {ActivityCount} activities ({NewCount} new), watermark updated to {SyncTime}",
                    activities.Count, newActivityCount, syncStartTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GitLab fetch job failed");
                throw;
            }
        }

        internal static IJobDetail CreateJobDetail()
        {
            return JobBuilder.Create<GitLabFetchJob>()
                .WithIdentity("GitLabFetch", "ExternalActivities")
                .Build();
        }

        internal static ITrigger CreateTrigger(int intervalMinutes)
        {
            return TriggerBuilder.Create()
                .WithIdentity("GitLabFetch", "ExternalActivities")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(intervalMinutes)
                    .RepeatForever())
                .Build();
        }
    }
}
