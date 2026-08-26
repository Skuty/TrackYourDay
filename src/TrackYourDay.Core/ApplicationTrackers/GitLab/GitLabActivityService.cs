using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using TrackYourDay.Core.ApplicationTrackers.Shared;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    /// <summary>
    /// Represents a GitLab activity event with deterministic identifier.
    /// </summary>
    public record class GitLabActivity : IHasDeterministicGuid, IHasOccurrenceDate
    {
        public required string UpstreamId { get; init; }
        public required DateTime OccurrenceDate { get; init; }
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

            var events = await _gitLabRestApiClient.GetUserEvents(userId, DateOnly.FromDateTime(startDate)).ConfigureAwait(false);

            foreach (var gitlabEvent in events)
            {
                var eventActivities = await MapGitLabEventToGitLabActivityAsync(gitlabEvent, user, startDate, cancellationToken).ConfigureAwait(false);
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
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while checking GitLab connection: {Message}", ex.Message);
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "GitLab connection timed out");
                return false;
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "Invalid GitLab API URL format");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while checking GitLab connection");
                return false;
            }
        }

        private async Task<List<GitLabActivity>?> MapGitLabEventToGitLabActivityAsync(
            GitLabEvent gitlabEvent,
            GitLabUser user,
            DateTime startDate,
            CancellationToken cancellationToken)
        {
            if (gitlabEvent == null)
            {
                return null;
            }

            // Handle Push events (commits and branch creation)
            if (gitlabEvent.PushData != null)
            {
                return await MapPushEventAsync(gitlabEvent, user, startDate, cancellationToken).ConfigureAwait(false);
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
                    OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = $"{gitlabEvent.Action} {gitlabEvent.TargetType}: {gitlabEvent.TargetTitle}"
                }
            ];
        }

        private async Task<List<GitLabActivity>> MapPushEventAsync(
            GitLabEvent gitlabEvent,
            GitLabUser user,
            DateTime startDate,
            CancellationToken cancellationToken)
        {
            if (gitlabEvent.PushData is null)
            {
                return [];
            }

            var pushData = gitlabEvent.PushData;
            var project = await _gitLabRestApiClient.GetProject(new GitLabProjectId(gitlabEvent.ProjectId)).ConfigureAwait(false);
            var projectName = project.NameWithNamespace;
            var branchName = pushData.Ref;

            // Check if this is a new branch creation
            if (pushData.Action == "created" && pushData.RefType == "branch")
            {
                var upstreamId = $"gitlab-branch-created-{gitlabEvent.ProjectId}-{branchName}-{gitlabEvent.CreatedAt:O}";
                return
                [
                    new GitLabActivity
                    {
                        UpstreamId = upstreamId,
                        OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
                        Description = $"Created new branch '{branchName}' in Repository: {projectName}"
                    }
                ];
            }

            // Check if this is a tag creation
            if (pushData.Action == "created" && pushData.RefType == "tag")
            {
                var upstreamId = $"gitlab-tag-created-{gitlabEvent.ProjectId}-{branchName}-{gitlabEvent.CreatedAt:O}";
                return
                [
                    new GitLabActivity
                    {
                        UpstreamId = upstreamId,
                        OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
                        Description = $"Created new tag '{branchName}' in Repository: {projectName}"
                    }
                ];
            }

            // Check if this is a branch/tag deletion
            if (pushData.Action == "removed")
            {
                var refType = pushData.RefType;
                var upstreamId = $"gitlab-{refType}-removed-{gitlabEvent.ProjectId}-{branchName}-{gitlabEvent.CreatedAt:O}";
                return
                [
                    new GitLabActivity
                    {
                        UpstreamId = upstreamId,
                        OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
                        Description = $"Deleted {refType} '{branchName}' from Repository: {projectName}"
                    }
                ];
            }

            // Regular commit push
            List<GitLabCommit> commits;
            var commitFrom = pushData.CommitFrom;
            var commitTo = pushData.CommitTo;

            if (!string.IsNullOrEmpty(commitFrom) && !string.IsNullOrEmpty(commitTo))
            {
                try
                {
                    commits = await _gitLabRestApiClient.GetCommitsByShaRange(
                        new GitLabProjectId(gitlabEvent.ProjectId),
                        commitFrom,
                        commitTo).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to fetch commits by SHA range for project {ProjectId}, branch {BranchName}. Falling back to branch query.",
                        gitlabEvent.ProjectId,
                        branchName);
                    commits = [];
                }
            }
            else
            {
                _logger.LogWarning(
                    "Push event {EventId} for project {ProjectId} has incomplete SHA range (from={CommitFrom}, to={CommitTo}). Falling back to branch query.",
                    gitlabEvent.Id,
                    gitlabEvent.ProjectId,
                    commitFrom,
                    commitTo);
                commits = [];
            }

            if (commits.Count == 0 && pushData.CommitCount > 0)
            {
                commits = await _gitLabRestApiClient.GetCommits(
                    new GitLabProjectId(gitlabEvent.ProjectId),
                    new GitLabRefName(branchName),
                    DateOnly.FromDateTime(startDate)).ConfigureAwait(false);
            }

            var relevantCommits = SelectCommitsForCurrentUser(commits, user);
            var gitLabActivities = new List<GitLabActivity>();

            foreach (var commit in relevantCommits)
            {
                var upstreamId = $"gitlab-commit-{gitlabEvent.ProjectId}-{commit.Id}";
                gitLabActivities.Add(new GitLabActivity
                {
                    UpstreamId = upstreamId,
                    OccurrenceDate = commit.CommittedDate.DateTime,
                    Description = $"Commit to Repository: {projectName}, branch: {branchName}, Title: {commit.Title}"
                });
            }

            return gitLabActivities;
        }

        private static List<GitLabCommit> SelectCommitsForCurrentUser(IEnumerable<GitLabCommit> commits, GitLabUser user)
        {
            var commitList = commits.ToList();
            if (commitList.Count == 0)
            {
                return commitList;
            }

            var hasUserEmail = !string.IsNullOrWhiteSpace(user.Email);
            var hasUserName = !string.IsNullOrWhiteSpace(user.Name);
            if (!hasUserEmail && !hasUserName)
            {
                return commitList;
            }

            var matchingCommits = commitList.Where(commit =>
            {
                var emailMatches = hasUserEmail &&
                                   (string.Equals(commit.AuthorEmail, user.Email, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(commit.CommitterEmail, user.Email, StringComparison.OrdinalIgnoreCase));
                var nameMatches = hasUserName &&
                                  (string.Equals(commit.AuthorName, user.Name, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(commit.CommitterName, user.Name, StringComparison.OrdinalIgnoreCase));

                return emailMatches || nameMatches;
            }).ToList();

            return matchingCommits.Count > 0 ? matchingCommits : commitList;
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
                    OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
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
                    OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
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
                    OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
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
                    OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
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
                    OccurrenceDate = gitlabEvent.CreatedAt.DateTime,
                    Description = description
                }
            ];
        }
    }
}

/*
UPDATE historical_data
SET DataJson = json_remove(
                json_set(
                  DataJson,
                  '$.OccurrenceDate',
                  json_extract(DataJson, '$.OccuranceDate')
                ),
                '$.OccuranceDate'
              )
WHERE TypeName = 'GitLabActivity'
  AND json_type(DataJson, '$.OccuranceDate') IS NOT NULL;
*/