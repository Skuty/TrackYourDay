// src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.LlmPrompts;

public class LlmPromptService(
    IGenericSettingsRepository settingsRepository,
    IHistoricalDataRepository<EndedActivity> activityRepository,
    IHistoricalDataRepository<EndedMeeting> meetingRepository,
    UserTaskService userTaskService,
    ActivityNameSummaryStrategy summaryStrategy,
    IJiraActivityService jiraActivityService,
    ILogger<LlmPromptService> logger) : ILlmPromptService
{
    private const int AverageRowBytes = 80;
    private const string KeyPrefix = "llm_template:";

    public string GeneratePrompt(string templateKey, DateOnly startDate, DateOnly endDate, bool includeJiraIssues = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        if (startDate > endDate)
            throw new ArgumentException("Start date must not be after end date", nameof(startDate));

        var template = GetTemplateByKey(templateKey) 
            ?? throw new InvalidOperationException($"Template '{templateKey}' not found");

        if (!template.HasValidPlaceholder())
            throw new InvalidOperationException($"Template '{templateKey}' missing placeholder");

        var activities = GetActivitiesForDateRange(startDate, endDate);
        if (activities.Count == 0)
            throw new InvalidOperationException($"No activities found for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        var markdown = SerializeToMarkdown(activities);
        
        if (includeJiraIssues)
        {
            var jiraIssuesMarkdown = GetJiraIssuesMarkdown(startDate, endDate);
            if (!string.IsNullOrEmpty(jiraIssuesMarkdown))
            {
                markdown = $"{markdown}\n\n{jiraIssuesMarkdown}";
            }
        }
        
        var rendered = template.SystemPrompt.Replace(LlmPromptTemplate.Placeholder, markdown);

        logger.LogInformation("Generated prompt for {TemplateKey}: {CharCount} characters, {ActivityCount} activities, JiraIssues={IncludeJira}",
            templateKey, rendered.Length, activities.Count, includeJiraIssues);

        return rendered;
    }

    public IReadOnlyList<LlmPromptTemplate> GetActiveTemplates()
    {
        var templates = new List<LlmPromptTemplate>();
        var keys = settingsRepository.GetAllKeys()
            .Where(k => k.StartsWith(KeyPrefix));

        foreach (var key in keys)
        {
            var json = settingsRepository.GetSetting(key);
            if (json != null)
            {
                var template = JsonConvert.DeserializeObject<LlmPromptTemplate>(json);
                if (template != null && template.IsActive)
                {
                    templates.Add(template);
                }
            }
        }

        return templates.OrderBy(t => t.DisplayOrder).ToList();
    }

    private IReadOnlyCollection<GroupedActivity> GetActivitiesForDateRange(DateOnly startDate, DateOnly endDate)
    {
        var allItems = new List<TrackedActivity>();
        allItems.AddRange(activityRepository.Find(new ActivityByDateRangeSpecification(startDate, endDate)));
        allItems.AddRange(meetingRepository.Find(new MeetingByDateRangeSpecification(startDate, endDate)));
        var userTasks = userTaskService.GetAllTasks()
            .Where(t => t.IsCompleted && DateOnly.FromDateTime(t.StartDate) >= startDate && DateOnly.FromDateTime(t.StartDate) <= endDate);
        allItems.AddRange(userTasks);
        return summaryStrategy.Generate(allItems);
    }

    private static string SerializeToMarkdown(IReadOnlyCollection<GroupedActivity> activities)
    {
        var sb = new StringBuilder(activities.Count * AverageRowBytes + 200);
        sb.AppendLine("| Date       | Activity Description | Duration  |");
        sb.AppendLine("|------------|---------------------|-----------|");
        foreach (var activity in activities)
        {
            var duration = FormatDuration(activity.Duration);
            var description = EscapeMarkdown(activity.Description);
            sb.AppendLine($"| {activity.Date:yyyy-MM-dd} | {description} | {duration} |");
        }
        return sb.ToString();
    }

    private static string FormatDuration(TimeSpan duration) 
        => $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

    private static string EscapeMarkdown(string text) 
        => text.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");

    private LlmPromptTemplate? GetTemplateByKey(string templateKey)
    {
        var key = GetStorageKey(templateKey);
        var json = settingsRepository.GetSetting(key);
        return json != null ? JsonConvert.DeserializeObject<LlmPromptTemplate>(json) : null;
    }

    private static string GetStorageKey(string templateKey) => $"{KeyPrefix}{templateKey}";

    private string GetJiraIssuesMarkdown(DateOnly startDate, DateOnly endDate)
    {
        try
        {
            var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
            var jiraActivities = jiraActivityService.GetActivitiesUpdatedAfter(startDateTime)
                .Where(a => DateOnly.FromDateTime(a.OccurrenceDate) >= startDate 
                         && DateOnly.FromDateTime(a.OccurrenceDate) <= endDate)
                .OrderBy(a => a.OccurrenceDate)
                .ToList();

            if (jiraActivities.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(jiraActivities.Count * AverageRowBytes + 200);
            sb.AppendLine("## Related Jira Issues");
            sb.AppendLine();
            sb.AppendLine("| Date       | Jira Activity Description |");
            sb.AppendLine("|------------|---------------------------|");
            
            foreach (var activity in jiraActivities)
            {
                var description = EscapeMarkdown(activity.Description);
                sb.AppendLine($"| {activity.OccurrenceDate:yyyy-MM-dd} | {description} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Jira issues for date range {StartDate} to {EndDate}", 
                startDate, endDate);
            return string.Empty;
        }
    }
}
