using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public record class JiraActivity(DateTime OccurrenceDate, string Description);

    public class JiraActivityService
    {
        private readonly IJiraRestApiClient jiraRestApiClient;
        private readonly ILogger<JiraActivityService> logger;
        private JiraUser currentUser;

        public JiraActivityService(IJiraRestApiClient jiraRestApiClient, ILogger<JiraActivityService> logger)
        {
            this.jiraRestApiClient = jiraRestApiClient;
            this.logger = logger;
        }

        public List<JiraActivity> GetTodayActivities()
        {
            try
            {
                if (this.currentUser == null)
                {
                    this.currentUser = this.jiraRestApiClient.GetCurrentUser();
                }

                var issues = this.jiraRestApiClient.GetUserIssues(this.currentUser.AccountId, DateTime.Today);

                return issues.Select(issue => new JiraActivity(issue.Updated, $"Issue {issue.Key}: {issue.Summary}")).ToList();
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error while fetching Jira activities");
                return new List<JiraActivity>();
            }
        }
    }
}