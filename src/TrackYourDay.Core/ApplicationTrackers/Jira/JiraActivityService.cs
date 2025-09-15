using Microsoft.Extensions.Logging;
using System.Linq;

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

                //TODO Important: Expand history of issues and get real Jira Activities for issue

                return issues.Select(issue => new JiraActivity(
                    issue.Fields.Updated.LocalDateTime, 
                    BuildDetailedActivityDescription(issue)
                )).ToList();
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error while fetching Jira activities");
                this.stopFetchingDueToFailedRequests = true;
                return new List<JiraActivity>();
            }
        }

        private string BuildDetailedActivityDescription(JiraIssueResponse issue)
        {
            var parts = new List<string>();
            
            // Basic issue information
            parts.Add($"Jira Activity - {issue.Key}: {issue.Fields.Summary ?? "No Summary"}");
            
            // Issue type and project information
            if (issue.Fields.IssueType != null)
            {
                var issueTypeInfo = issue.Fields.IssueType.Subtask ? "Subtask" : "Issue";
                parts.Add($"Type: {issue.Fields.IssueType.Name} ({issueTypeInfo})");
            }
            
            if (issue.Fields.Project != null)
            {
                parts.Add($"Project: {issue.Fields.Project.Name} ({issue.Fields.Project.Key})");
            }
            
            // Status information
            if (issue.Fields.Status != null)
            {
                var statusInfo = issue.Fields.Status.Name;
                if (issue.Fields.Status.StatusCategory?.Name != null)
                {
                    statusInfo += $" ({issue.Fields.Status.StatusCategory.Name})";
                }
                parts.Add($"Status: {statusInfo}");
            }
            
            // Priority information
            if (issue.Fields.Priority != null)
            {
                parts.Add($"Priority: {issue.Fields.Priority.Name}");
            }
            
            // Assignment information
            if (issue.Fields.Assignee != null)
            {
                parts.Add($"Assignee: {issue.Fields.Assignee.DisplayName}");
            }
            else
            {
                parts.Add("Assignee: Unassigned");
            }
            
            if (issue.Fields.Reporter != null)
            {
                parts.Add($"Reporter: {issue.Fields.Reporter.DisplayName}");
            }
            
            // Components information
            if (issue.Fields.Components != null && issue.Fields.Components.Any())
            {
                var componentNames = string.Join(", ", issue.Fields.Components.Select(c => c.Name));
                parts.Add($"Components: {componentNames}");
            }
            
            // Labels information
            if (issue.Fields.Labels != null && issue.Fields.Labels.Any())
            {
                var labelNames = string.Join(", ", issue.Fields.Labels);
                parts.Add($"Labels: {labelNames}");
            }
            
            // Timing information
            parts.Add($"Updated: {issue.Fields.Updated.LocalDateTime:yyyy-MM-dd HH:mm:ss}");
            
            if (issue.Fields.Created.HasValue)
            {
                var createdDate = issue.Fields.Created.Value.LocalDateTime;
                var daysSinceCreated = (issue.Fields.Updated.LocalDateTime - createdDate).Days;
                parts.Add($"Created: {createdDate:yyyy-MM-dd HH:mm:ss} ({daysSinceCreated} days ago)");
            }
            
            // Description preview (first 100 characters)
            if (!string.IsNullOrWhiteSpace(issue.Fields.Description))
            {
                var descriptionPreview = issue.Fields.Description.Length > 100 
                    ? issue.Fields.Description.Substring(0, 100) + "..." 
                    : issue.Fields.Description;
                parts.Add($"Description: {descriptionPreview.Replace("\n", " ").Replace("\r", "")}");
            }
            
            // Issue ID for reference
            parts.Add($"Issue ID: {issue.Id}");
            
            return string.Join(" | ", parts);
        }
    }
}