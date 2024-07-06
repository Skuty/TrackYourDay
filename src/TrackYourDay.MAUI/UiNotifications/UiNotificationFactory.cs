using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Workdays;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.UiNotifications
{
    public class UiNotificationFactory : INotificationFactory
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public UiNotificationFactory(WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public ExecutableNotification GetNotificationByName(string name)
        {
            if (name == "EndOfWorkdayNear")
            {
                return new EndOfWorkDayNearNotification(TimeSpan.FromMinutes(45), workdayReadModelRepository);
            }

            if (name == "EndOfWorkday")
            {
                return new EndOfWorkDayNotification(workdayReadModelRepository);
            }

            throw new NotImplementedException();
        }

        public ExecutableNotification GetDefaultNotification()
        {
            return new TipForDayNotification();
        }
    }
}