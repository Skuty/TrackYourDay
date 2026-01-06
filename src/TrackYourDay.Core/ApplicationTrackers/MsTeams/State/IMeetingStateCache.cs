namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

/// <summary>
/// Thread-safe cache for ongoing meeting state.
/// Singleton lifecycle—survives job scope disposal.
/// </summary>
public interface IMeetingStateCache
{
    /// <summary>
    /// Gets the currently ongoing meeting, or null if no meeting active.
    /// Thread-safe.
    /// </summary>
    StartedMeeting? GetOngoingMeeting();

    /// <summary>
    /// Sets the ongoing meeting. Overwrites previous value.
    /// Thread-safe.
    /// </summary>
    void SetOngoingMeeting(StartedMeeting? meeting);

    /// <summary>
    /// Gets the rule ID that matched the current ongoing meeting.
    /// Used for meeting continuity checks.
    /// Thread-safe.
    /// </summary>
    Guid? GetMatchedRuleId();

    /// <summary>
    /// Sets the rule ID for the ongoing meeting.
    /// Thread-safe.
    /// </summary>
    void SetMatchedRuleId(Guid? ruleId);

    /// <summary>
    /// Atomically clears both meeting and rule ID.
    /// Thread-safe.
    /// </summary>
    void ClearMeetingState();
}
