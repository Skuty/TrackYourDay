using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents
{
    public record class MeetingEndedEvent(Guid Guid, EndedMeeting EndedMeeting) : INotification;
}
