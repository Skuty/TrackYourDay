namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Generates structured prompts for external LLM consumption.
/// </summary>
public interface IPromptGeneratorService
{
    /// <summary>
    /// Creates a prompt from daily activities using the specified template.
    /// </summary>
    /// <param name="date">Date for which to generate prompt.</param>
    /// <param name="template">Prompt template type.</param>
    /// <returns>Plain text prompt ready for LLM input.</returns>
    string GeneratePrompt(DateOnly date, PromptTemplate template);
}
