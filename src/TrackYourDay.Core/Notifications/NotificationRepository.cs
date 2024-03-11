namespace TrackYourDay.Core.Notifications
{
    public class NotificationRepository
    {
        private List<ExecutableNotification> scheduledNotifications;
        private List<ExecutedNotification> executedNotifications;

        public NotificationRepository()
        {
            scheduledNotifications = new List<ExecutableNotification>();
            executedNotifications = new List<ExecutedNotification>();
        }

        internal void Add(ExecutedNotification executedNotification)
        {
            executedNotifications.Add(executedNotification);
        }

        internal void Add(ExecutableNotification scheduledNotification)
        {
            scheduledNotifications.Add(scheduledNotification);
        }

        internal IEnumerable<ExecutableNotification> GetAll()
        {
            return scheduledNotifications;
        }

        internal void Remove(ExecutableNotification scheduledNotification)
        {
            scheduledNotifications.Remove(scheduledNotification);
        }
    }
}