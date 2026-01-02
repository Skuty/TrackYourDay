// src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs
using System.Text;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.LlmPrompts;

public class LlmPromptService(
    LlmPromptTemplateStore templateStore,
    IHistoricalDataRepository<EndedActivity> activityRepository,
    IHistoricalDataRepository<EndedMeeting> meetingRepository,
    UserTaskService userTaskService,
    ActivityNameSummaryStrategy summaryStrategy,
    ILogger<LlmPromptService> logger) : ILlmPromptService
{
    private const int AverageRowBytes = 80;

    public string GeneratePrompt(string templateKey, DateOnly startDate, DateOnly endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        if (startDate > endDate)
            throw new ArgumentException("Start date must not be after end date", nameof(startDate));

        var template = templateStore.GetByKey(templateKey) 
            ?? throw new InvalidOperationException($"Template '{templateKey}' not found");

        if (!template.HasValidPlaceholder())
            throw new InvalidOperationException($"Template '{templateKey}' missing placeholder");

        var activities = GetActivitiesForDateRange(startDate, endDate);
        if (activities.Count == 0)
            throw new InvalidOperationException($"No activities found for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        var markdown = SerializeToMarkdown(activities);
        var rendered = template.SystemPrompt.Replace(LlmPromptTemplate.Placeholder, markdown);

        logger.LogInformation("Generated prompt for {TemplateKey}: {CharCount} characters, {ActivityCount} activities",
            templateKey, rendered.Length, activities.Count);

        return rendered;
    }

    public IReadOnlyList<LlmPromptTemplate> GetActiveTemplates() => templateStore.GetActiveTemplates();

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
}
