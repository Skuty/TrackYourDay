using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public record class JiraActivity(DateTime OccurrenceDate, string Description)
    {
        public Guid Guid { get; init; } = Guid.NewGuid();
    }

    public interface IJiraActivityService
    {
        Task<List<JiraActivity>> GetActivitiesUpdatedAfter(DateTime updateDate);
        Task<bool> CheckConnection();
    }

    public class JiraActivityService : IJiraActivityService
    {
        private readonly IJiraRestApiClient jiraRestApiClient;
        private readonly ILogger<JiraActivityService> logger;
        private JiraUser currentUser;
        private bool stopFetchingDueToFailedRequests = false;

        public JiraActivityService(IJiraRestApiClient jiraRestApiClient, ILogger<JiraActivityService> logger)
        {
            this.jiraRestApiClient = jiraRestApiClient;
            this.logger = logger;
        }

        public async Task<List<JiraActivity>> GetActivitiesUpdatedAfter(DateTime updateDate)
        {
            if (this.stopFetchingDueToFailedRequests)
            {
                return new List<JiraActivity>();
            }

            try
            {
                if (this.currentUser == null)
                {
                    this.currentUser = await this.jiraRestApiClient.GetCurrentUser();
                }

                var issues = await this.jiraRestApiClient.GetUserIssues(this.currentUser, updateDate);

                var activities = new List<JiraActivity>();

                foreach (var issue in issues)
                {
                    // Check if this issue was created by the current user in the date range
                    if (issue.Fields.Created.HasValue &&
                        issue.Fields.Created.Value.LocalDateTime >= updateDate &&
                        issue.Fields.Creator?.DisplayName == this.currentUser.DisplayName)
                    {
                        activities.Add(CreateIssueCreationActivity(issue));
                    }

                    // Extract activities from changelog (only for current user)
                    if (issue.Changelog?.Histories != null)
                    {
                        var changelogActivities = this.MapChangelogToActivities(issue);
                        activities.AddRange(changelogActivities);
                    }

                    // Fetch and process worklogs for this issue
                    try
                    {
                        var worklogs = await this.jiraRestApiClient.GetIssueWorklogs(issue.Key, updateDate);
                        var worklogActivities = worklogs
                            .Where(w => w.Author?.DisplayName == this.currentUser.DisplayName)
                            .Select(w => CreateWorklogActivity(issue, w))
                            .ToList();
                        activities.AddRange(worklogActivities);
                    }
                    catch (Exception ex)
                    {
                        this.logger?.LogWarning(ex, $"Failed to fetch worklogs for issue {issue.Key}");
                    }
                }

                return activities.OrderBy(a => a.OccurrenceDate).ToList();
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error while fetching Jira activities");
                this.stopFetchingDueToFailedRequests = true;
                return new List<JiraActivity>();
            }
        }

        public async Task<bool> CheckConnection()
        {
            try
            {
                var user = await this.jiraRestApiClient.GetCurrentUser();
                return user != null && !string.IsNullOrEmpty(user.DisplayName) && user.DisplayName != "Not recognized";
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error while checking Jira connection");
                return false;
            }
        }

        private JiraActivity CreateIssueCreationActivity(JiraIssueResponse issue)
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

            return new JiraActivity(issue.Fields.Created!.Value.LocalDateTime, description);
        }

        private JiraActivity CreateWorklogActivity(JiraIssueResponse issue, JiraWorklogResponse worklog)
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

            return new JiraActivity(worklog.Started.LocalDateTime, description);
        }

        private List<JiraActivity> MapChangelogToActivities(JiraIssueResponse issue)
        {
            var activities = new List<JiraActivity>();

            if (issue.Changelog?.Histories == null)
            {
                return activities;
            }

            foreach (var history in issue.Changelog.Histories)
            {
                // Only include changes made by the current user
                if (history.Author?.DisplayName != this.currentUser.DisplayName)
                {
                    continue;
                }

                if (history.Items == null || !history.Items.Any())
                {
                    continue;
                }

                foreach (var item in history.Items)
                {
                    var activity = this.MapChangeItemToActivity(issue, history, item);
                    if (activity != null)
                    {
                        activities.Add(activity);
                    }
                }
            }

            return activities;
        }

        private JiraActivity? MapChangeItemToActivity(JiraIssueResponse issue, JiraHistoryResponse history, JiraChangeItemResponse item)
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

            return new JiraActivity(activityDate, description);
        }

        private string MapAssigneeChange(string issueKey, string? summary, string? from, string? to)
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

        private string MapResolutionChange(string issueKey, string? summary, string? from, string? to)
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

        private string MapFixVersionChange(string issueKey, string? summary, string? from, string? to)
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

        private string MapComponentChange(string issueKey, string? summary, string? from, string? to)
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

        private string MapSprintChange(string issueKey, string? summary, string? from, string? to)
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

        private string MapParentChange(string issueKey, string? summary, string? from, string? to)
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