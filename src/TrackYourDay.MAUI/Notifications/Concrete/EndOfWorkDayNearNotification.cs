using TrackYourDay.Core.Workdays;
using TrackYourDay.MAUI.BackgroundJobs;
using TrackYourDay.MAUI.MauiPages;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.Notifications.Concrete
{

    public class EndOfWorkDayNearNotification : ExecutableNotification
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;
        private readonly TimeSpan timeLeftToWorkActivelyConfigValue;

        public EndOfWorkDayNearNotification(
            TimeSpan timeLeftToWorkActivelyConfigValue,
            WorkdayReadModelRepository workdayReadModelRepository)
        {
            Name = "Powiadomienie o zbliżającym się końcu Dnia Pracy";
            this.timeLeftToWorkActivelyConfigValue = TimeSpan.FromMinutes(45);
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public override bool ShouldBeExecuted()
        {
            return
                base.ShouldBeExecuted()
                && GetTimeLeftToWorkActively() < timeLeftToWorkActivelyConfigValue;
        }

        public override void Execute()
        {
            base.Execute();

            MauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                "Zbliża się koniec Twojego Dnia Pracy",
                $"Pozostało Ci {(int)GetTimeLeftToWorkActively().TotalMinutes} minut Aktywnej Pracy"));
        }

        private TimeSpan GetTimeLeftToWorkActively()
        {
            return workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today)).TimeLeftToWorkActively;
        }
    }
}