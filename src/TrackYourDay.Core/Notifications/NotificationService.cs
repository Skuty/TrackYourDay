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
        private List<ExecutableNotification> scheduledNotifications;
        private NotificationRepository notificationRepository = null!;

        public NotificationService()
        {
            this.scheduledNotifications = this.notificationRepository.GetAll().ToList();
        }

        public void ScheduleNotification(ExecutableNotification scheduledNotification) 
        {
            this.scheduledNotifications.Add(scheduledNotification);
            this.notificationRepository.Add(scheduledNotification);
            //TODO: Publish notification
        }

        public void ProcessScheduledNotifications() 
        {
            foreach (var scheduledNotification in this.scheduledNotifications)
            {
                if (scheduledNotification.ShouldBeExecuted())
                {
                    scheduledNotification.Execute();
                    this.scheduledNotifications.Remove(scheduledNotification);
                    //this.notificationRepository.BeginTransaction();
                    this.notificationRepository.Remove(scheduledNotification);
                    this.notificationRepository.Add(ExecutedNotification.CreateFrom(scheduledNotification));
                    //this.notificationRepository.EndTransaction();
                }
            }
        }
    }
}
