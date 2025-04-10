using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public class JiraTracker
    {
        private readonly JiraActivityService jiraActivityService;
        private DateTime? lastFetchedDate;
        private List<JiraActivity> jiraActivities;

        public JiraTracker(JiraActivityService jiraActivityService)
        {
            this.jiraActivityService = jiraActivityService;
            this.jiraActivities = new List<JiraActivity>();
        }

        public async Task RecognizeActivity()
        {
            // Process activities (if needed for real-time tracking)
        }

        public IReadOnlyCollection<JiraActivity> GetJiraActivities()
        {
            if (this.lastFetchedDate == null || this.lastFetchedDate.Value < DateTime.Now.AddMinutes(-5))
            {
                this.lastFetchedDate = DateTime.Now;
                this.jiraActivities = this.jiraActivityService.GetTodayActivities();
            }

            return this.jiraActivities;
        }
    }
}