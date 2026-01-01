// src/TrackYourDay.Core/LlmPrompts/ITemplateManagementService.cs
namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Manages template CRUD operations for Settings UI.
/// </summary>
public interface ITemplateManagementService
{
    IReadOnlyList<LlmPromptTemplate> GetAllTemplates();
    LlmPromptTemplate? GetTemplateByKey(string templateKey);

    /// <summary>
    /// Validates and saves template.
    /// </summary>
    /// <exception cref="ArgumentException">Validation failed</exception>
    void SaveTemplate(string templateKey, string name, string systemPrompt, int displayOrder);

    /// <summary>
    /// Soft deletes template. Blocks if last active template.
    /// </summary>
    /// <exception cref="InvalidOperationException">Cannot delete last active template</exception>
    void DeleteTemplate(string templateKey);

    void RestoreTemplate(string templateKey);
    void ReorderTemplates(Dictionary<string, int> keyToOrder);

    /// <summary>
    /// Generates preview using hardcoded sample activities.
    /// </summary>
    string GeneratePreview(string systemPrompt);
}
