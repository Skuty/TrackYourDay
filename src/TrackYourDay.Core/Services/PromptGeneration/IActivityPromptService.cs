namespace TrackYourDay.Core.Services.PromptGeneration;

/// <summary>
/// Retrieves and aggregates activity data for prompt generation.
/// </summary>
public interface IActivityPromptService
{
    /// <summary>
    /// Gets deduplicated activities and Jira work for the specified date.
    /// </summary>
    /// <param name="date">Date to retrieve activities for.</param>
    /// <returns>Collection of activity summaries with Jira keys.</returns>
    IReadOnlyList<ActivitySummaryDto> GetActivitiesForDate(DateOnly date);
}
