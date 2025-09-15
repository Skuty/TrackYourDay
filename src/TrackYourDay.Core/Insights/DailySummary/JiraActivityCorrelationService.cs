using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.DailySummary
{
    public interface IJiraActivityCorrelationService
    {
        List<JiraActivityCorrelation> CorrelateActivitiesWithJiraIssues(
            IReadOnlyCollection<EndedActivity> activities,
            List<JiraActivity> jiraActivities);
    }

    public class JiraActivityCorrelationService : IJiraActivityCorrelationService
    {
        private readonly ILogger<JiraActivityCorrelationService> logger;
        private static readonly Regex JiraIssueKeyRegex = new(@"\b[A-Z]+-\d+\b", RegexOptions.Compiled);
        private static readonly Regex JiraUrlRegex = new(@"jira.*?/browse/([A-Z]+-\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public JiraActivityCorrelationService(ILogger<JiraActivityCorrelationService> logger)
        {
            this.logger = logger;
        }

        public List<JiraActivityCorrelation> CorrelateActivitiesWithJiraIssues(
            IReadOnlyCollection<EndedActivity> activities,
            List<JiraActivity> jiraActivities)
        {
            var correlations = new List<JiraActivityCorrelation>();

            foreach (var activity in activities)
            {
                var correlation = AnalyzeActivity(activity, jiraActivities);
                correlations.Add(correlation);
            }

            return correlations;
        }

        private JiraActivityCorrelation AnalyzeActivity(EndedActivity activity, List<JiraActivity> jiraActivities)
        {
            var activityDescription = activity.GetDescription();
            
            // Method 1: Direct Jira issue key detection in window title/description
            var issueKeyFromTitle = ExtractJiraIssueKeyFromText(activityDescription);
            if (!string.IsNullOrEmpty(issueKeyFromTitle))
            {
                return new JiraActivityCorrelation(
                    activity, 
                    issueKeyFromTitle, 
                    CorrelationMethod.WindowTitle, 
                    0.9);
            }

            // Method 2: Jira web interface detection
            var issueKeyFromJiraUrl = ExtractJiraIssueKeyFromJiraUrl(activityDescription);
            if (!string.IsNullOrEmpty(issueKeyFromJiraUrl))
            {
                return new JiraActivityCorrelation(
                    activity, 
                    issueKeyFromJiraUrl, 
                    CorrelationMethod.JiraWebInterface, 
                    0.95);
            }

            // Method 3: Time proximity correlation with Jira activities
            var issueKeyFromTimeProximity = FindJiraIssueByTimeProximity(activity, jiraActivities);
            if (!string.IsNullOrEmpty(issueKeyFromTimeProximity))
            {
                return new JiraActivityCorrelation(
                    activity, 
                    issueKeyFromTimeProximity, 
                    CorrelationMethod.TimeProximity, 
                    0.7);
            }

            // Method 4: Development tool correlation (IDE, Git, etc.)
            var issueKeyFromDevTools = CorrelateWithDevelopmentTools(activityDescription);
            if (!string.IsNullOrEmpty(issueKeyFromDevTools))
            {
                return new JiraActivityCorrelation(
                    activity, 
                    issueKeyFromDevTools, 
                    CorrelationMethod.BranchName, 
                    0.8);
            }

            // No correlation found
            return new JiraActivityCorrelation(activity, null, CorrelationMethod.Manual, 0.0);
        }

        private string? ExtractJiraIssueKeyFromText(string text)
        {
            var match = JiraIssueKeyRegex.Match(text);
            return match.Success ? match.Value : null;
        }

        private string? ExtractJiraIssueKeyFromJiraUrl(string text)
        {
            var match = JiraUrlRegex.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        private string? FindJiraIssueByTimeProximity(EndedActivity activity, List<JiraActivity> jiraActivities)
        {
            // Find Jira activities that occurred within a reasonable time window of the system activity
            var timeWindow = TimeSpan.FromMinutes(30);
            
            var nearbyJiraActivities = jiraActivities
                .Where(ja => Math.Abs((ja.OccurrenceDate - activity.StartDate).TotalMinutes) <= timeWindow.TotalMinutes)
                .OrderBy(ja => Math.Abs((ja.OccurrenceDate - activity.StartDate).TotalMinutes))
                .ToList();

            if (nearbyJiraActivities.Any())
            {
                // Extract issue key from the closest Jira activity description
                var closestActivity = nearbyJiraActivities.First();
                return ExtractJiraIssueKeyFromText(closestActivity.Description);
            }

            return null;
        }

        private string? CorrelateWithDevelopmentTools(string activityDescription)
        {
            // Check for common development tools and extract issue keys
            var devToolPatterns = new[]
            {
                @"Visual Studio.*?([A-Z]+-\d+)",
                @"IntelliJ.*?([A-Z]+-\d+)",
                @"VS Code.*?([A-Z]+-\d+)",
                @"Git.*?([A-Z]+-\d+)",
                @"Terminal.*?([A-Z]+-\d+)"
            };

            foreach (var pattern in devToolPatterns)
            {
                var match = Regex.Match(activityDescription, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }

            // Check for branch names or commit messages that might contain issue keys
            if (activityDescription.ToLower().Contains("git") || 
                activityDescription.ToLower().Contains("branch") ||
                activityDescription.ToLower().Contains("commit"))
            {
                return ExtractJiraIssueKeyFromText(activityDescription);
            }

            return null;
        }
    }
}
