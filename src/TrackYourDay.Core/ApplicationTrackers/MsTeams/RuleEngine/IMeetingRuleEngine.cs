using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

/// <summary>
/// Evaluates meeting recognition rules against running processes.
/// NOT thread-safe—caller must ensure synchronization if rules mutate during evaluation.
/// </summary>
public interface IMeetingRuleEngine
{
    /// <summary>
    /// Evaluates all rules in priority order against provided processes.
    /// Returns first matching process or null if no matches.
    /// </summary>
    /// <param name="rules">Rules sorted by Priority ascending.</param>
    /// <param name="processes">Candidate processes to evaluate.</param>
    /// <param name="ongoingMeetingRuleId">Rule ID of currently tracked meeting (for continuity check).</param>
    /// <returns>Matched process metadata or null.</returns>
    /// <exception cref="ArgumentNullException">If rules or processes is null.</exception>
    MeetingMatch? EvaluateRules(
        IReadOnlyList<MeetingRecognitionRule> rules,
        IEnumerable<ProcessInfo> processes,
        Guid? ongoingMeetingRuleId);
}
