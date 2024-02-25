using MediatR;

namespace TrackYourDay.Tests.Old.Old.Activities.Notifications
{
    public class ActivityEventRecognizedEvent : INotification
    {
        public ActivityEventRecognizedEvent(Guid eventId, ActivityEvent activityEvent)
        {
            EventId = eventId;
            ActivityEvent = activityEvent;
        }

        public Guid EventId { get; }

        public ActivityEvent ActivityEvent { get; }
    }
}
