using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

/// <summary>
/// Published when user manually requests to end a meeting from UI.
/// </summary>
public record RequestManualMeetingEndEvent : INotification;
