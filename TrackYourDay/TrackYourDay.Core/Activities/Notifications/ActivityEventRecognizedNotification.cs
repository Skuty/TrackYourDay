using MediatR;

namespace TrackYourDay.Core.Activities.Notifications
{
    public class ActivityEventRecognizedNotification : INotification
    {
        public ActivityEventRecognizedNotification(Guid notificationId, ActivityEvent activityEvent)
        {
            NotificationId = notificationId;
            ActivityEvent = activityEvent;
        }

        public Guid NotificationId { get; }

        public ActivityEvent ActivityEvent { get; }
    }
}
