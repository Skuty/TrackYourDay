namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

/// <summary>
/// String matching algorithms supported for pattern matching.
/// </summary>
public enum PatternMatchMode
{
    /// <summary>
    /// Substring match: target.Contains(pattern)
    /// </summary>
    Contains = 1,

    /// <summary>
    /// Prefix match: target.StartsWith(pattern)
    /// </summary>
    StartsWith = 2,

    /// <summary>
    /// Suffix match: target.EndsWith(pattern)
    /// </summary>
    EndsWith = 3,

    /// <summary>
    /// Exact equality: target == pattern
    /// </summary>
    Exact = 4,

    /// <summary>
    /// Regular expression: Regex.IsMatch(target, pattern)
    /// Timeout: 2 seconds to prevent ReDoS.
    /// </summary>
    Regex = 5
}
