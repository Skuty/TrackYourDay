

namespace TrackYourDay.Core.Notifications
{
    public class NotificationRepository
    {
        private List<ExecutableNotification> scheduledNotifications;
        private List<ExecutedNotification> executedNotifications;

        public NotificationRepository() 
        {
            this.scheduledNotifications = new List<ExecutableNotification>();
            this.executedNotifications = new List<ExecutedNotification>();
        }

        internal void Add(ExecutedNotification executedNotification)
        {
            this.executedNotifications.Add(executedNotification);
        }

        internal void Add(ExecutableNotification scheduledNotification)
        {
            this.scheduledNotifications.Add(scheduledNotification);
        }

        internal IEnumerable<ExecutableNotification>? GetAll()
        {
            return this.scheduledNotifications; 
        }

        internal void Remove(ExecutableNotification scheduledNotification)
        {
            this.scheduledNotifications.Remove(scheduledNotification);
        }
    }
}