namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Strategy for discovering ongoing meetings.
/// </summary>
public interface IMeetingDiscoveryStrategy
{
    /// <summary>
    /// Recognizes the current meeting based on system state.
    /// </summary>
    /// <param name="currentMeeting">Currently ongoing meeting, if any.</param>
    /// <param name="currentMatchedRuleId">Rule ID that matched the current meeting, if any.</param>
    /// <returns>Recognized meeting or null if no meeting detected.</returns>
    (StartedMeeting? Meeting, Guid? MatchedRuleId) RecognizeMeeting(StartedMeeting? currentMeeting, Guid? currentMatchedRuleId);
}
