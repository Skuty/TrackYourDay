using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.DailySummary
{
    public static class DailySummaryExtensions
    {
        /// <summary>
        /// Extension method to get activities from ActivityTracker for daily summary generation
        /// </summary>
        public static IReadOnlyCollection<EndedActivity> GetActivitiesForDate(this ActivityTracker activityTracker, DateOnly date)
        {
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            var endOfDay = date.ToDateTime(TimeOnly.MaxValue);

            return activityTracker.GetEndedActivities()
                .Where(activity => activity.StartDate >= startOfDay && activity.EndDate <= endOfDay)
                .ToList();
        }

        /// <summary>
        /// Formats the daily summary report as a readable text summary
        /// </summary>
        public static string ToFormattedSummary(this DailySummaryReport report)
        {
            var summary = $"""
                Daily Work Summary - {report.Date:yyyy-MM-dd}
                ================================================
                
                📊 Overview:
                • Total Work Time: {report.FormattedTotalWorkTime}
                • Jira-Related Time: {report.FormattedTotalJiraTime}
                • Issues Worked On: {report.TotalJiraIssuesWorkedOn}
                
                🎯 Jira Issues:
                """;

            if (report.JiraIssues.Any())
            {
                foreach (var issue in report.JiraIssues)
                {
                    summary += $"""
                        
                        [{issue.IssueKey}] {issue.IssueSummary}
                        ⏱️ Time Spent: {issue.FormattedTimeSpent}
                        📝 Activities:
                        """;

                    foreach (var activity in issue.ActivityPeriods)
                    {
                        summary += $"""
                              • {activity.StartTime:HH:mm}-{activity.EndTime:HH:mm} ({activity.FormattedDuration}) - {activity.ActivityDescription}
                            """;
                    }
                }
            }
            else
            {
                summary += "\n   No Jira issues detected in activities.";
            }

            if (report.UnassignedActivities.Any())
            {
                summary += $"""
                    
                    
                    🔍 Other Activities ({report.UnassignedActivities.Sum(a => a.Duration).TotalMinutes:F0}m total):
                    """;

                foreach (var activity in report.UnassignedActivities.Take(10)) // Limit to first 10
                {
                    summary += $"""
                          • {activity.StartTime:HH:mm}-{activity.EndTime:HH:mm} ({activity.FormattedDuration}) - {activity.ActivityDescription}
                        """;
                }

                if (report.UnassignedActivities.Count > 10)
                {
                    summary += $"\n      ... and {report.UnassignedActivities.Count - 10} more activities";
                }
            }

            return summary;
        }

        /// <summary>
        /// Exports the daily summary as a simple CSV format
        /// </summary>
        public static string ToCsv(this DailySummaryReport report)
        {
            var csv = "Date,IssueKey,IssueSummary,TimeSpent(Minutes),ActivityCount\n";
            
            foreach (var issue in report.JiraIssues)
            {
                csv += $"{report.Date:yyyy-MM-dd},{issue.IssueKey},\"{issue.IssueSummary}\",{issue.TotalTimeSpent.TotalMinutes:F0},{issue.ActivityPeriods.Count}\n";
            }

            return csv;
        }

        /// <summary>
        /// Gets productivity insights from the daily summary
        /// </summary>
        public static ProductivityInsights GetProductivityInsights(this DailySummaryReport report)
        {
            var jiraTimePercentage = report.TotalWorkTime.TotalMinutes > 0 
                ? (report.TotalJiraTime.TotalMinutes / report.TotalWorkTime.TotalMinutes) * 100 
                : 0;

            var averageTimePerIssue = report.JiraIssues.Any() 
                ? TimeSpan.FromMinutes(report.TotalJiraTime.TotalMinutes / report.JiraIssues.Count)
                : TimeSpan.Zero;

            var longestSession = report.JiraIssues
                .SelectMany(i => i.ActivityPeriods)
                .OrderByDescending(a => a.Duration)
                .FirstOrDefault();

            return new ProductivityInsights(
                jiraTimePercentage,
                averageTimePerIssue,
                longestSession,
                report.JiraIssues.Count,
                report.UnassignedActivities.Count);
        }
    }

    public record class ProductivityInsights(
        double JiraTimePercentage,
        TimeSpan AverageTimePerIssue,
        ActivityPeriod? LongestSession,
        int TotalIssuesWorkedOn,
        int UnassignedActivitiesCount)
    {
        public string Summary => $"""
            Productivity Insights:
            • {JiraTimePercentage:F1}% of time spent on Jira issues
            • Average {AverageTimePerIssue.TotalMinutes:F0}m per issue
            • Worked on {TotalIssuesWorkedOn} different issues
            • {UnassignedActivitiesCount} activities not linked to Jira
            """;
    }
}
