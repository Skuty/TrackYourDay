using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;

/// <summary>
/// Manages persistence of meeting recognition rules using IGenericSettingsService.
/// Rules stored as JSON array under key "MeetingRecognitionRules.v1".
/// Thread-safe if underlying IGenericSettingsService is thread-safe.
/// </summary>
public interface IMeetingRuleRepository
{
    /// <summary>
    /// Retrieves all rules sorted by Priority ascending.
    /// Returns default rule if no rules exist (first-run scenario).
    /// </summary>
    /// <returns>Sorted rule list.</returns>
    IReadOnlyList<MeetingRecognitionRule> GetAllRules();

    /// <summary>
    /// Replaces all rules with provided set. Validates priority uniqueness.
    /// </summary>
    /// <param name="rules">New rule set.</param>
    /// <exception cref="ArgumentException">If priorities are not unique.</exception>
    void SaveRules(IReadOnlyList<MeetingRecognitionRule> rules);

    /// <summary>
    /// Increments match count and updates last matched timestamp for a rule.
    /// </summary>
    /// <param name="ruleId">Target rule ID.</param>
    /// <param name="matchedAt">UTC timestamp of match.</param>
    void IncrementMatchCount(Guid ruleId, DateTime matchedAt);

    /// <summary>
    /// Creates default rule matching legacy ProcessBasedMeetingRecognizingStrategy behavior.
    /// </summary>
    /// <returns>Default rule (priority 1, Polish exclusions).</returns>
    MeetingRecognitionRule CreateDefaultRule();
}
