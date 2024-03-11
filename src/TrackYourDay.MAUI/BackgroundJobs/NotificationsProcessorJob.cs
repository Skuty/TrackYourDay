using Quartz;
using TrackYourDay.Core.Notifications;

namespace TrackYourDay.MAUI.BackgroundJobs
{
    public class NotificationsProcessorJob : IJob
    {
        private readonly NotificationService notificationService;

        public NotificationsProcessorJob(NotificationService notificationService)
        {
            this.notificationService = notificationService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            this.notificationService.ProcessScheduledNotifications();

            return Task.CompletedTask;
        }
    }
}
