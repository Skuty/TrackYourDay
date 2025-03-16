using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public class GitLabTracker
    {
        private readonly GitLabActivityService gitLabActivityService;

        public GitLabTracker(GitLabActivityService gitLabActivityService)
        {
            this.gitLabActivityService = gitLabActivityService;
        }

        public async Task RecognizeActivity()
        {
            // Process activities
        }

        public async Task GetGitLabActivities()
        {
            // Process activities
        }
    }
}
