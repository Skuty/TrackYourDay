using TrackYourDay.Core.Workdays;
using TrackYourDay.MAUI.MauiPages;
using TrackYourDay.MAUI.Notifications;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.BackgroundJobs
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

        public ExecutableNotification GetByNotificationName(string name)
        {
            return name switch
            {
                "Powiadomienie o zbliżającym się końcu Dnia Pracy"
                    => new EndOfWorkDayNearNotification(TimeSpan.FromMinutes(45), workdayReadModelRepository),
                "Powiadomienie o końcu Dnia Pracy"
                    => new EndOfWorkDayNotification(workdayReadModelRepository),
                _ => throw new NotImplementedException($"Notification with supplied name: '{name}' is not implemented."),
            };
        }
    }

    public class EndOfWorkDayNearNotification : ExecutableNotification
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;
        private readonly TimeSpan timeLeftToWorkActivelyConfigValue;

        public EndOfWorkDayNearNotification(
            TimeSpan timeLeftToWorkActivelyConfigValue,
            WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.Name = "Powiadomienie o zbliżającym się końcu Dnia Pracy";
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

    public class EndOfWorkDayNotification : ExecutableNotification
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public EndOfWorkDayNotification(WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.Name = "Powiadomienie o końcu Dnia Pracy";
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
            return this.workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today)).TimeLeftToWorkActively;
        }
    }
}