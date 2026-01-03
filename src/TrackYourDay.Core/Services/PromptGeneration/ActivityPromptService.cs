using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Retrieves and aggregates activity data for prompt generation.
/// </summary>
public sealed class ActivityPromptService(
    IHistoricalDataRepository<EndedActivity> activityRepository,
    IJiraActivityProvider jiraActivityProvider,
    ILogger<ActivityPromptService> logger) : IActivityPromptService
{
    private static readonly TimeSpan MinActivityDuration = TimeSpan.FromMinutes(5);

    public IReadOnlyList<ActivitySummaryDto> GetActivitiesForDate(DateOnly date)
    {
        logger.LogInformation("Fetching activities for prompt generation: {Date}", date);

        var systemActivities = GetSystemActivities(date);
        var jiraActivities = GetJiraActivities(date);

        var combined = systemActivities.Concat(jiraActivities)
            .OrderBy(a => a.StartTime)
            .ToList();

        logger.LogInformation("Retrieved {Count} activities ({SystemCount} system, {JiraCount} Jira)",
            combined.Count, systemActivities.Count, jiraActivities.Count);

        return combined;
    }

    private IReadOnlyList<ActivitySummaryDto> GetSystemActivities(DateOnly date)
    {
        var specification = new ActivityByDateSpecification(date);
        var activities = activityRepository.Find(specification);

        return activities
            .Where(a => a.GetDuration() >= MinActivityDuration)
            .GroupBy(a => new
            {
                Title = a.GetDescription(),
                ApplicationName = ExtractApplicationName(a.ActivityType.ActivityDescription)
            })
            .Select(g => new ActivitySummaryDto(
                Title: g.Key.Title ?? "(No title)",
                ApplicationName: g.Key.ApplicationName ?? "(Unknown)",
                StartTime: TimeOnly.FromDateTime(g.First().StartDate).ToTimeSpan(),
                Duration: TimeSpan.FromTicks(g.Sum(a => a.GetDuration().Ticks))
            ))
            .ToList();
    }

    private IReadOnlyList<ActivitySummaryDto> GetJiraActivities(DateOnly date)
    {
        var jiraActivities = jiraActivityProvider.GetJiraActivities();

        return jiraActivities
            .Where(a => DateOnly.FromDateTime(a.OccurrenceDate) == date)
            .Select(a => new ActivitySummaryDto(
                Title: a.Description,
                ApplicationName: "Jira",
                StartTime: TimeOnly.FromDateTime(a.OccurrenceDate).ToTimeSpan(),
                Duration: TimeSpan.Zero // Jira activities are events, not durations
            ))
            .ToList();
    }

    private static string ExtractApplicationName(string activityDescription)
    {
        if (activityDescription.Contains("Focus on application - "))
        {
            var parts = activityDescription.Split(new[] { " - " }, 2, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var windowTitle = parts[1];
                var titleParts = windowTitle.Split('-', '|', '—');
                return titleParts[0].Trim();
            }
        }
        else if (activityDescription.Contains("Application started - "))
        {
            return activityDescription.Replace("Application started - ", "").Trim();
        }

        return "System";
    }
}
