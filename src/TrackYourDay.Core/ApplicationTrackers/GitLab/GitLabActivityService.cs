namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public record class GitLabActivity(DateTime OccuranceDate, string Description);

    //TODO: Split namespaces for internal and eternal objects like GitLabACtivity and GitLabCommit

    public class GitLabActivityService
    {
        private readonly IGitLabRestApiClient gitLabRestApiClient;
        private GitLabUserId userId;
        private string userEmail;
        private List<GitLabActivity> gitlabActivities;

        public GitLabActivityService(IGitLabRestApiClient gitLabRestApiClient)
        {
            this.gitLabRestApiClient = gitLabRestApiClient;
            this.gitlabActivities = new List<GitLabActivity>();
        }

        public List<GitLabActivity> GetTodayActivities()
        {
            this.gitlabActivities.Clear();

            if (this.userId == null)
            {
                var user = this.gitLabRestApiClient.GetCurrentUser();
                this.userId = new GitLabUserId(user.Id);
                this.userEmail = user.Email;
            }

            var events = this.gitLabRestApiClient.GetUserEvents(new GitLabUserId(this.userId.Id), DateOnly.FromDateTime(DateTime.Today));

            foreach(var gitlabEvent in events)
            {
                // TODO: store GitLabData and only add new items to it instead of repulling everything from scratch
                var gitLabActivity = this.MapGitLabEventToGitLabActivity(gitlabEvent);

                this.gitlabActivities.AddRange(gitLabActivity);
            }

            return this.gitlabActivities;
        }

        private List<GitLabActivity> MapGitLabEventToGitLabActivity(GitLabEvent gitlabEvent)
        {
            if (gitlabEvent == null)
            {
                return null;
            }

            if (gitlabEvent.PushData != null)
            {
                var project = this.gitLabRestApiClient.GetProject(new GitLabProjectId(gitlabEvent.ProjectId));
                var projectName = project.NameWithNamespace;
                var commits = this.gitLabRestApiClient.GetCommits(new GitLabProjectId(gitlabEvent.ProjectId), new GitLabRefName(gitlabEvent.PushData.Ref), DateOnly.FromDateTime(DateTime.Today))
                    .Where(c => c.AuthorEmail == this.userEmail);

                var gitLabActivities = new List<GitLabActivity>();
                foreach (var commit in commits)
                {
                    gitlabActivities.Add(new GitLabActivity(commit.CreatedAt.DateTime, $"Commit done to Repository: {projectName}, to branch: {gitlabEvent.PushData.Ref}, with Title: {commit.Title}"));
                }

                return gitLabActivities;
            }

            if (gitlabEvent.TargetType == "MergeRequest" && gitlabEvent.Action == "opened")
            {
                return new List<GitLabActivity>
                {
                    new GitLabActivity(gitlabEvent.CreatedAt.DateTime, $"Merge Request Opened with Title: {gitlabEvent.TargetTitle}")
                };
            }

            return new List<GitLabActivity>
            {
                new GitLabActivity(gitlabEvent.CreatedAt.DateTime, $"{gitlabEvent.Action} {gitlabEvent.TargetType} {gitlabEvent.TargetTitle}")
            };
        }

        //TODO: Add tests for duplicated events
    }
}
