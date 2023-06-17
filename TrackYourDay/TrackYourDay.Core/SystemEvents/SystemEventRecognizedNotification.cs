using MediatR;

namespace TrackYourDay.Core.Events
{
    public class SystemEventRecognizedNotification  : INotification
    {
        public SystemEventRecognizedNotification(Guid notificationId, SystemEvent systemEvent)
        {
            NotificationId = notificationId;
            SystemEvent = systemEvent;
        }

        public Guid NotificationId { get; }

        public SystemEvent SystemEvent { get; }
    }
}
