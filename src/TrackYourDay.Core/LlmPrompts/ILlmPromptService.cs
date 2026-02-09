// src/TrackYourDay.Core/LlmPrompts/ILlmPromptService.cs
namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Orchestrates prompt generation by querying database repositories.
/// All data comes from pre-stored activities and snapshots (zero API calls).
/// </summary>
public interface ILlmPromptService
{
    /// <summary>
    /// Generates prompt for single date using specified template.
    /// Queries database repositories only - no external API calls.
    /// </summary>
    /// <param name="templateKey">Template identifier</param>
    /// <param name="date">Single date for activity filtering</param>
    /// <exception cref="InvalidOperationException">Template not found or invalid</exception>
    Task<string> GeneratePrompt(string templateKey, DateOnly date);

    /// <summary>
    /// Returns active templates ordered by DisplayOrder for dropdown.
    /// </summary>
    IReadOnlyList<LlmPromptTemplate> GetActiveTemplates();
}
