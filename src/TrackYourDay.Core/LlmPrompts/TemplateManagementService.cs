// src/TrackYourDay.Core/LlmPrompts/TemplateManagementService.cs
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Manages template CRUD with validation logic.
/// </summary>
public class TemplateManagementService(
    ILlmPromptTemplateRepository repository,
    ILogger<TemplateManagementService> logger) : ITemplateManagementService
{
    public IReadOnlyList<LlmPromptTemplate> GetAllTemplates()
        => repository.GetAllTemplates();

    public LlmPromptTemplate? GetTemplateByKey(string templateKey)
        => repository.GetByKey(templateKey);

    public void SaveTemplate(string templateKey, string name, string systemPrompt, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);

        if (!LlmPromptTemplate.IsValidTemplateKey(templateKey))
        {
            throw new ArgumentException(
                "Template key must be 3-50 lowercase alphanumeric characters or hyphens",
                nameof(templateKey));
        }

        if (name.Length is < 5 or > 100)
        {
            throw new ArgumentException("Name must be 5-100 characters", nameof(name));
        }

        if (systemPrompt.Length is < 100 or > 10000)
        {
            throw new ArgumentException("Prompt must be 100-10,000 characters", nameof(systemPrompt));
        }

        if (!systemPrompt.Contains(LlmPromptTemplate.Placeholder, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Prompt must contain {LlmPromptTemplate.Placeholder}",
                nameof(systemPrompt));
        }

        if (displayOrder < 1)
        {
            throw new ArgumentException("Display order must be positive", nameof(displayOrder));
        }

        var existing = repository.GetByKey(templateKey);
        var now = DateTime.UtcNow;

        var template = existing != null
            ? existing with
            {
                Name = name,
                SystemPrompt = systemPrompt,
                DisplayOrder = displayOrder,
                UpdatedAt = now
            }
            : new LlmPromptTemplate
            {
                Id = 0,
                TemplateKey = templateKey,
                Name = name,
                SystemPrompt = systemPrompt,
                IsActive = true,
                DisplayOrder = displayOrder,
                CreatedAt = now,
                UpdatedAt = now
            };

        repository.Save(template);

        logger.LogInformation("Saved template {TemplateKey}: {Name}", templateKey, name);
    }

    public void DeleteTemplate(string templateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var activeCount = repository.GetActiveTemplateCount();
        if (activeCount <= 1)
        {
            throw new InvalidOperationException("Cannot delete the last active template");
        }

        repository.Delete(templateKey);

        logger.LogInformation("Deleted template {TemplateKey}", templateKey);
    }

    public void RestoreTemplate(string templateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        repository.Restore(templateKey);

        logger.LogInformation("Restored template {TemplateKey}", templateKey);
    }

    public void ReorderTemplates(Dictionary<string, int> keyToOrder)
    {
        if (keyToOrder.Count == 0) return;

        repository.BulkUpdateDisplayOrder(keyToOrder);

        logger.LogInformation("Reordered {Count} templates", keyToOrder.Count);
    }

    public string GeneratePreview(string systemPrompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);

        var sampleActivities = CreateSampleActivities();
        var markdown = SerializeToMarkdown(sampleActivities);
        
        return systemPrompt.Replace(LlmPromptTemplate.Placeholder, markdown);
    }

    private static GroupedActivity[] CreateSampleActivities()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        
        var activities = new[]
        {
            GroupedActivity.CreateEmptyWithDescriptionForDate(today, "Visual Studio Code - TrackYourDay project"),
            GroupedActivity.CreateEmptyWithDescriptionForDate(today, "PROJ-456: Code Review"),
            GroupedActivity.CreateEmptyWithDescriptionForDate(today, "Chrome - Stack Overflow"),
            GroupedActivity.CreateEmptyWithDescriptionForDate(today, "Microsoft Teams - Daily Standup"),
            GroupedActivity.CreateEmptyWithDescriptionForDate(today, "PROJ-789: Bug Investigation")
        };

        var guid = Guid.NewGuid();
        activities[0].Include(guid, new TimePeriod(DateTime.Today.AddHours(9), DateTime.Today.AddHours(11).AddMinutes(15)));
        activities[1].Include(guid, new TimePeriod(DateTime.Today.AddHours(11).AddMinutes(30), DateTime.Today.AddHours(13)));
        activities[2].Include(guid, new TimePeriod(DateTime.Today.AddHours(13).AddMinutes(30), DateTime.Today.AddHours(14)));
        activities[3].Include(guid, new TimePeriod(DateTime.Today.AddHours(14).AddMinutes(15), DateTime.Today.AddHours(14).AddMinutes(45)));
        activities[4].Include(guid, new TimePeriod(DateTime.Today.AddHours(15), DateTime.Today.AddHours(17)));

        return activities;
    }

    private static string SerializeToMarkdown(GroupedActivity[] activities)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("| Date       | Activity Description | Duration  |");
        sb.AppendLine("|------------|---------------------|-----------|");

        foreach (var activity in activities)
        {
            var duration = $"{(int)activity.Duration.TotalHours:D2}:{activity.Duration.Minutes:D2}:{activity.Duration.Seconds:D2}";
            var description = activity.Description.Replace("|", "\\|");
            sb.AppendLine($"| {activity.Date:yyyy-MM-dd} | {description} | {duration} |");
        }

        return sb.ToString();
    }
}
