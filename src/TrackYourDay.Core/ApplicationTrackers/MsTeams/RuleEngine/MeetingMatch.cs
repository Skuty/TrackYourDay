namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

/// <summary>
/// Result of successful rule evaluation.
/// </summary>
public sealed record MeetingMatch
{
    /// <summary>
    /// ID of the rule that matched.
    /// </summary>
    public required Guid MatchedRuleId { get; init; }

    /// <summary>
    /// Process name that matched.
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Window title that matched.
    /// </summary>
    public required string WindowTitle { get; init; }

    /// <summary>
    /// UTC timestamp when match occurred.
    /// </summary>
    public required DateTime MatchedAt { get; init; }
}
