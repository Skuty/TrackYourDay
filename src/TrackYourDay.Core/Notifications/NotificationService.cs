/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.Core.Notifications
{
    public class NotificationService
    {
        private readonly INotificationFactory notitficationFactory;
        private readonly List<ExecutableNotification> scheduledNotifications;
        private readonly NotificationRepository notificationRepository = null!;

        public NotificationService(INotificationFactory executableNotificationFactory)
        {
            //this.scheduledNotifications = this.notificationRepository.GetAll().ToList();
            scheduledNotifications = new List<ExecutableNotification>();
            this.notitficationFactory = executableNotificationFactory;
            this.ScheduleNotification(this.notitficationFactory.GetDefaultNotification());
        }

        public void ProcessScheduledNotifications()
        {
            foreach (var scheduledNotification in scheduledNotifications)
            {
                if (scheduledNotification.ShouldBeExecuted())
                {
                    scheduledNotification.Execute();
                    //this.scheduledNotifications.Remove(scheduledNotification);
                    //this.notificationRepository.BeginTransaction();
                    //this.notificationRepository.Remove(scheduledNotification);
                    //this.notificationRepository.Add(ExecutedNotification.CreateFrom(scheduledNotification));
                    //this.notificationRepository.EndTransaction();
                }
            }
        }

        private void ScheduleNotification(ExecutableNotification scheduledNotification)
        {
            scheduledNotifications.Add(scheduledNotification);
            //this.notificationRepository.Add(scheduledNotification);
            //TODO: Publish notification
        }
    }
}
