// src/TrackYourDay.Core/LlmPrompts/ILlmPromptService.cs
namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Orchestrates prompt generation for UI consumption.
/// </summary>
public interface ILlmPromptService
{
    /// <summary>
    /// Generates prompt for date range using specified template.
    /// Includes related Jira issues in the prompt.
    /// </summary>
    /// <exception cref="InvalidOperationException">No activities found or template invalid</exception>
    string GeneratePrompt(string templateKey, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Returns active templates ordered by DisplayOrder for dropdown.
    /// </summary>
    IReadOnlyList<LlmPromptTemplate> GetActiveTemplates();
}
