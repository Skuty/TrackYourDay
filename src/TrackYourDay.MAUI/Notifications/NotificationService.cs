using TrackYourDay.MAUI.BackgroundJobs;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.Notifications
{
    public class NotificationService
    {
        private readonly ExecutableNotificationFactory executableNotificationFactory;
        private readonly List<ExecutableNotification> scheduledNotifications;
        private readonly NotificationRepository notificationRepository = null!;

        public NotificationService(ExecutableNotificationFactory executableNotificationFactory)
        {
            //this.scheduledNotifications = this.notificationRepository.GetAll().ToList();
            this.scheduledNotifications = new List<ExecutableNotification>();
            this.executableNotificationFactory = executableNotificationFactory;
            this.ScheduleNotification(this.executableNotificationFactory.GetNearWorkdayEnd());
            this.ScheduleNotification(this.executableNotificationFactory.GetWorkDayEnd());
        }

        public void ProcessScheduledNotifications()
        {
            foreach (var scheduledNotification in this.scheduledNotifications)
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
            this.scheduledNotifications.Add(scheduledNotification);
            //this.notificationRepository.Add(scheduledNotification);
            //TODO: Publish notification
        }
    }
}
