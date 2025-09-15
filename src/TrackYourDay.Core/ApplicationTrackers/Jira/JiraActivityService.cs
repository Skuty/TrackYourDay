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

                //TODO Important: Expand history of issues and get real Jira Activities for issue

                return issues.Select(issue => new JiraActivity(
                    issue.Fields.Updated.LocalDateTime, 
                    $"Jira Issue Updated - {issue.Key}: {issue.Fields.Summary} | Updated: {issue.Fields.Updated.LocalDateTime:yyyy-MM-dd HH:mm} | Issue ID: {issue.Id}"
                )).ToList();
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error while fetching Jira activities");
                this.stopFetchingDueToFailedRequests = true;
                return new List<JiraActivity>();
            }
        }
    }
}