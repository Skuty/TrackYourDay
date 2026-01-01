// src/TrackYourDay.Core/LlmPrompts/ILlmPromptTemplateRepository.cs
namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Template persistence following SqliteGenericSettingsRepository pattern.
/// </summary>
public interface ILlmPromptTemplateRepository
{
    IReadOnlyList<LlmPromptTemplate> GetActiveTemplates();
    IReadOnlyList<LlmPromptTemplate> GetAllTemplates();
    LlmPromptTemplate? GetByKey(string templateKey);
    void Save(LlmPromptTemplate template);
    void Delete(string templateKey);
    void Restore(string templateKey);
    int GetActiveTemplateCount();
    bool TemplateKeyExists(string templateKey);
    int GetMaxDisplayOrder();
    void BulkUpdateDisplayOrder(Dictionary<string, int> keyToOrder);
}
