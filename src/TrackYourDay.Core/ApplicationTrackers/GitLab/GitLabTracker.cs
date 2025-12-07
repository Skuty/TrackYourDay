using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public class GitLabTracker
    {
        private readonly GitLabActivityService gitLabActivityService;
        private readonly IHistoricalDataRepository<GitLabActivity>? repository;
        private readonly IClock clock;
        private DateTime? lastFetchedDate;
        private List<GitLabActivity> gitlabActivities;

        public GitLabTracker(
            GitLabActivityService gitLabActivityService,
            IHistoricalDataRepository<GitLabActivity>? repository = null,
            IClock? clock = null)
        {
            this.gitLabActivityService = gitLabActivityService;
            this.repository = repository;
            this.clock = clock ?? new Clock();
            this.gitlabActivities = new List<GitLabActivity>();
        }

        public async Task RecognizeActivity()
        {
            // Process activities
        }

        public IReadOnlyCollection<GitLabActivity> GetGitLabActivities()
        {
            if (this.lastFetchedDate == null || this.lastFetchedDate.Value < DateTime.Now.AddMinutes(-5))
            {
                this.lastFetchedDate = DateTime.Now;
                this.gitlabActivities = this.gitLabActivityService.GetTodayActivities();
            }

            return this.gitlabActivities;
        }

        public IReadOnlyCollection<GitLabActivity> GetGitLabActivitiesForDate(DateOnly date)
        {
            if (repository == null)
            {
                // If repository is not available, return today's activities if date is today
                if (date == DateOnly.FromDateTime(DateTime.Today))
                {
                    return GetGitLabActivities();
                }
                return new List<GitLabActivity>();
            }

            var specification = new GitLabActivityByDateSpecification(date);
            return repository.Find(specification);
        }

        public IReadOnlyCollection<GitLabActivity> GetGitLabActivitiesForDateRange(DateOnly startDate, DateOnly endDate)
        {
            if (repository == null)
            {
                // If repository is not available, return today's activities if range includes today
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (startDate <= today && endDate >= today)
                {
                    return GetGitLabActivities();
                }
                return new List<GitLabActivity>();
            }

            var specification = new GitLabActivityByDateRangeSpecification(startDate, endDate);
            return repository.Find(specification);
        }
    }
}
