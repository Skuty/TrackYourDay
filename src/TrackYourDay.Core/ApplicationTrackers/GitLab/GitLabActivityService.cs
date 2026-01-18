using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    /// <summary>
    /// Represents a GitLab activity event with deterministic identifier.
    /// </summary>
    public record class GitLabActivity
    {
        public required string UpstreamId { get; init; }
        public required DateTime OccuranceDate { get; init; }
        public required string Description { get; init; }
        
        /// <summary>
        /// Deterministic GUID based on UpstreamId for deduplication.
        /// </summary>
        public Guid Guid => GenerateDeterministicGuid(UpstreamId);

        private static Guid GenerateDeterministicGuid(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes);
        }
    }

    public class GitLabActivityService : IGitLabActivityService
    {
        private readonly IGitLabRestApiClient _gitLabRestApiClient;
        private readonly ILogger<GitLabActivityService> _logger;

        public GitLabActivityService(IGitLabRestApiClient gitLabRestApiClient, ILogger<GitLabActivityService> logger)
        {
            _gitLabRestApiClient = gitLabRestApiClient;
            _logger = logger;
        }

        public async Task<List<GitLabActivity>> GetActivitiesUpdatedAfter(DateTime startDate, CancellationToken cancellationToken = default)
        {
            var activities = new List<GitLabActivity>();

            var user = await _gitLabRestApiClient.GetCurrentUser().ConfigureAwait(false);
            var userId = new GitLabUserId(user.Id);
            var userEmail = user.Email;

            var events = await _gitLabRestApiClient.GetUserEvents(userId, DateOnly.FromDateTime(startDate)).ConfigureAwait(false);

            foreach (var gitlabEvent in events)
            {
                var eventActivities = await MapGitLabEventToGitLabActivityAsync(gitlabEvent, userEmail, cancellationToken).ConfigureAwait(false);
                if (eventActivities != null)
                {
                    activities.AddRange(eventActivities);
                }
            }

            return activities;
        }

        public async Task<bool> CheckConnection()
        {
            try
            {
                var user = await _gitLabRestApiClient.GetCurrentUser().ConfigureAwait(false);
                return user != null && user.Id > 0 && user.Username != "Not recognized";
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while checking GitLab connection");
                return false;
            }
        }

        private async Task<List<GitLabActivity>?> MapGitLabEventToGitLabActivityAsync(GitLabEvent gitlabEvent, string userEmail, CancellationToken cancellationToken)
        {
            if (gitlabEvent == null)
            {
                return null;
            }

            // Handle Push events (commits and branch creation)
            if (gitlabEvent.PushData != null)
            {
                return await MapPushEventAsync(gitlabEvent, userEmail, cancellationToken).ConfigureAwait(false);
            }

            // Handle Merge Request events
            if (gitlabEvent.TargetType == "MergeRequest")
            {
                return MapMergeRequestEvent(gitlabEvent);
            }

            // Handle Issue events
            if (gitlabEvent.TargetType == "Issue")
            {
                return MapIssueEvent(gitlabEvent);
            }

            // Handle Note/Comment events
            if (gitlabEvent.TargetType == "Note" && gitlabEvent.Note != null)
            {
                return MapNoteEvent(gitlabEvent);
            }

            // Handle Wiki Page events
            if (gitlabEvent.TargetType == "WikiPage::Meta")
            {
                return MapWikiEvent(gitlabEvent);
            }

            // Handle Milestone events
            if (gitlabEvent.TargetType == "Milestone")
            {
                return MapMilestoneEvent(gitlabEvent);
            }

            // Fallback for other event types
            var upstreamId = $"gitlab-event-{gitlabEvent.ProjectId}-{gitlabEvent.TargetType}-{gitlabEvent.CreatedAt:O}";
            return
            [
                new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = $"{gitlabEvent.Action} {gitlabEvent.TargetType}: {gitlabEvent.TargetTitle}"
                }
            ];
        }

        private async Task<List<GitLabActivity>> MapPushEventAsync(GitLabEvent gitlabEvent, string userEmail, CancellationToken cancellationToken)
        {
            var project = await _gitLabRestApiClient.GetProject(new GitLabProjectId(gitlabEvent.ProjectId)).ConfigureAwait(false);
            var projectName = project.NameWithNamespace;
            var branchName = gitlabEvent.PushData.Ref;

            // Check if this is a new branch creation
            if (gitlabEvent.PushData.Action == "created" && gitlabEvent.PushData.RefType == "branch")
            {
                var upstreamId = $"gitlab-branch-created-{gitlabEvent.ProjectId}-{branchName}-{gitlabEvent.CreatedAt:O}";
                return
                [
                    new GitLabActivity
                    {
                        UpstreamId = upstreamId,
                        OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                        Description = $"Created new branch '{branchName}' in Repository: {projectName}"
                    }
                ];
            }

            // Check if this is a tag creation
            if (gitlabEvent.PushData.Action == "created" && gitlabEvent.PushData.RefType == "tag")
            {
                var upstreamId = $"gitlab-tag-created-{gitlabEvent.ProjectId}-{branchName}-{gitlabEvent.CreatedAt:O}";
                return
                [
                    new GitLabActivity
                    {
                        UpstreamId = upstreamId,
                        OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                        Description = $"Created new tag '{branchName}' in Repository: {projectName}"
                    }
                ];
            }

            // Check if this is a branch/tag deletion
            if (gitlabEvent.PushData.Action == "removed")
            {
                var refType = gitlabEvent.PushData.RefType;
                var upstreamId = $"gitlab-{refType}-removed-{gitlabEvent.ProjectId}-{branchName}-{gitlabEvent.CreatedAt:O}";
                return
                [
                    new GitLabActivity
                    {
                        UpstreamId = upstreamId,
                        OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                        Description = $"Deleted {refType} '{branchName}' from Repository: {projectName}"
                    }
                ];
            }

            // Regular commit push
            List<GitLabCommit> commits;
            var commitFrom = gitlabEvent.PushData.CommitFrom;
            var commitTo = gitlabEvent.PushData.CommitTo;

            if (!string.IsNullOrEmpty(commitFrom) && !string.IsNullOrEmpty(commitTo))
            {
                var allCommits = await _gitLabRestApiClient.GetCommitsByShaRange(
                    new GitLabProjectId(gitlabEvent.ProjectId), 
                    commitFrom, 
                    commitTo).ConfigureAwait(false);
                commits = allCommits.Where(c => c.AuthorEmail == userEmail).ToList();
            }
            else
            {
                var allCommits = await _gitLabRestApiClient.GetCommits(
                    new GitLabProjectId(gitlabEvent.ProjectId), 
                    new GitLabRefName(branchName), 
                    DateOnly.FromDateTime(DateTime.Today)).ConfigureAwait(false);
                commits = allCommits.Where(c => c.AuthorEmail == userEmail).ToList();
            }

            var gitLabActivities = new List<GitLabActivity>();

            foreach (var commit in commits)
            {
                var upstreamId = $"gitlab-commit-{gitlabEvent.ProjectId}-{commit.Id}";
                gitLabActivities.Add(new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccuranceDate = commit.CommittedDate.DateTime,
                    Description = $"Commit to Repository: {projectName}, branch: {branchName}, Title: {commit.Title}"
                });
            }

            return gitLabActivities;
        }

        private static List<GitLabActivity> MapMergeRequestEvent(GitLabEvent gitlabEvent)
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

            var upstreamId = $"gitlab-mr-{gitlabEvent.ProjectId}-{gitlabEvent.Id}-{gitlabEvent.Action}";
            return
            [
                new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = description
                }
            ];
        }

        private static List<GitLabActivity> MapIssueEvent(GitLabEvent gitlabEvent)
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

            var upstreamId = $"gitlab-issue-{gitlabEvent.ProjectId}-{gitlabEvent.Id}-{gitlabEvent.Action}";
            return
            [
                new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = description
                }
            ];
        }

        private static List<GitLabActivity> MapNoteEvent(GitLabEvent gitlabEvent)
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

            var upstreamId = $"gitlab-note-{gitlabEvent.ProjectId}-{gitlabEvent.Note.Id}-{gitlabEvent.CreatedAt:O}";
            return
            [
                new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = description
                }
            ];
        }

        private static List<GitLabActivity> MapWikiEvent(GitLabEvent gitlabEvent)
        {
            var description = gitlabEvent.Action switch
            {
                "created" => $"Created Wiki Page: {gitlabEvent.TargetTitle}",
                "updated" => $"Updated Wiki Page: {gitlabEvent.TargetTitle}",
                "destroyed" => $"Deleted Wiki Page: {gitlabEvent.TargetTitle}",
                _ => $"{gitlabEvent.Action} Wiki Page: {gitlabEvent.TargetTitle}"
            };

            var upstreamId = $"gitlab-wiki-{gitlabEvent.ProjectId}-{gitlabEvent.Id}-{gitlabEvent.Action}";
            return
            [
                new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = description
                }
            ];
        }

        private static List<GitLabActivity> MapMilestoneEvent(GitLabEvent gitlabEvent)
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

            var upstreamId = $"gitlab-milestone-{gitlabEvent.ProjectId}-{gitlabEvent.Id}-{gitlabEvent.Action}";
            return
            [
                new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccuranceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = description
                }
            ];
        }
    }
}
