using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

/// <summary>
/// Published when a meeting end is detected and requires user confirmation.
/// </summary>
public record MeetingEndConfirmationRequestedEvent(
    Guid EventId, 
    PendingEndMeeting PendingMeeting) : INotification;
