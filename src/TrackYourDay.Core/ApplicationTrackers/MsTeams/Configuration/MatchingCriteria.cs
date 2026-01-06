namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

/// <summary>
/// Defines which process attributes must match for a rule to apply.
/// </summary>
public enum MatchingCriteria
{
    /// <summary>
    /// Only process name pattern must match (window title ignored).
    /// </summary>
    ProcessNameOnly = 1,

    /// <summary>
    /// Only window title pattern must match (process name ignored).
    /// </summary>
    WindowTitleOnly = 2,

    /// <summary>
    /// Both process name AND window title patterns must match.
    /// </summary>
    Both = 3
}
