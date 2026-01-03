namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Provides static prompt templates for LLM work summarization.
/// </summary>
public sealed class PromptTemplateProvider : IPromptTemplateProvider
{
    private static readonly IReadOnlyDictionary<PromptTemplate, string> DisplayNames = new Dictionary<PromptTemplate, string>
    {
        [PromptTemplate.DetailedSummaryWithTimeAllocation] = "Detailed Summary with Time Allocation",
        [PromptTemplate.ConciseBulletPointSummary] = "Concise Bullet-Point Summary",
        [PromptTemplate.JiraFocusedWorklogTemplate] = "Jira-Focused Worklog Template"
    };

    private static readonly IReadOnlyDictionary<PromptTemplate, string> Templates = new Dictionary<PromptTemplate, string>
    {
        [PromptTemplate.DetailedSummaryWithTimeAllocation] = 
@"You are analyzing my work day on {DATE}.

Below is a chronological list of activities with durations:
{ACTIVITY_LIST}

Jira tickets I worked on: {JIRA_KEYS}

Task:
1. Group related activities into 3-9 distinct work items
2. Allocate time to each work item (must sum to total duration)
3. Map work items to Jira tickets where applicable
4. For items without tickets, provide descriptive summaries

Output format:
- [JIRA-123] Description (2h 30m)
- [JIRA-456] Description (1h 15m)
- [No ticket] Description (45m)",

        [PromptTemplate.ConciseBulletPointSummary] = 
@"Summarize my work on {DATE} in 3-9 bullet points.

Activities:
{ACTIVITY_LIST}

Jira tickets: {JIRA_KEYS}

Requirements:
- Each bullet = one work focus area
- Include time estimate
- Reference Jira keys when relevant
- If no tickets match, describe work without keys",

        [PromptTemplate.JiraFocusedWorklogTemplate] = 
@"Generate Jira worklog entries for {DATE}.

Activities:
{ACTIVITY_LIST}

Known tickets: {JIRA_KEYS}

Rules:
- Create 3-9 worklog entries
- Match activities to Jira keys when possible
- For unmatched work, create generic 'Development' or 'Research' entries without keys
- Include start time and duration for each entry

Format:
JIRA-123 | 09:00 | 2h 30m | Description
JIRA-456 | 11:30 | 1h 15m | Description
(No key) | 14:00 | 45m | Description"
    };

    public string GetTemplate(PromptTemplate template)
    {
        if (!Templates.TryGetValue(template, out var templateString))
        {
            throw new ArgumentException($"Unknown template: {template}", nameof(template));
        }

        return templateString;
    }

    public IReadOnlyDictionary<PromptTemplate, string> GetAvailableTemplates()
    {
        return DisplayNames;
    }
}
