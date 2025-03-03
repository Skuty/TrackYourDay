using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents
{
    public record class MeetingStartedEvent(Guid Guid, StartedMeeting EndedMeeting) : INotification;
}
