// src/TrackYourDay.Core/LlmPrompts/LlmPromptTemplate.cs
namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Prompt template with three separate placeholders for Jira activities, GitLab activities, and currently assigned issues.
/// </summary>
public sealed record LlmPromptTemplate
{
    public required string TemplateKey { get; init; }
    public required string Name { get; init; }
    public required string SystemPrompt { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Placeholder for Jira activities that occurred on the specified date.
    /// </summary>
    public const string JiraActivitiesPlaceholder = "{JIRA_ACTIVITIES}";
    
    /// <summary>
    /// Placeholder for GitLab activities that occurred on the specified date.
    /// </summary>
    public const string GitLabActivitiesPlaceholder = "{GITLAB_ACTIVITIES}";
    
    /// <summary>
    /// Placeholder for currently assigned issues (snapshot at generation time).
    /// </summary>
    public const string CurrentlyAssignedIssuesPlaceholder = "{CURRENTLY_ASSIGNED_ISSUES}";

    public static bool IsValidTemplateKey(string key)
        => !string.IsNullOrWhiteSpace(key)
           && key.Length is >= 3 and <= 50
           && key.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');

    /// <summary>
    /// Validates template contains at least one placeholder.
    /// </summary>
    public bool HasValidPlaceholder()
        => SystemPrompt.Contains(JiraActivitiesPlaceholder, StringComparison.Ordinal)
        || SystemPrompt.Contains(GitLabActivitiesPlaceholder, StringComparison.Ordinal)
        || SystemPrompt.Contains(CurrentlyAssignedIssuesPlaceholder, StringComparison.Ordinal);
}
