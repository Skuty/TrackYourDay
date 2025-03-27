using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public class GitLabTracker
    {
        private readonly GitLabActivityService gitLabActivityService;
        private DateTime? lastFetchedDate;
        private List<GitLabActivity> gitlabActivities;

        public GitLabTracker(GitLabActivityService gitLabActivityService)
        {
            this.gitLabActivityService = gitLabActivityService;
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
    }
}
