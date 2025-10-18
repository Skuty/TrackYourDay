using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public record class JiraActivity(DateTime OccurrenceDate, string Description);

    public interface IJiraActivityService
    {
        List<JiraActivity> GetActivitiesUpdatedAfter(DateTime updateDate);
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

        public List<JiraActivity> GetActivitiesUpdatedAfter(DateTime updateDate)
        {
            if (this.stopFetchingDueToFailedRequests)
            {
                return new List<JiraActivity>();
            }

            try
            {
                if (this.currentUser == null)
                {
                    this.currentUser = this.jiraRestApiClient.GetCurrentUser();
                }

                var issues = this.jiraRestApiClient.GetUserIssues(this.currentUser, updateDate);

                var activities = new List<JiraActivity>();

                foreach (var issue in issues)
                {
                    // Extract activities from changelog
                    if (issue.Changelog?.Histories != null)
                    {
                        var changelogActivities = this.MapChangelogToActivities(issue);
                        activities.AddRange(changelogActivities);
                    }
                    else
                    {
                        // Fallback to simple update notification if no changelog
                        activities.Add(new JiraActivity(
                            issue.Fields.Updated.LocalDateTime,
                            $"Jira Issue Updated - {issue.Key}: {issue.Fields.Summary}"
                        ));
                    }
                }

                return activities;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error while fetching Jira activities");
                this.stopFetchingDueToFailedRequests = true;
                return new List<JiraActivity>();
            }
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
            var author = history.Author?.DisplayName ?? "Unknown";

            var description = item.Field?.ToLower() switch
            {
                "status" => $"Changed status of {issueKey}: {issueSummary} from '{item.FromString}' to '{item.ToValue}'",
                "assignee" => MapAssigneeChange(issueKey, issueSummary, item.FromString, item.ToValue),
                "resolution" => MapResolutionChange(issueKey, issueSummary, item.FromString, item.ToValue),
                "summary" => $"Updated summary of {issueKey} from '{item.FromString}' to '{item.ToValue}'",
                "description" => $"Updated description of {issueKey}: {issueSummary}",
                "priority" => $"Changed priority of {issueKey}: {issueSummary} from '{item.FromString}' to '{item.ToValue}'",
                "labels" => $"Updated labels of {issueKey}: {issueSummary}",
                "fix version" or "fixversion" => MapFixVersionChange(issueKey, issueSummary, item.FromString, item.ToValue),
                "component" => MapComponentChange(issueKey, issueSummary, item.FromString, item.ToValue),
                "sprint" => MapSprintChange(issueKey, issueSummary, item.FromString, item.ToValue),
                "story points" or "storypoints" => $"Changed story points of {issueKey}: {issueSummary} from '{item.FromString}' to '{item.ToValue}'",
                "comment" => $"Commented on {issueKey}: {issueSummary}",
                "attachment" => $"Added attachment to {issueKey}: {issueSummary}",
                "link" => $"Added link to {issueKey}: {issueSummary}",
                "timeestimate" or "time estimate" => $"Updated time estimate for {issueKey}: {issueSummary}",
                "timespent" or "time spent" => $"Logged work on {issueKey}: {issueSummary}",
                _ => $"Updated {item.Field} of {issueKey}: {issueSummary}"
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
    }
}