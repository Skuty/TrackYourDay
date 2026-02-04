using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

/// <summary>
/// Published when a meeting end is detected and requires user confirmation.
/// Contains all meeting data to avoid UI querying tracker state.
/// </summary>
public record MeetingEndConfirmationRequestedEvent(
    Guid MeetingGuid,
    string MeetingTitle,
    DateTime StartTime) : INotification;
