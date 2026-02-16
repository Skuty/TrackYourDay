using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

/// <summary>
/// Published when a recognized meeting is canceled (marked as false positive).
/// </summary>
public record class MeetingCanceledEvent(Guid MeetingGuid) : INotification;
