using MediatR;
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
        private readonly MauiPageFactory mauiPageFactory;

        public EndOfWorkDayNotification(
            WorkdayReadModelRepository workdayReadModelRepository,
            MauiPageFactory mauiPageFactory)
        {
            Name = "Powiadomienie o końcu Dnia Pracy";
            this.workdayReadModelRepository = workdayReadModelRepository;
            this.mauiPageFactory = mauiPageFactory;
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

            this.mauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                    "Twój Dzień Pracy się zakończył",
                    $"Zapisz pracę i kończ na dziś :)"));
        }

        private TimeSpan GetTimeLeftToWorkActively()
        {
            return workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today)).TimeLeftToWorkActively;
        }
    }
}