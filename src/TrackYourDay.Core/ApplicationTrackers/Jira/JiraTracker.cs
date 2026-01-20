using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira.PublicEvents;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public class JiraTracker
    {
        private const string LastFetchTimestampKey = "JiraTracker.LastFetchTimestamp";
        private readonly IJiraActivityService jiraActivityService;
        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly IGenericSettingsService settingsService;
        private readonly ILogger<JiraTracker> logger;
        private List<JiraActivity> publishedActivities;

        public JiraTracker(
            IJiraActivityService jiraActivityService,
            IClock clock,
            IPublisher publisher,
            IGenericSettingsService settingsService,
            ILogger<JiraTracker> logger)
        {
            this.jiraActivityService = jiraActivityService;
            this.clock = clock;
            this.publisher = publisher;
            this.settingsService = settingsService;
            this.logger = logger;
            this.publishedActivities = new List<JiraActivity>();
        }

        public async Task RecognizeActivity()
        {
            var lastFetchTimestamp = this.settingsService.GetSetting<DateTime>(LastFetchTimestampKey, DateTime.Today);
            var allActivities = await this.jiraActivityService.GetActivitiesUpdatedAfter(lastFetchTimestamp);
            
            // Filter activities that occurred after the last fetch timestamp
            var newActivities = allActivities
                .Where(a => a.OccurrenceDate > lastFetchTimestamp)
                .OrderBy(a => a.OccurrenceDate)
                .ToList();

            // Publish events for new activities
            foreach (var activity in newActivities)
            {
                this.publishedActivities.Add(activity);
                await this.publisher.Publish(new JiraActivityDiscoveredEvent(Guid.NewGuid(), activity), CancellationToken.None);
                this.logger.LogInformation("Jira activity discovered: {0}", activity.Description);
            }

            // Update last fetch timestamp
            if (newActivities.Any())
            {
                var latestActivityDate = newActivities.Max(a => a.OccurrenceDate);
                this.settingsService.SetSetting(LastFetchTimestampKey, latestActivityDate);
                this.settingsService.PersistSettings();
            }
        }

        public async Task<IReadOnlyCollection<JiraActivity>> GetJiraActivities()
        {
            await this.RecognizeActivity();
            return this.publishedActivities.AsReadOnly();
        }
    }
}