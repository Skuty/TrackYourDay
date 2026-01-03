using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Generates LLM prompts from daily activity data.
/// </summary>
public sealed partial class PromptGeneratorService(
    IPromptTemplateProvider templateProvider,
    IActivityPromptService activityService,
    ILogger<PromptGeneratorService> logger) : IPromptGeneratorService
{
    [GeneratedRegex(@"^[A-Z]{2,10}-\d+$")]
    private static partial Regex JiraKeyPattern();

    public string GeneratePrompt(DateOnly date, PromptTemplate template)
    {
        logger.LogInformation("Generating prompt for {Date} using template {Template}", date, template);

        var activities = activityService.GetActivitiesForDate(date);
        var templateString = templateProvider.GetTemplate(template);
        var activityList = FormatActivityList(activities);
        var jiraKeys = ExtractJiraKeys(activities);
        var jiraKeyList = jiraKeys.Count > 0
            ? string.Join(", ", jiraKeys)
            : "No Jira tickets detected. Provide general summary without ticket references.";

        return templateString
            .Replace("{DATE}", date.ToString("yyyy-MM-dd"))
            .Replace("{ACTIVITY_LIST}", activityList)
            .Replace("{JIRA_KEYS}", jiraKeyList);
    }

    private static string FormatActivityList(IReadOnlyList<ActivitySummaryDto> activities)
    {
        if (activities.Count == 0)
        {
            return "(No activities recorded)";
        }

        var sb = new StringBuilder();
        foreach (var activity in activities)
        {
            var startTime = activity.StartTime.ToString(@"hh\:mm");
            var duration = FormatDuration(activity.Duration);
            sb.AppendLine($"- {startTime} | {duration} | {activity.ApplicationName} | {activity.Title}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }

        return $"{duration.Minutes}m";
    }

    private static IReadOnlyList<string> ExtractJiraKeys(IReadOnlyList<ActivitySummaryDto> activities)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var activity in activities)
        {
            // Jira activities have format "KEY-123: Summary"
            if (activity.Title.Contains(':'))
            {
                var potentialKey = activity.Title.Split(':')[0].Trim();
                if (JiraKeyPattern().IsMatch(potentialKey))
                {
                    keys.Add(potentialKey.ToUpperInvariant());
                }
            }
        }

        return keys.OrderBy(k => k).ToList();
    }
}
