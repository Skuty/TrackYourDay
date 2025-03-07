using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Web.Events
{
    public class MeetingStartedEventHandler : INotificationHandler<MeetingStartedEvent>
    {
        private readonly EventWrapperForComponents eventWrapperForComponents;

        public MeetingStartedEventHandler(EventWrapperForComponents eventWrapperForComponents)
        {
            this.eventWrapperForComponents = eventWrapperForComponents;
        }

        public Task Handle(MeetingStartedEvent notification, CancellationToken cancellationToken)
        {
            this.eventWrapperForComponents.OperationalBarOnMeetingStarted(notification);

            return Task.CompletedTask;
        }
    }
}
