// src/TrackYourDay.Core/LlmPrompts/TemplateManagementService.cs
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.LlmPrompts;

public class TemplateManagementService(
    IGenericSettingsRepository settingsRepository,
    ILogger<TemplateManagementService> logger) : ITemplateManagementService
{
    private const string KeyPrefix = "llm_template:";

    public IReadOnlyList<LlmPromptTemplate> GetAllTemplates()
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
                if (template != null)
                {
                    templates.Add(template);
                }
            }
        }

        return templates.OrderBy(t => t.DisplayOrder).ToList();
    }

    public LlmPromptTemplate? GetTemplateByKey(string templateKey)
    {
        var key = GetStorageKey(templateKey);
        var json = settingsRepository.GetSetting(key);
        return json != null ? JsonConvert.DeserializeObject<LlmPromptTemplate>(json) : null;
    }

    public void SaveTemplate(string templateKey, string name, string systemPrompt, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);

        if (!LlmPromptTemplate.IsValidTemplateKey(templateKey))
            throw new ArgumentException("Template key must be 3-50 lowercase alphanumeric characters or hyphens", nameof(templateKey));
        if (name.Length is < 5 or > 100)
            throw new ArgumentException("Name must be 5-100 characters", nameof(name));
        if (systemPrompt.Length is < 100 or > 10000)
            throw new ArgumentException("Prompt must be 100-10,000 characters", nameof(systemPrompt));
        if (!systemPrompt.Contains(LlmPromptTemplate.Placeholder, StringComparison.Ordinal))
            throw new ArgumentException($"Prompt must contain {LlmPromptTemplate.Placeholder}", nameof(systemPrompt));
        if (displayOrder < 1)
            throw new ArgumentException("Display order must be positive", nameof(displayOrder));

        var existing = GetTemplateByKey(templateKey);
        var now = DateTime.UtcNow;

        if (existing != null)
        {
            var updated = existing with { Name = name, SystemPrompt = systemPrompt, DisplayOrder = displayOrder, UpdatedAt = now };
            SaveTemplate(updated);
        }
        else
        {
            var template = new LlmPromptTemplate
            {
                TemplateKey = templateKey, Name = name, SystemPrompt = systemPrompt,
                IsActive = true, DisplayOrder = displayOrder, CreatedAt = now, UpdatedAt = now
            };
            SaveTemplate(template);
        }

        logger.LogInformation("Saved template {TemplateKey}: {Name}", templateKey, name);
    }

    public void DeleteTemplate(string templateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        if (GetActiveTemplateCount() <= 1)
            throw new InvalidOperationException("Cannot delete the last active template");

        var existing = GetTemplateByKey(templateKey);
        if (existing == null) return;

        var updated = existing with
        {
            IsActive = false,
            UpdatedAt = DateTime.UtcNow
        };
        SaveTemplate(updated);

        logger.LogInformation("Deleted template {TemplateKey}", templateKey);
    }

    public void RestoreTemplate(string templateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var existing = GetTemplateByKey(templateKey);
        if (existing == null) return;

        var updated = existing with
        {
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
        SaveTemplate(updated);

        logger.LogInformation("Restored template {TemplateKey}", templateKey);
    }

    public void ReorderTemplates(Dictionary<string, int> keyToOrder)
    {
        if (keyToOrder.Count == 0) return;
        var now = DateTime.UtcNow;
        foreach (var kvp in keyToOrder)
        {
            var existing = GetTemplateByKey(kvp.Key);
            if (existing != null)
            {
                var updated = existing with { DisplayOrder = kvp.Value, UpdatedAt = now };
                SaveTemplate(updated);
            }
        }
        logger.LogInformation("Reordered {Count} templates", keyToOrder.Count);
    }

    public void SeedDefaultTemplates()
    {
        if (GetActiveTemplateCount() > 0)
        {
            logger.LogDebug("Templates already seeded");
            return;
        }

        var now = DateTime.UtcNow;
        var templates = new[]
        {
            new LlmPromptTemplate
            {
                TemplateKey = "detailed",
                Name = "Detailed Time Breakdown",
                SystemPrompt = @"You are a time tracking assistant. Analyze the following activity log and generate a detailed time log report.

REQUIREMENTS:
- Generate between 3 and 9 time log entries
- Group similar activities together
- Identify Jira ticket keys using pattern: [A-Z][A-Z0-9]+-\d+
- If no Jira key found, use ""N/A"" for Jira Key field
- Sum durations for grouped activities
- Each entry must include: Description, Duration (decimal hours), Jira Key
- Note: Durations already exclude break periods

ACTIVITY DATA:
{ACTIVITY_DATA_PLACEHOLDER}

OUTPUT FORMAT:
| Description | Duration (hours) | Jira Key |
|-------------|------------------|----------|
| ... | ... | ... |

Generate the report now.",
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new LlmPromptTemplate
            {
                TemplateKey = "concise",
                Name = "Concise Summary",
                SystemPrompt = @"Summarize the following workday into 3-9 time log entries suitable for Jira worklog submission.

Rules:
1. Merge similar activities
2. Extract Jira keys (format: ABC-123) from descriptions
3. If no key found, write ""No ticket"" in Jira Key column
4. Convert durations to decimal hours (e.g., 1h 30m = 1.5)
5. Break time already excluded from durations

Data:
{ACTIVITY_DATA_PLACEHOLDER}

Output as table with columns: Task, Hours, Jira Ticket",
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = now,
                UpdatedAt = now
            },
            new LlmPromptTemplate
            {
                TemplateKey = "task-oriented",
                Name = "Task-Oriented Log",
                SystemPrompt = @"Act as a project manager reviewing an engineer's workday. Group the activities below into distinct tasks (minimum 3, maximum 9).

For each task:
- Write a clear description
- Sum total time spent (already excludes breaks)
- Identify associated Jira ticket (if mentioned in activity names)
- If no ticket, indicate ""Administrative"" or ""Untracked""

Activities:
{ACTIVITY_DATA_PLACEHOLDER}

Format output as:
1. [Jira Key or ""N/A""] - Description (X.X hours)
2. ...",
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var template in templates)
        {
            SaveTemplate(template);
        }

        logger.LogInformation("Seeded {Count} default LLM prompt templates", templates.Length);
    }

    private int GetActiveTemplateCount()
    {
        return GetAllTemplates().Count(t => t.IsActive);
    }

    private void SaveTemplate(LlmPromptTemplate template)
    {
        var key = GetStorageKey(template.TemplateKey);
        var json = JsonConvert.SerializeObject(template);
        settingsRepository.SetSetting(key, json);
        settingsRepository.Save();

        logger.LogDebug("Saved template {TemplateKey}", template.TemplateKey);
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

    private static string GetStorageKey(string templateKey) => $"{KeyPrefix}{templateKey}";
}
