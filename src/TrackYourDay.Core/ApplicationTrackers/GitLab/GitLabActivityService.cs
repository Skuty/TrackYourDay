using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public record class GitLabActivity(DateTime OccuranceDate, string Description);

    //TODO: Split namespaces for internal and eternal objects like GitLabACtivity and GitLabCommit

    public class GitLabActivityService
    {
        private readonly IGitLabRestApiClient gitLabRestApiClient;
        private readonly ILogger<GitLabActivityService> logger;
        private GitLabUserId userId;
        private string userEmail;
        private List<GitLabActivity> gitlabActivities;
        private bool stopFetchingDueToFailedRequests = false;

        public GitLabActivityService(IGitLabRestApiClient gitLabRestApiClient, ILogger<GitLabActivityService> logger)
        {
            this.gitLabRestApiClient = gitLabRestApiClient;
            this.logger = logger;
            this.gitlabActivities = new List<GitLabActivity>();
        }

        public List<GitLabActivity> GetTodayActivities()
        {
            if (this.stopFetchingDueToFailedRequests)
            {
                return this.gitlabActivities;
            }

            this.gitlabActivities.Clear();
            try
            {
                if (this.userId == null)
                {
                    var user = this.gitLabRestApiClient.GetCurrentUser();
                    this.userId = new GitLabUserId(user.Id);
                    this.userEmail = user.Email;
                }

                var events = this.gitLabRestApiClient.GetUserEvents(new GitLabUserId(this.userId.Id), DateOnly.FromDateTime(DateTime.Today));

                foreach (var gitlabEvent in events)
                {
                    // TODO: store GitLabData and only add new items to it instead of repulling everything from scratch
                    var gitLabActivity = this.MapGitLabEventToGitLabActivity(gitlabEvent);

                    this.gitlabActivities.AddRange(gitLabActivity);
                }
            } catch (Exception e)
            {
                this.logger.LogError(e, "Error while fetching GitLab activities");
                this.stopFetchingDueToFailedRequests = true;
            }


            return this.gitlabActivities;
        }

        private List<GitLabActivity> MapGitLabEventToGitLabActivity(GitLabEvent gitlabEvent)
        {
            if (gitlabEvent == null)
            {
                return null;
            }

            // Handle Push events (commits and branch creation)
            if (gitlabEvent.PushData != null)
            {
                return this.MapPushEvent(gitlabEvent);
            }

            // Handle Merge Request events
            if (gitlabEvent.TargetType == "MergeRequest")
            {
                return this.MapMergeRequestEvent(gitlabEvent);
            }

            // Handle Issue events
            if (gitlabEvent.TargetType == "Issue")
            {
                return this.MapIssueEvent(gitlabEvent);
            }

            // Handle Note/Comment events
            if (gitlabEvent.TargetType == "Note" && gitlabEvent.Note != null)
            {
                return this.MapNoteEvent(gitlabEvent);
            }

            // Handle Wiki Page events
            if (gitlabEvent.TargetType == "WikiPage::Meta")
            {
                return this.MapWikiEvent(gitlabEvent);
            }

            // Handle Milestone events
            if (gitlabEvent.TargetType == "Milestone")
            {
                return this.MapMilestoneEvent(gitlabEvent);
            }

            // Fallback for other event types
            return new List<GitLabActivity>
            {
                new GitLabActivity(gitlabEvent.CreatedAt.DateTime, $"{gitlabEvent.Action} {gitlabEvent.TargetType}: {gitlabEvent.TargetTitle}")
            };
        }

        private List<GitLabActivity> MapPushEvent(GitLabEvent gitlabEvent)
        {
            var project = this.gitLabRestApiClient.GetProject(new GitLabProjectId(gitlabEvent.ProjectId));
            var projectName = project.NameWithNamespace;
            var branchName = gitlabEvent.PushData.Ref;

            // Check if this is a new branch creation (action_name is "pushed new" and push_data.action is "created")
            if (gitlabEvent.PushData.Action == "created" && gitlabEvent.PushData.RefType == "branch")
            {
                return new List<GitLabActivity>
                {
                    new GitLabActivity(gitlabEvent.CreatedAt.DateTime, $"Created new branch '{branchName}' in Repository: {projectName}")
                };
            }

            // Check if this is a tag creation
            if (gitlabEvent.PushData.Action == "created" && gitlabEvent.PushData.RefType == "tag")
            {
                return new List<GitLabActivity>
                {
                    new GitLabActivity(gitlabEvent.CreatedAt.DateTime, $"Created new tag '{branchName}' in Repository: {projectName}")
                };
            }

            // Check if this is a branch/tag deletion
            if (gitlabEvent.PushData.Action == "removed")
            {
                var refType = gitlabEvent.PushData.RefType;
                return new List<GitLabActivity>
                {
                    new GitLabActivity(gitlabEvent.CreatedAt.DateTime, $"Deleted {refType} '{branchName}' from Repository: {projectName}")
                };
            }

            // Regular commit push
            var commits = this.gitLabRestApiClient.GetCommits(new GitLabProjectId(gitlabEvent.ProjectId), new GitLabRefName(branchName), DateOnly.FromDateTime(DateTime.Today))
                .Where(c => c.AuthorEmail == this.userEmail);

            var gitLabActivities = new List<GitLabActivity>();
            foreach (var commit in commits)
            {
                gitlabActivities.Add(new GitLabActivity(commit.CreatedAt.DateTime, $"Commit to Repository: {projectName}, branch: {branchName}, Title: {commit.Title}"));
            }

            return gitlabActivities;
        }

        private List<GitLabActivity> MapMergeRequestEvent(GitLabEvent gitlabEvent)
        {
            var description = gitlabEvent.Action switch
            {
                "opened" => $"Opened Merge Request: {gitlabEvent.TargetTitle}",
                "closed" => $"Closed Merge Request: {gitlabEvent.TargetTitle}",
                "merged" => $"Merged Merge Request: {gitlabEvent.TargetTitle}",
                "approved" => $"Approved Merge Request: {gitlabEvent.TargetTitle}",
                "unapproved" => $"Removed approval from Merge Request: {gitlabEvent.TargetTitle}",
                "updated" => $"Updated Merge Request: {gitlabEvent.TargetTitle}",
                "reopened" => $"Reopened Merge Request: {gitlabEvent.TargetTitle}",
                _ => $"{gitlabEvent.Action} Merge Request: {gitlabEvent.TargetTitle}"
            };

            return new List<GitLabActivity>
            {
                new GitLabActivity(gitlabEvent.CreatedAt.DateTime, description)
            };
        }

        private List<GitLabActivity> MapIssueEvent(GitLabEvent gitlabEvent)
        {
            var description = gitlabEvent.Action switch
            {
                "opened" => $"Opened Issue: {gitlabEvent.TargetTitle}",
                "closed" => $"Closed Issue: {gitlabEvent.TargetTitle}",
                "reopened" => $"Reopened Issue: {gitlabEvent.TargetTitle}",
                "updated" => $"Updated Issue: {gitlabEvent.TargetTitle}",
                "commented on" => $"Commented on Issue: {gitlabEvent.TargetTitle}",
                _ => $"{gitlabEvent.Action} Issue: {gitlabEvent.TargetTitle}"
            };

            return new List<GitLabActivity>
            {
                new GitLabActivity(gitlabEvent.CreatedAt.DateTime, description)
            };
        }

        private List<GitLabActivity> MapNoteEvent(GitLabEvent gitlabEvent)
        {
            var noteType = gitlabEvent.Note.NoteableType;
            var commentPreview = gitlabEvent.Note.Body.Length > 50
                ? gitlabEvent.Note.Body.Substring(0, 50) + "..."
                : gitlabEvent.Note.Body;

            var description = noteType switch
            {
                "MergeRequest" => $"Commented on Merge Request '{gitlabEvent.TargetTitle}': {commentPreview}",
                "Issue" => $"Commented on Issue '{gitlabEvent.TargetTitle}': {commentPreview}",
                "Commit" => $"Commented on Commit '{gitlabEvent.TargetTitle}': {commentPreview}",
                "Snippet" => $"Commented on Snippet '{gitlabEvent.TargetTitle}': {commentPreview}",
                _ => $"Commented on {noteType} '{gitlabEvent.TargetTitle}': {commentPreview}"
            };

            return new List<GitLabActivity>
            {
                new GitLabActivity(gitlabEvent.CreatedAt.DateTime, description)
            };
        }

        private List<GitLabActivity> MapWikiEvent(GitLabEvent gitlabEvent)
        {
            var description = gitlabEvent.Action switch
            {
                "created" => $"Created Wiki Page: {gitlabEvent.TargetTitle}",
                "updated" => $"Updated Wiki Page: {gitlabEvent.TargetTitle}",
                "destroyed" => $"Deleted Wiki Page: {gitlabEvent.TargetTitle}",
                _ => $"{gitlabEvent.Action} Wiki Page: {gitlabEvent.TargetTitle}"
            };

            return new List<GitLabActivity>
            {
                new GitLabActivity(gitlabEvent.CreatedAt.DateTime, description)
            };
        }

        private List<GitLabActivity> MapMilestoneEvent(GitLabEvent gitlabEvent)
        {
            var description = gitlabEvent.Action switch
            {
                "created" => $"Created Milestone: {gitlabEvent.TargetTitle}",
                "updated" => $"Updated Milestone: {gitlabEvent.TargetTitle}",
                "closed" => $"Closed Milestone: {gitlabEvent.TargetTitle}",
                "reopened" => $"Reopened Milestone: {gitlabEvent.TargetTitle}",
                "destroyed" => $"Deleted Milestone: {gitlabEvent.TargetTitle}",
                _ => $"{gitlabEvent.Action} Milestone: {gitlabEvent.TargetTitle}"
            };

            return new List<GitLabActivity>
            {
                new GitLabActivity(gitlabEvent.CreatedAt.DateTime, description)
            };
        }

        //TODO: Add tests for duplicated events
    }
}
