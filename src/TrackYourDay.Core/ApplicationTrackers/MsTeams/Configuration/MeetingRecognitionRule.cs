namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

/// <summary>
/// Defines a single meeting recognition rule with priority ordering.
/// Rules are evaluated in ascending priority order (1 = highest priority).
/// </summary>
public sealed record MeetingRecognitionRule
{
    /// <summary>
    /// Unique identifier for the rule. Generated on creation.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Evaluation priority. Lower values evaluated first (1, 2, 3...).
    /// Must be unique across all rules in the rule set.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Determines which process attributes must match for rule to apply.
    /// </summary>
    public required MatchingCriteria Criteria { get; init; }

    /// <summary>
    /// Pattern for matching process name (e.g., "ms-teams.exe").
    /// Required if Criteria is ProcessNameOnly or Both.
    /// </summary>
    public PatternDefinition? ProcessNamePattern { get; init; }

    /// <summary>
    /// Pattern for matching window title.
    /// Required if Criteria is WindowTitleOnly or Both.
    /// </summary>
    public PatternDefinition? WindowTitlePattern { get; init; }

    /// <summary>
    /// Patterns that must NOT match. If any exclusion matches, rule fails.
    /// Evaluated after inclusion patterns pass.
    /// </summary>
    public IReadOnlyList<PatternDefinition> Exclusions { get; init; } = [];

    /// <summary>
    /// Total number of times this rule has matched a process.
    /// Incremented on each successful match. Persisted across restarts.
    /// </summary>
    public long MatchCount { get; init; }

    /// <summary>
    /// UTC timestamp of last successful match. Null if never matched.
    /// </summary>
    public DateTime? LastMatchedAt { get; init; }

    /// <summary>
    /// Validates rule configuration.
    /// </summary>
    public void Validate()
    {
        if (Priority < 1)
            throw new ArgumentException("Priority must be >= 1", nameof(Priority));

        if (Criteria == MatchingCriteria.ProcessNameOnly && ProcessNamePattern is null)
            throw new ArgumentException("ProcessNamePattern required for ProcessNameOnly criteria", nameof(ProcessNamePattern));

        if (Criteria == MatchingCriteria.WindowTitleOnly && WindowTitlePattern is null)
            throw new ArgumentException("WindowTitlePattern required for WindowTitleOnly criteria", nameof(WindowTitlePattern));

        if (Criteria == MatchingCriteria.Both && (ProcessNamePattern is null || WindowTitlePattern is null))
            throw new ArgumentException("Both patterns required for Both criteria", nameof(Criteria));

        if (MatchCount < 0)
            throw new ArgumentException("MatchCount cannot be negative", nameof(MatchCount));
    }

    /// <summary>
    /// Creates a new rule with incremented match count and updated timestamp.
    /// </summary>
    public MeetingRecognitionRule IncrementMatchCount(DateTime matchedAt)
    {
        return this with
        {
            MatchCount = MatchCount + 1,
            LastMatchedAt = matchedAt
        };
    }
}
