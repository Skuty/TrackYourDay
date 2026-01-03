namespace TrackYourDay.Core.ApplicationTrackers.Jira;

/// <summary>
/// Provides access to Jira activities tracked by the system.
/// </summary>
public interface IJiraActivityProvider
{
    /// <summary>
    /// Gets all tracked Jira activities.
    /// </summary>
    IReadOnlyCollection<JiraActivity> GetJiraActivities();
}
