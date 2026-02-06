using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

/// <summary>
/// Published when meeting end detection checks are postponed by the user.
/// Signals UI components to update visual state (e.g., operational bar icon color).
/// </summary>
/// <param name="MeetingGuid">GUID of the meeting with postponed checks</param>
/// <param name="PostponedUntil">DateTime when checks will resume</param>
public sealed record MeetingCheckPostponedEvent(Guid MeetingGuid, DateTime PostponedUntil) : INotification;
