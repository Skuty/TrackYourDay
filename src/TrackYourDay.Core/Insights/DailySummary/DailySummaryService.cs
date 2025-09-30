using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.DailySummary
{
    public interface IDailySummaryService
    {
        Task<DailySummaryReport> GenerateDailySummaryAsync(DateOnly date);
        Task<DailySummaryReport> GenerateDailySummaryAsync(DateOnly date, IReadOnlyCollection<EndedActivity> activities);
    }

    public class DailySummaryService : IDailySummaryService
    {
        private readonly IJiraActivityService jiraActivityService;
        private readonly IJiraActivityCorrelationService correlationService;
        private readonly WorkdayReadModelRepository workdayRepository;
        private readonly ILogger<DailySummaryService> logger;

        public DailySummaryService(
            IJiraActivityService jiraActivityService,
            IJiraActivityCorrelationService correlationService,
            WorkdayReadModelRepository workdayRepository,
            ILogger<DailySummaryService> logger)
        {
            this.jiraActivityService = jiraActivityService;
            this.correlationService = correlationService;
            this.workdayRepository = workdayRepository;
            this.logger = logger;
        }

        public async Task<DailySummaryReport> GenerateDailySummaryAsync(DateOnly date)
        {
            // Get workday data which contains all activities for the date
            var workday = workdayRepository.Get(date);
            
            // For now, we'll need to get activities from somewhere else since workday doesn't expose them directly
            // This is a limitation of the current architecture - we might need to enhance it
            var activities = new List<EndedActivity>(); // TODO: Get actual activities from storage
            
            return await GenerateDailySummaryAsync(date, activities);
        }

        public async Task<DailySummaryReport> GenerateDailySummaryAsync(DateOnly date, IReadOnlyCollection<EndedActivity> activities)
        {
            try
            {
                logger.LogInformation("Generating daily summary for {Date}", date);

                // Get Jira activities for the date
                var startOfDay = date.ToDateTime(TimeOnly.MinValue);
                var jiraActivities = jiraActivityService.GetActivitiesUpdatedAfter(startOfDay);

                // Correlate system activities with Jira issues
                var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

                // Group activities by Jira issue
                var jiraIssueGroups = GroupActivitiesByJiraIssue(correlations, jiraActivities);

                // Calculate total work time
                var totalWorkTime = activities.Aggregate(TimeSpan.Zero, (sum, activity) => sum + activity.GetDuration());

                // Calculate total Jira-related time
                var totalJiraTime = TimeSpan.FromMinutes(jiraIssueGroups.Sum(x => x.TotalTimeSpent.TotalMinutes));

                // Get unassigned activities (those not correlated to any Jira issue)
                var unassignedActivities = correlations
                    .Where(c => !c.HasJiraIssue)
                    .Select(c => ActivityPeriod.FromEndedActivity(c.SystemActivity))
                    .ToList();

                var report = new DailySummaryReport(
                    date,
                    totalWorkTime,
                    totalJiraTime,
                    jiraIssueGroups,
                    unassignedActivities);

                logger.LogInformation("Generated daily summary for {Date}: {JiraIssueCount} Jira issues, {TotalTime} total work time", 
                    date, jiraIssueGroups.Count, totalWorkTime);

                return report;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating daily summary for {Date}", date);
                throw;
            }
        }

        private List<JiraIssueTimeSummary> GroupActivitiesByJiraIssue(
            List<JiraActivityCorrelation> correlations, 
            List<JiraActivity> jiraActivities)
        {
            return correlations
                .Where(c => c.HasJiraIssue)
                .GroupBy(c => c.DetectedIssueKey)
                .Select(g => {
                    var jiraIssue = jiraActivities.FirstOrDefault(j => j.Description.Contains(g.Key!));
                    var totalTicks = g.Sum(c => c.SystemActivity.GetDuration().Ticks);
                    var activityPeriods = g.Select(c => ActivityPeriod.FromEndedActivity(c.SystemActivity)).ToList();
                    
                    var issueSummary = jiraIssue?.Description ?? GetIssueSummaryFromJiraActivities(g.Key!, jiraActivities);
                    var totalTime = TimeSpan.FromTicks(totalTicks);
                    
                    return new JiraIssueTimeSummary(
                        g.Key!,
                        issueSummary,
                        totalTime,
                        activityPeriods);
                })
                .OrderByDescending(x => x.TotalTimeSpent)
                .ToList();
        }

        private string GetIssueSummaryFromJiraActivities(string issueKey, List<JiraActivity> jiraActivities)
        {
            // Try to extract issue summary from Jira activity descriptions
            var relevantActivity = jiraActivities
                .FirstOrDefault(ja => ja.Description.Contains(issueKey));

            if (relevantActivity != null)
            {
                // Parse the issue summary from the Jira activity description
                // Format: "Jira {ActionType} - {IssueKey}: {FieldName} {ChangeDescription} | Issue: {Summary} | Time: {Timestamp}"
                var parts = relevantActivity.Description.Split('|');
                if (parts.Length >= 2)
                {
                    var issuePart = parts[1].Trim();
                    if (issuePart.StartsWith("Issue:"))
                    {
                        return issuePart.Substring(6).Trim();
                    }
                }
            }

            return $"Issue {issueKey}"; // Fallback if we can't extract the summary
        }
    }
}
