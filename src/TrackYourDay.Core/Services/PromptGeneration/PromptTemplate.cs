namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Defines available prompt templates for LLM-based work summarization.
/// </summary>
public enum PromptTemplate
{
    /// <summary>
    /// Detailed narrative with explicit time allocation.
    /// </summary>
    DetailedSummaryWithTimeAllocation = 1,

    /// <summary>
    /// Concise bullet-point format.
    /// </summary>
    ConciseBulletPointSummary = 2,

    /// <summary>
    /// Jira worklog entry format with timestamps.
    /// </summary>
    JiraFocusedWorklogTemplate = 3
}
