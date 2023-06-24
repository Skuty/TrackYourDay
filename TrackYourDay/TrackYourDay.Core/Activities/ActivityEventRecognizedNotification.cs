using MediatR;

namespace TrackYourDay.Core.Activities
{
    public class ActivityEventRecognizedNotification : INotification
    {
        public ActivityEventRecognizedNotification(Guid notificationId, ActivityEvent systemEvent)
        {
            NotificationId = notificationId;
            SystemEvent = systemEvent;
        }

        public Guid NotificationId { get; }

        public ActivityEvent SystemEvent { get; }
    }
}
