using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public class GitLabTracker
    {

        public GitLabTracker()
        {
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

    public record class GitLabActivity(Guid Guid, DateTime OccuranceDate, string Description); 
}
