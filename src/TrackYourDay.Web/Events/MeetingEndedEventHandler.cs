using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Web.Events
{
    public class MeetingEndedEventHandler : INotificationHandler<MeetingEndedEvent>
    {
        private readonly EventWrapperForComponents _eventWrapperForComponents;

        public MeetingEndedEventHandler(EventWrapperForComponents eventWrapperForComponents)
        {
            _eventWrapperForComponents = eventWrapperForComponents;
        }

        public Task Handle(MeetingEndedEvent notification, CancellationToken cancellationToken)
        {
            _eventWrapperForComponents.OperationalBarOnMeetingEnded(notification);
            return Task.CompletedTask;
        }
    }
}
