using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Commands;

/// <summary>
/// Command to confirm a pending meeting end.
/// </summary>
public record ConfirmMeetingEndCommand(Guid MeetingGuid) : IRequest;
