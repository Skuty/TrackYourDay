using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public class GitLabTracker
    {
        private const string LastFetchTimestampKey = "GitLabTracker.LastFetchTimestamp";
        private readonly IGitLabActivityService gitLabActivityService;
        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly IGenericSettingsService settingsService;
        private readonly ILogger<GitLabTracker> logger;
        private List<GitLabActivity> publishedActivities;

        public GitLabTracker(
            IGitLabActivityService gitLabActivityService,
            IClock clock,
            IPublisher publisher,
            IGenericSettingsService settingsService,
            ILogger<GitLabTracker> logger)
        {
            this.gitLabActivityService = gitLabActivityService;
            this.clock = clock;
            this.publisher = publisher;
            this.settingsService = settingsService;
            this.logger = logger;
            this.publishedActivities = new List<GitLabActivity>();
        }

        public void RecognizeActivity()
        {
            var lastFetchTimestamp = this.settingsService.GetSetting<DateTime>(LastFetchTimestampKey, DateTime.MinValue);
            var allActivities = this.gitLabActivityService.GetTodayActivities();
            
            // Filter activities that occurred after the last fetch timestamp
            var newActivities = allActivities
                .Where(a => a.OccuranceDate > lastFetchTimestamp)
                .OrderBy(a => a.OccuranceDate)
                .ToList();

            // Publish events for new activities
            foreach (var activity in newActivities)
            {
                this.publishedActivities.Add(activity);
                this.publisher.Publish(new GitLabActivityDiscoveredEvent(Guid.NewGuid(), activity), CancellationToken.None);
                this.logger.LogInformation("GitLab activity discovered: {0}", activity.Description);
            }

            // Update last fetch timestamp
            if (newActivities.Any())
            {
                var latestActivityDate = newActivities.Max(a => a.OccuranceDate);
                this.settingsService.SetSetting(LastFetchTimestampKey, latestActivityDate);
                this.settingsService.PersistSettings();
            }
        }

        public IReadOnlyCollection<GitLabActivity> GetGitLabActivities()
        {
            return this.publishedActivities.AsReadOnly();
        }
    }
}
