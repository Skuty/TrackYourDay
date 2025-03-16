namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public record class GitLabActivity(Guid Guid, DateTime OccuranceDate, string Description);

    public class GitLabActivityService
    {
        private readonly IGitLabRestApiClient gitLabRestApiClient;
        private GitLabUserId userId;

        public GitLabActivityService(IGitLabRestApiClient gitLabRestApiClient)
        {
            this.gitLabRestApiClient = gitLabRestApiClient;
        }

        public List<GitLabActivity> GetTodayActivities()
        {
            if (this.userId == null)
            {
                var user = this.gitLabRestApiClient.GetCurrentUser();
                this.userId = new GitLabUserId(user.Id);
            }

            var events = this.gitLabRestApiClient.GetUserEvents(new GitLabUserId(this.userId.Id), DateOnly.FromDateTime(DateTime.Today));

            return events.Select(e => new GitLabActivity(Guid.NewGuid(), e.CreatedAt.DateTime, e.Action)).ToList();
        }

        private string GetBasicDescription(GitLabEvent e)
        {
            return string.Empty;
        }

        private string GetPushDescription(GitLabEvent e)
        {
            return string.Empty;
        }

        private string GetMergeRequestDescription(GitLabEvent e)
        {
            return string.Empty;
        }

        private string GetProjectName(GitLabEvent e)
        {
            return string.Empty;
        }
    }
}
