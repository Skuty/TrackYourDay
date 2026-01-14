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
        private readonly IPublisher _publisher;
        private readonly ILogger<GitLabFetchJob> _logger;

        public GitLabFetchJob(
            IGitLabActivityService activityService,
            IGitLabActivityRepository repository,
            IPublisher publisher,
            ILogger<GitLabFetchJob> logger)
        {
            _activityService = activityService;
            _repository = repository;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("GitLab fetch job started");

                var activities = _activityService.GetTodayActivities();

                var newActivityCount = 0;
                foreach (var activity in activities)
                {
                    var isNew = await _repository.TryAppendAsync(activity, context.CancellationToken);
                    if (isNew)
                    {
                        newActivityCount++;
                        await _publisher.Publish(new GitLabActivityDiscoveredEvent(activity.Guid, activity), context.CancellationToken);
                    }
                }

                _logger.LogInformation("GitLab fetch job completed: {TotalCount} activities, {NewCount} new",
                    activities.Count, newActivityCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GitLab fetch job failed");
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
