// src/TrackYourDay.Core/LlmPrompts/LlmPromptTemplate.cs
namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// Prompt template with placeholder for activity data injection.
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

    public const string Placeholder = "{ACTIVITY_DATA_PLACEHOLDER}";

    public static bool IsValidTemplateKey(string key)
        => !string.IsNullOrWhiteSpace(key)
           && key.Length is >= 3 and <= 50
           && key.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');

    public bool HasValidPlaceholder()
        => SystemPrompt.Contains(Placeholder, StringComparison.Ordinal);
}
