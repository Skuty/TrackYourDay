// src/TrackYourDay.Core/LlmPrompts/LlmPromptTemplateStore.cs
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Stores LLM prompt templates using GenericSettingsRepository.
/// Templates are serialized as JSON with keys prefixed by "llm_template:".
/// </summary>
public class LlmPromptTemplateStore(
    IGenericSettingsRepository settingsRepository,
    ILogger<LlmPromptTemplateStore> logger)
{
    private const string KeyPrefix = "llm_template:";
    private const string MetadataKey = "llm_template:_metadata";

    public IReadOnlyList<LlmPromptTemplate> GetAllTemplates()
    {
        var templates = new List<LlmPromptTemplate>();
        var keys = settingsRepository.GetAllKeys()
            .Where(k => k.StartsWith(KeyPrefix) && k != MetadataKey);

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

    public IReadOnlyList<LlmPromptTemplate> GetActiveTemplates()
    {
        return GetAllTemplates().Where(t => t.IsActive).ToList();
    }

    public LlmPromptTemplate? GetByKey(string templateKey)
    {
        var key = GetStorageKey(templateKey);
        var json = settingsRepository.GetSetting(key);
        return json != null ? JsonConvert.DeserializeObject<LlmPromptTemplate>(json) : null;
    }

    public void Save(LlmPromptTemplate template)
    {
        var key = GetStorageKey(template.TemplateKey);
        var json = JsonConvert.SerializeObject(template);
        settingsRepository.SetSetting(key, json);
        settingsRepository.Save();

        logger.LogDebug("Saved template {TemplateKey}", template.TemplateKey);
    }

    public void Update(LlmPromptTemplate template)
    {
        Save(template);
    }

    public void Delete(string templateKey)
    {
        var existing = GetByKey(templateKey);
        if (existing == null) return;

        var updated = existing with
        {
            IsActive = false,
            UpdatedAt = DateTime.UtcNow
        };
        Save(updated);
    }

    public void Restore(string templateKey)
    {
        var existing = GetByKey(templateKey);
        if (existing == null) return;

        var updated = existing with
        {
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
        Save(updated);
    }

    public int GetActiveTemplateCount()
    {
        return GetActiveTemplates().Count;
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
            Save(template);
        }

        logger.LogInformation("Seeded {Count} default LLM prompt templates", templates.Length);
    }

    private static string GetStorageKey(string templateKey) => $"{KeyPrefix}{templateKey}";
}
