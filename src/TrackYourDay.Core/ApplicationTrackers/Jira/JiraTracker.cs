using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.ApplicationTrackers.Shared;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    /// <summary>
    /// Unified Jira activity tracker with dual deduplication validation.
    /// Uses GUID-based persistence as primary mechanism with timestamp-based validation.
    /// </summary>
    public sealed class JiraTracker : IExternalActivityTracker<JiraActivity>
    {
        private readonly IJiraActivityService _activityService;
        private readonly IJiraActivityRepository _repository;
        private readonly IJiraSettingsService _settingsService;
        private readonly ILogger<JiraTracker> _logger;

        public JiraTracker(
            IJiraActivityService activityService,
            IJiraActivityRepository repository,
            IJiraSettingsService settingsService,
            ILogger<JiraTracker> logger)
        {
            _activityService = activityService;
            _repository = repository;
            _settingsService = settingsService;
            _logger = logger;
        }

        /// <summary>
        /// Discovers and processes new Jira activities.
        /// Primary: GUID-based deduplication via repository.
        /// Secondary: Timestamp validation for consistency checking.
        /// </summary>
        public async Task<int> RecognizeActivitiesAsync(CancellationToken cancellationToken = default)
        {
            var syncStartTime = DateTime.UtcNow;
            var settings = _settingsService.GetSettings();
            var watermark = settings.LastSyncTimestamp ?? syncStartTime.AddDays(-7);
            
            _logger.LogInformation("Jira activity recognition started, fetching since {Watermark}", watermark);

            var activities = await _activityService.GetActivitiesUpdatedAfter(watermark).ConfigureAwait(false);

            var newActivityCount = 0;
            foreach (var activity in activities)
            {
                // Primary: GUID-based deduplication (repository enforces uniqueness)
                var isNewByGuid = await _repository.TryAppendAsync(activity, cancellationToken).ConfigureAwait(false);
                
                // Secondary: Timestamp-based validation (consistency check)
                var isNewByTimestamp = activity.OccurrenceDate > watermark;
                
                // Log mismatches for debugging/monitoring
                if (isNewByGuid != isNewByTimestamp)
                {
                    _logger.LogWarning(
                        "Deduplication mismatch for JiraActivity {ActivityId}: " +
                        "GUID-based={GuidResult}, Timestamp-based={TimestampResult} " +
                        "(OccurrenceDate={OccurrenceDate}, Watermark={Watermark}). " +
                        "Description: {Description}",
                        activity.Guid, isNewByGuid, isNewByTimestamp, 
                        activity.OccurrenceDate, watermark, activity.Description);
                }
                
                // Count new activities (GUID-based decision, no event publishing for Jira)
                if (isNewByGuid)
                {
                    newActivityCount++;
                    _logger.LogDebug("Jira activity discovered: {Description}", activity.Description);
                }
            }

            // Update watermark to current sync time
            _settingsService.UpdateLastSyncTimestamp(syncStartTime);
            _settingsService.PersistSettings();

            _logger.LogInformation(
                "Jira activity recognition completed: {TotalCount} activities processed, {NewCount} new",
                activities.Count, newActivityCount);

            return newActivityCount;
        }

        /// <summary>
        /// Gets Jira activities for a specific date range from repository.
        /// </summary>
        public async Task<IReadOnlyCollection<JiraActivity>> GetActivitiesAsync(
            DateOnly fromDate, 
            DateOnly toDate, 
            CancellationToken cancellationToken = default)
        {
            return await _repository.GetActivitiesAsync(fromDate, toDate, cancellationToken).ConfigureAwait(false);
        }
    }
}