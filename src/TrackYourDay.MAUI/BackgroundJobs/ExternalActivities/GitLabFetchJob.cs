using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;
using TrackYourDay.Core.ApplicationTrackers.GitLab;

namespace TrackYourDay.MAUI.BackgroundJobs.ExternalActivities
{
    /// <summary>
    /// Quartz job wrapper for GitLab activity tracking.
    /// Delegates all logic to GitLabTracker for consolidation.
    /// </summary>
    internal sealed class GitLabFetchJob : IJob
    {
        private readonly GitLabTracker _tracker;
        private readonly ILogger<GitLabFetchJob> _logger;

        public GitLabFetchJob(
            GitLabTracker tracker,
            ILogger<GitLabFetchJob> logger)
        {
            _tracker = tracker;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("GitLab fetch job started");
                
                var newActivityCount = await _tracker.RecognizeActivitiesAsync(context.CancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation("GitLab fetch job completed: {NewCount} new activities", newActivityCount);
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
