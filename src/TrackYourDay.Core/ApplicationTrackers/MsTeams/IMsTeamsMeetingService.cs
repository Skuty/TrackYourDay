namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Service for managing MS Teams meeting lifecycle and confirmations.
/// </summary>
public interface IMsTeamsMeetingService
{
    /// <summary>
    /// Confirms that a pending meeting has ended.
    /// Publishes MeetingEndedEvent if successful.
    /// </summary>
    Task ConfirmMeetingEndAsync(Guid meetingGuid, CancellationToken cancellationToken = default);

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
