using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    /// <summary>
    /// Unified GitLab activity tracker with dual deduplication validation.
    /// Uses GUID-based persistence as primary mechanism with timestamp-based validation.
    /// </summary>
    public sealed class GitLabTracker
    {
        private readonly IGitLabActivityService _activityService;
        private readonly IHistoricalDataRepository<GitLabActivity> _repository;
        private readonly IGitLabSettingsService _settingsService;
        private readonly IPublisher _publisher;
        private readonly ILogger<GitLabTracker> _logger;

        public GitLabTracker(
            IGitLabActivityService activityService,
            IHistoricalDataRepository<GitLabActivity> repository,
            IGitLabSettingsService settingsService,
            IPublisher publisher,
            ILogger<GitLabTracker> logger)
        {
            _activityService = activityService;
            _repository = repository;
            _settingsService = settingsService;
            _publisher = publisher;
            _logger = logger;
        }

        /// <summary>
        /// Discovers and processes new GitLab activities.
        /// Primary: GUID-based deduplication via repository.
        /// Secondary: Timestamp validation for consistency checking.
        /// </summary>
        public async Task<int> RecognizeActivitiesAsync(CancellationToken cancellationToken = default)
        {
            var syncStartTime = DateTime.UtcNow;
            var settings = _settingsService.GetSettings();
            var watermark = settings.LastSyncTimestamp ?? syncStartTime.AddDays(-7);
            
            _logger.LogInformation("GitLab activity recognition started, fetching since {Watermark}", watermark);

            var activities = await _activityService.GetActivitiesUpdatedAfter(watermark, cancellationToken).ConfigureAwait(false);

            var newActivityCount = 0;
            foreach (var activity in activities)
            {
                // Primary: GUID-based deduplication (repository enforces uniqueness)
                var isNewByGuid = await _repository.TryAppendAsync(activity, cancellationToken).ConfigureAwait(false);
                
                // Secondary: Timestamp-based validation (consistency check)
                var isNewByTimestamp = activity.OccuranceDate > watermark;
                
                // Log mismatches for debugging/monitoring
                if (isNewByGuid != isNewByTimestamp)
                {
                    _logger.LogWarning(
                        "Deduplication mismatch for GitLabActivity {ActivityId}: " +
                        "GUID-based={GuidResult}, Timestamp-based={TimestampResult} " +
                        "(OccurrenceDate={OccurrenceDate}, Watermark={Watermark}). " +
                        "Description: {Description}",
                        activity.Guid, isNewByGuid, isNewByTimestamp, 
                        activity.OccuranceDate, watermark, activity.Description);
                }
                
                // Publish event only for truly new activities (GUID-based decision)
                if (isNewByGuid)
                {
                    newActivityCount++;
                    await _publisher.Publish(
                        new GitLabActivityDiscoveredEvent(activity.Guid, activity), 
                        cancellationToken).ConfigureAwait(false);
                    
                    _logger.LogInformation("GitLab activity discovered: {Description}", activity.Description);
                }
            }

            // Update watermark to current sync time
            _settingsService.UpdateLastSyncTimestamp(syncStartTime);
            _settingsService.PersistSettings();

            _logger.LogInformation(
                "GitLab activity recognition completed: {TotalCount} activities processed, {NewCount} new",
                activities.Count, newActivityCount);

            return newActivityCount;
        }

        /// <summary>
        /// Gets GitLab activities for a specific date range from repository.
        /// </summary>
        public async Task<IReadOnlyCollection<GitLabActivity>> GetActivitiesAsync(
            DateOnly fromDate, 
            DateOnly toDate, 
            CancellationToken cancellationToken = default)
        {
            var specification = new DateRangeSpecification<GitLabActivity>(fromDate, toDate);
            return await _repository.FindAsync(specification, cancellationToken).ConfigureAwait(false);
        }
    }
}
