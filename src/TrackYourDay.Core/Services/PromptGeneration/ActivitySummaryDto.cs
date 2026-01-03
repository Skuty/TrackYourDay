namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Lightweight activity summary for prompt generation.
/// </summary>
public sealed record ActivitySummaryDto(
    string Title,
    string ApplicationName,
    TimeSpan StartTime,
    TimeSpan Duration);
