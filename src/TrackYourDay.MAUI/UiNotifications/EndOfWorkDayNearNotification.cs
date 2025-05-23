﻿using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.Notifications;
using TrackYourDay.MAUI.BackgroundJobs;
using TrackYourDay.MAUI.MauiPages;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.UiNotifications
{

    public class EndOfWorkDayNearNotification : ExecutableNotification
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;
        private readonly MauiPageFactory mauiPageFactory;
        private readonly TimeSpan timeLeftToWorkActivelyConfigValue;

        public EndOfWorkDayNearNotification(
            TimeSpan timeLeftToWorkActivelyConfigValue,
            WorkdayReadModelRepository workdayReadModelRepository,
            MauiPageFactory mauiPageFactory) : base()
        {
            this.IsEnabled = true;
            Name = "Powiadomienie o zbliżającym się końcu Dnia Pracy";
            this.timeLeftToWorkActivelyConfigValue = TimeSpan.FromMinutes(45);
            this.workdayReadModelRepository = workdayReadModelRepository;
            this.mauiPageFactory = mauiPageFactory;
        }

        public override bool ShouldBeExecuted()
        {
            var shouldBeExecuted = base.ShouldBeExecuted();
            var timeLeftToWorkActively = GetTimeLeftToWorkActively();
            return
                shouldBeExecuted && timeLeftToWorkActively < timeLeftToWorkActivelyConfigValue;
        }

        public override void Execute()
        {
            base.Execute();

            this.mauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                "Zbliża się koniec Twojego Dnia Pracy",
                $"Pozostało Ci {(int)GetTimeLeftToWorkActively().TotalMinutes} minut Aktywnej Pracy"));
        }

        private TimeSpan GetTimeLeftToWorkActively()
        {
            var workday = workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today));
            return workday.TimeLeftToWorkActively;
        }
    }
}