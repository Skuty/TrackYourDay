using TrackYourDay.Core.Workdays;
using TrackYourDay.MAUI.Notifications.Concrete;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.Notifications
{
    public class ExecutableNotificationFactory
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public ExecutableNotificationFactory(WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public ExecutableNotification GetNearWorkdayEnd()
        {
            return new EndOfWorkDayNearNotification(TimeSpan.FromMinutes(45), workdayReadModelRepository);
        }

        public ExecutableNotification GetWorkDayEnd()
        {
            return new EndOfWorkDayNotification(workdayReadModelRepository);
        }

        public ExecutableNotification GetTipForDay()
        {
            return new TipForDayNotification();
        }
    }
}