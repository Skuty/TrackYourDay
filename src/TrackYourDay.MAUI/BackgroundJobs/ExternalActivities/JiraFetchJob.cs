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
        private readonly IJiraCurrentStateService _currentStateService;
        private readonly ILogger<JiraFetchJob> _logger;

        public JiraFetchJob(
            JiraTracker tracker,
            IJiraCurrentStateService currentStateService,
            ILogger<JiraFetchJob> logger)
        {
            _tracker = tracker;
            _currentStateService = currentStateService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Jira fetch job started");

                var newActivityCount = await _tracker.RecognizeActivitiesAsync(context.CancellationToken).ConfigureAwait(false);
                await _currentStateService.SyncStateFromRemoteService(context.CancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Jira fetch job completed: {NewCount} new activities", 
                    newActivityCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Jira fetch job failed");
                throw;
            }
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
