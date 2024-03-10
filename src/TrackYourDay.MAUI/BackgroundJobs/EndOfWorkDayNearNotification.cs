using TrackYourDay.Core.Workdays;
using TrackYourDay.MAUI.MauiPages;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.Core.Notifications
{
    public class EndOfWorkDayNearNotification : ExecutableNotification
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;
        private readonly TimeSpan timeLeftToWorkActivelyConfigValue;

        public EndOfWorkDayNearNotification(TimeSpan timeLeftToWorkActivelyConfigValue, WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.timeLeftToWorkActivelyConfigValue = TimeSpan.FromMinutes(45);
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public override bool ShouldBeExecuted()
        {
            return 
                base.ShouldBeExecuted()
                && this.GetTimeLeftToWorkActively() < this.timeLeftToWorkActivelyConfigValue;
        }

        public override void Execute()
        {
            base.Execute();

            MauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                "Zbliża się koniec Twojego Dnia Pracy",
                $"Pozostało Ci {(int)this.GetTimeLeftToWorkActively().TotalMinutes} minut Aktywnej Pracy"));
        }

        private TimeSpan GetTimeLeftToWorkActively()
        {
            return this.workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today)).TimeLeftToWorkActively;
        }
    }

    public class EndOfWorkDayNotification : ExecutableNotification
    {
        public EndOfWorkDayNotification()
        {
            
        }

        public override bool ShouldBeExecuted()
        {
            return
                base.ShouldBeExecuted()
                && this.GetTimeLeftToWorkActively() <= TimeSpan.Zero;
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
            return TimeSpan.FromMinutes(30);
        }
    }

    public class CountOfBreaksExceededNotification : ExecutableNotification
    {
        private readonly int countOfBreaksExceededConfigValue;

        public CountOfBreaksExceededNotification(int countOfBreaksExceededConfigValue)
        {
            this.countOfBreaksExceededConfigValue = 12;
        }

        public override bool ShouldBeExecuted()
        {
            return
                base.ShouldBeExecuted()
                && this.GetCountOfBreaksAlreadyTaken() > countOfBreaksExceededConfigValue;
        }

        public override void Execute()
        {
            base.Execute();

            MauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                "Masz sporo przerw lub spotkań" +
                ", czy to Cię nie rozprasza?",
                $"Co najmniej {this.countOfBreaksExceededConfigValue} przerw."));
        }

        private int GetCountOfBreaksAlreadyTaken()
        {
            return 15;
        }
    }
}