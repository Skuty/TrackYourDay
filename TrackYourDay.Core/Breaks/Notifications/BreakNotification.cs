using MediatR;

namespace TrackYourDay.Core.Breaks.Notifications
{
    public class BreakEndedNotifcation : INotification
    {
        public BreakEndedNotifcation(Guid notificationId, EndedBreak endedBreak)
        {
            NotificationId = notificationId;
            EndedBreak = endedBreak;
        }

        public Guid NotificationId { get; }

        public EndedBreak EndedBreak { get; }
    }

    public class BreakStartedNotifcation : INotification
    {
        public BreakStartedNotifcation(Guid notificationId, StartedBreak startedBreak)
        {
            NotificationId = notificationId;
            StartedBreak = startedBreak;
        }

        public Guid NotificationId { get; }

        public StartedBreak StartedBreak { get; }
    }
}
