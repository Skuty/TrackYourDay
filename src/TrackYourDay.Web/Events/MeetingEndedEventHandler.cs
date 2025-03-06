using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Web.Events
{
    public class MeetingEndedEventHandler : INotificationHandler<MeetingEndedEvent>
    {
        private readonly EventWrapperForComponents eventWrapperForComponents;

        public MeetingEndedEventHandler(EventWrapperForComponents eventWrapperForComponents)
        {
            this.eventWrapperForComponents = eventWrapperForComponents;
        }

        public Task Handle(MeetingEndedEvent notification, CancellationToken cancellationToken)
        {
            this.eventWrapperForComponents.OperationalBarOnMeetingEnded(notification);

            return Task.CompletedTask;
        }
    }
}
