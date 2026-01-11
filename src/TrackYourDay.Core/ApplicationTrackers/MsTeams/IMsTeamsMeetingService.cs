namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Service for managing MS Teams meeting lifecycle and confirmations.
/// Singleton tracker with blocking state machine.
/// </summary>
public interface IMsTeamsMeetingService
{
    /// <summary>
    /// Confirms that a pending meeting has ended.
    /// Publishes MeetingEndedEvent if successful.
    /// </summary>
    /// <param name="meetingGuid">GUID of the meeting to confirm.</param>
    /// <param name="customDescription">Optional custom description that overrides meeting title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConfirmMeetingEndAsync(Guid meetingGuid, string? customDescription = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending end confirmation and returns meeting to ACTIVE state.
    /// </summary>
    /// <param name="meetingGuid">GUID of the meeting to cancel pending end.</param>
    void CancelPendingEnd(Guid meetingGuid);

    /// <summary>
    /// Gets the currently ongoing meeting, or null if none active.
    /// </summary>
    StartedMeeting? GetOngoingMeeting();

    /// <summary>
    /// Gets the pending end meeting awaiting confirmation.
    /// </summary>
    PendingEndMeeting? GetPendingEndMeeting();

    /// <summary>
    /// Gets all meetings that have ended since last retrieval.
    /// </summary>
    IReadOnlyCollection<EndedMeeting> GetEndedMeetings();
}
