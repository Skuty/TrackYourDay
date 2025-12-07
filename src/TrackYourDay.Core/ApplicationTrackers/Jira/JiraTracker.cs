using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public class JiraTracker
    {
        private readonly IJiraActivityService jiraActivityService;
        private readonly IHistoricalDataRepository<JiraActivity>? repository;
        private readonly IClock clock;
        private DateTime? lastFetchedDate;
        private List<JiraActivity> jiraActivities;

        public JiraTracker(
            IJiraActivityService jiraActivityService, 
            IClock clock,
            IHistoricalDataRepository<JiraActivity>? repository = null)
        {
            this.jiraActivityService = jiraActivityService;
            this.clock = clock;
            this.repository = repository;
            this.jiraActivities = new List<JiraActivity>();
        }

        public void RecognizeActivity()
        {
            if (this.lastFetchedDate == null)
            {
                var todayActivities = this.jiraActivityService.GetActivitiesUpdatedAfter(DateTime.Today);
                this.jiraActivities.AddRange(todayActivities);
                this.lastFetchedDate = this.clock.Now;
            }

            if (lastFetchedDate.Value < this.clock.Now.AddMinutes(-5))
            {
                var newActivities = this.jiraActivityService.GetActivitiesUpdatedAfter(this.lastFetchedDate.Value);
                this.jiraActivities.AddRange(newActivities);
                this.lastFetchedDate = this.clock.Now;
            }
        }

        public IReadOnlyCollection<JiraActivity> GetJiraActivities()
        {
            this.RecognizeActivity();
            return this.jiraActivities;
        }

        public IReadOnlyCollection<JiraActivity> GetJiraActivitiesForDate(DateOnly date)
        {
            if (repository == null)
            {
                // If repository is not available, return today's activities if date is today
                if (date == DateOnly.FromDateTime(DateTime.Today))
                {
                    return GetJiraActivities();
                }
                return new List<JiraActivity>();
            }

            var specification = new JiraActivityByDateSpecification(date);
            return repository.Find(specification);
        }

        public IReadOnlyCollection<JiraActivity> GetJiraActivitiesForDateRange(DateOnly startDate, DateOnly endDate)
        {
            if (repository == null)
            {
                // If repository is not available, return today's activities if range includes today
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (startDate <= today && endDate >= today)
                {
                    return GetJiraActivities();
                }
                return new List<JiraActivity>();
            }

            var specification = new JiraActivityByDateRangeSpecification(startDate, endDate);
            return repository.Find(specification);
        }
    }
}