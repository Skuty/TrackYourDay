using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents
{
    // Replace with native events without internal objects
    public record class MeetingEndedEvent(Guid Guid, EndedMeeting EndedMeeting) : INotification;
}
