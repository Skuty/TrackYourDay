using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.DailySummary
{
    public record class JiraIssueTimeSummary(
        string IssueKey,
        string IssueSummary,
        TimeSpan TotalTimeSpent,
        List<ActivityPeriod> ActivityPeriods)
    {
        public string FormattedTimeSpent => FormatTimeSpan(TotalTimeSpent);

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            else
                return $"{timeSpan.Minutes}m";
        }
    }

    public record class ActivityPeriod(
        DateTime StartTime,
        DateTime EndTime,
        string ActivityDescription,
        TimeSpan Duration)
    {
        public string FormattedDuration => JiraIssueTimeSummary.FormatTimeSpan(Duration);
        
        public static ActivityPeriod FromEndedActivity(EndedActivity activity)
        {
            return new ActivityPeriod(
                activity.StartDate,
                activity.EndDate,
                activity.GetDescription(),
                activity.GetDuration());
        }
    }

    public record class DailySummaryReport(
        DateOnly Date,
        TimeSpan TotalWorkTime,
        TimeSpan TotalJiraTime,
        List<JiraIssueTimeSummary> JiraIssues,
        List<ActivityPeriod> UnassignedActivities)
    {
        public string FormattedTotalWorkTime => JiraIssueTimeSummary.FormatTimeSpan(TotalWorkTime);
        public string FormattedTotalJiraTime => JiraIssueTimeSummary.FormatTimeSpan(TotalJiraTime);
        
        public int TotalJiraIssuesWorkedOn => JiraIssues.Count;
        
        public JiraIssueTimeSummary? MostWorkedOnIssue => 
            JiraIssues.OrderByDescending(x => x.TotalTimeSpent).FirstOrDefault();
    }

    public record class JiraActivityCorrelation(
        EndedActivity SystemActivity,
        string? DetectedIssueKey,
        CorrelationMethod Method,
        double ConfidenceScore)
    {
        public bool HasJiraIssue => !string.IsNullOrEmpty(DetectedIssueKey);
    }

    public enum CorrelationMethod
    {
        WindowTitle,
        JiraWebInterface,
        BranchName,
        CommitMessage,
        TimeProximity,
        Manual
    }
}
