using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public class JiraTracker
    {
        private readonly JiraActivityService jiraActivityService;
        private readonly IClock clock;
        private DateTime? lastFetchedDate;
        private List<JiraActivity> jiraActivities;

        public JiraTracker(JiraActivityService jiraActivityService, IClock clock)
        {
            this.jiraActivityService = jiraActivityService;
            this.clock = clock;
            this.jiraActivities = new List<JiraActivity>();
        }

        public void RecognizeActivity()
        {
            if (this.lastFetchedDate == null)
            {
                var todayActivities = this.jiraActivityService.GetActivitiesUpdatedAfter(DateTime.Today);
                this.jiraActivities.AddRange(todayActivities);
                this.lastFetchedDate = this.clock.Now; this.lastFetchedDate = this.clock.Now;
            }

            if (lastFetchedDate.Value < this.clock.Now.AddMinutes(-5))
            {
                var newActivities = this.jiraActivityService.GetActivitiesUpdatedAfter(this.lastFetchedDate.Value);
                this.jiraActivities.AddRange(newActivities);
                this.lastFetchedDate = this.clock.Now;
            }
        }

        public virtual IReadOnlyCollection<JiraActivity> GetJiraActivities()
        {
            this.RecognizeActivity();
            return this.jiraActivities;
        }
    }
}