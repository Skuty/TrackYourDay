using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Workdays;
using TrackYourDay.MAUI.MauiPages;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.UiNotifications
{
    public class EndOfWorkDayNotification : ExecutableNotification
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public EndOfWorkDayNotification(WorkdayReadModelRepository workdayReadModelRepository)
        {
            Name = "Powiadomienie o końcu Dnia Pracy";
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public override bool ShouldBeExecuted()
        {
            return
                base.ShouldBeExecuted()
                && GetTimeLeftToWorkActively() <= TimeSpan.Zero;
        }

        public override void Execute()
        {
            base.Execute();

            MauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                    "Twój Dzień Pracy się zakończył",
                    $"Zapisz pracę i kończ na dziś :)"));
        }

        private TimeSpan GetTimeLeftToWorkActively()
        {
            return workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today)).TimeLeftToWorkActively;
        }
    }
}