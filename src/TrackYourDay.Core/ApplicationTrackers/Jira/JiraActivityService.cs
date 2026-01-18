using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using TrackYourDay.Core.ApplicationTrackers.Shared;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    /// <summary>
    /// Represents a Jira activity event with deterministic identifier.
    /// </summary>
    public record class JiraActivity : IHasDeterministicGuid, IHasOccurrenceDate
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

    public interface IJiraActivityService
    {
        Task<List<JiraActivity>> GetActivitiesUpdatedAfter(DateTime updateDate);
        Task<bool> CheckConnection();
    }

    public class JiraActivityService : IJiraActivityService
    {
        private readonly IJiraRestApiClient _jiraRestApiClient;
        private readonly ILogger<JiraActivityService> _logger;

        public JiraActivityService(IJiraRestApiClient jiraRestApiClient, ILogger<JiraActivityService> logger)
        {
            _jiraRestApiClient = jiraRestApiClient;
            _logger = logger;
        }

        public async Task<List<JiraActivity>> GetActivitiesUpdatedAfter(DateTime updateDate)
        {
            var currentUser = await _jiraRestApiClient.GetCurrentUser().ConfigureAwait(false);
            var issues = await _jiraRestApiClient.GetUserIssues(currentUser, updateDate).ConfigureAwait(false);

            var activities = new List<JiraActivity>();

            foreach (var issue in issues)
            {
                // Check if this issue was created by the current user in the date range
                if (issue.Fields.Created.HasValue &&
                    issue.Fields.Created.Value.LocalDateTime >= updateDate &&
                    issue.Fields.Creator?.DisplayName == currentUser.DisplayName)
                {
                    activities.Add(CreateIssueCreationActivity(issue));
                }

                // Extract activities from changelog (only for current user)
                if (issue.Changelog?.Histories != null)
                {
                    var changelogActivities = MapChangelogToActivities(issue, currentUser);
                    activities.AddRange(changelogActivities);
                }

                // Fetch and process worklogs for this issue
                try
                {
                    var worklogs = await _jiraRestApiClient.GetIssueWorklogs(issue.Key, updateDate).ConfigureAwait(false);
                    var worklogActivities = worklogs
                        .Where(w => w.Author?.DisplayName == currentUser.DisplayName)
                        .Select(w => CreateWorklogActivity(issue, w))
                        .ToList();
                    activities.AddRange(worklogActivities);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch worklogs for issue {IssueKey}", issue.Key);
                }
            }

            return activities.OrderBy(a => a.OccurrenceDate).ToList();
        }

        public async Task<bool> CheckConnection()
        {
            try
            {
                var user = await _jiraRestApiClient.GetCurrentUser().ConfigureAwait(false);
                return user != null && !string.IsNullOrEmpty(user.DisplayName) && user.DisplayName != "Not recognized";
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while checking Jira connection");
                return false;
            }
        }

        private static JiraActivity CreateIssueCreationActivity(JiraIssueResponse issue)
        {
            var issueType = issue.Fields.IssueType?.Name ?? "Issue";
            var project = issue.Fields.Project?.Key ?? "Unknown";
            var description = $"Created {issueType} {issue.Key} in {project}: {issue.Fields.Summary}";

            // Add parent/epic context if it's a sub-task
            if (issue.Fields.IssueType?.IsSubtask == true && issue.Fields.Parent != null)
            {
                var parentType = issue.Fields.Parent.Fields?.IssueType?.Name ?? "Issue";
                description += $" (sub-task of {parentType} {issue.Fields.Parent.Key})";
            }

            var upstreamId = $"jira-issue-created-{issue.Key}-{issue.Id}";
            return new JiraActivity
            {
                UpstreamId = upstreamId,
                OccurrenceDate = issue.Fields.Created!.Value.LocalDateTime,
                Description = description
            };
        }

        private static JiraActivity CreateWorklogActivity(JiraIssueResponse issue, JiraWorklogResponse worklog)
        {
            var project = issue.Fields.Project?.Key ?? "Unknown";
            var issueType = issue.Fields.IssueType?.Name ?? "Issue";
            var timeSpent = worklog.TimeSpent ?? $"{worklog.TimeSpentSeconds}s";

            var description = $"Logged {timeSpent} on {issueType} {issue.Key} in {project}: {issue.Fields.Summary}";

            if (!string.IsNullOrEmpty(worklog.Comment))
            {
                var commentPreview = worklog.Comment.Length > 50
                    ? worklog.Comment.Substring(0, 50) + "..."
                    : worklog.Comment;
                description += $" - \"{commentPreview}\"";
            }

            var upstreamId = $"jira-worklog-{issue.Key}-{worklog.Id}";
            return new JiraActivity
            {
                UpstreamId = upstreamId,
                OccurrenceDate = worklog.Started.LocalDateTime,
                Description = description
            };
        }

        private static List<JiraActivity> MapChangelogToActivities(JiraIssueResponse issue, JiraUser currentUser)
        {
            var activities = new List<JiraActivity>();

            if (issue.Changelog?.Histories == null)
            {
                return activities;
            }

            foreach (var history in issue.Changelog.Histories)
            {
                // Only include changes made by the current user
                if (history.Author?.DisplayName != currentUser.DisplayName)
                {
                    continue;
                }

                if (history.Items == null || !history.Items.Any())
                {
                    continue;
                }

                foreach (var item in history.Items)
                {
                    var activity = MapChangeItemToActivity(issue, history, item);
                    if (activity != null)
                    {
                        activities.Add(activity);
                    }
                }
            }

            return activities;
        }

        private static JiraActivity? MapChangeItemToActivity(JiraIssueResponse issue, JiraHistoryResponse history, JiraChangeItemResponse item)
        {
            var issueKey = issue.Key;
            var issueSummary = issue.Fields.Summary;
            var activityDate = history.Created.LocalDateTime;
            var project = issue.Fields.Project?.Key ?? "Unknown";
            var issueType = issue.Fields.IssueType?.Name ?? "Issue";

            // Create issue identifier with type and project context
            var issueIdentifier = $"{issueType} {issueKey} in {project}";

            var description = item.Field?.ToLower() switch
            {
                "status" => $"Changed status of {issueIdentifier}: {issueSummary} from '{item.FromString}' to '{item.ToValue}'",
                "assignee" => MapAssigneeChange(issueIdentifier, issueSummary, item.FromString, item.ToValue),
                "resolution" => MapResolutionChange(issueIdentifier, issueSummary, item.FromString, item.ToValue),
                "summary" => $"Updated summary of {issueIdentifier} from '{item.FromString}' to '{item.ToValue}'",
                "description" => $"Updated description of {issueIdentifier}: {issueSummary}",
                "priority" => $"Changed priority of {issueIdentifier}: {issueSummary} from '{item.FromString}' to '{item.ToValue}'",
                "labels" => $"Updated labels of {issueIdentifier}: {issueSummary}",
                "fix version" or "fixversion" => MapFixVersionChange(issueIdentifier, issueSummary, item.FromString, item.ToValue),
                "component" => MapComponentChange(issueIdentifier, issueSummary, item.FromString, item.ToValue),
                "sprint" => MapSprintChange(issueIdentifier, issueSummary, item.FromString, item.ToValue),
                "story points" or "storypoints" => $"Changed story points of {issueIdentifier}: {issueSummary} from '{item.FromString}' to '{item.ToValue}'",
                "comment" => $"Commented on {issueIdentifier}: {issueSummary}",
                "attachment" => $"Added attachment to {issueIdentifier}: {issueSummary}",
                "link" => $"Added link to {issueIdentifier}: {issueSummary}",
                "timeestimate" or "time estimate" => $"Updated time estimate for {issueIdentifier}: {issueSummary}",
                "timespent" or "time spent" => $"Logged work on {issueIdentifier}: {issueSummary}",
                "parent" => MapParentChange(issueIdentifier, issueSummary, item.FromString, item.ToValue),
                _ => $"Updated {item.Field} of {issueIdentifier}: {issueSummary}"
            };

            var upstreamId = $"jira-history-{issueKey}-{history.Id}-{item.Field}";
            return new JiraActivity
            {
                UpstreamId = upstreamId,
                OccurrenceDate = activityDate,
                Description = description
            };
        }

        private static string MapAssigneeChange(string issueKey, string? summary, string? from, string? to)
        {
            if (string.IsNullOrEmpty(from))
            {
                return $"Assigned {issueKey}: {summary} to {to}";
            }
            else if (string.IsNullOrEmpty(to))
            {
                return $"Unassigned {issueKey}: {summary} from {from}";
            }
            else
            {
                return $"Reassigned {issueKey}: {summary} from {from} to {to}";
            }
        }

        private static string MapResolutionChange(string issueKey, string? summary, string? from, string? to)
        {
            if (string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            {
                return $"Resolved {issueKey}: {summary} as '{to}'";
            }
            else if (!string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to))
            {
                return $"Removed resolution from {issueKey}: {summary}";
            }
            else
            {
                return $"Changed resolution of {issueKey}: {summary} from '{from}' to '{to}'";
            }
        }

        private static string MapFixVersionChange(string issueKey, string? summary, string? from, string? to)
        {
            if (string.IsNullOrEmpty(from))
            {
                return $"Added fix version '{to}' to {issueKey}: {summary}";
            }
            else if (string.IsNullOrEmpty(to))
            {
                return $"Removed fix version '{from}' from {issueKey}: {summary}";
            }
            else
            {
                return $"Changed fix version of {issueKey}: {summary} from '{from}' to '{to}'";
            }
        }

        private static string MapComponentChange(string issueKey, string? summary, string? from, string? to)
        {
            if (string.IsNullOrEmpty(from))
            {
                return $"Added component '{to}' to {issueKey}: {summary}";
            }
            else if (string.IsNullOrEmpty(to))
            {
                return $"Removed component '{from}' from {issueKey}: {summary}";
            }
            else
            {
                return $"Changed component of {issueKey}: {summary} from '{from}' to '{to}'";
            }
        }

        private static string MapSprintChange(string issueKey, string? summary, string? from, string? to)
        {
            if (string.IsNullOrEmpty(from))
            {
                return $"Added {issueKey}: {summary} to sprint";
            }
            else if (string.IsNullOrEmpty(to))
            {
                return $"Removed {issueKey}: {summary} from sprint";
            }
            else
            {
                return $"Moved {issueKey}: {summary} to different sprint";
            }
        }

        private static string MapParentChange(string issueKey, string? summary, string? from, string? to)
        {
            if (string.IsNullOrEmpty(from))
            {
                return $"Linked {issueKey}: {summary} to parent issue {to}";
            }
            else if (string.IsNullOrEmpty(to))
            {
                return $"Removed parent link from {issueKey}: {summary}";
            }
            else
            {
                return $"Changed parent of {issueKey}: {summary} from {from} to {to}";
            }
        }
    }
}