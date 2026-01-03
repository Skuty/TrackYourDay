namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Provides static prompt templates with placeholder substitution.
/// </summary>
public interface IPromptTemplateProvider
{
    /// <summary>
    /// Retrieves the template string for the specified type.
    /// </summary>
    string GetTemplate(PromptTemplate template);

    /// <summary>
    /// Returns all available template types with display names.
    /// </summary>
    IReadOnlyDictionary<PromptTemplate, string> GetAvailableTemplates();
}
