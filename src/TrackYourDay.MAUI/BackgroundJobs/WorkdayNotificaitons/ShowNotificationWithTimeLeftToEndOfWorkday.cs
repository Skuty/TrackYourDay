using Microsoft.UI;
using Quartz;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Settings;
using TrackYourDay.MAUI.MauiPages;
using WinRT.Interop;

namespace TrackYourDay.MAUI.BackgroundJobs.WorkdayNotificaitons
{
    internal class ShowNotificationWithTimeLeftToEndOfWorkday : IJob
    {
        private readonly ISettingsSet settingsSet;
        private readonly ActivityTracker activityTracker;
        private readonly BreakTracker breakTracker;

        public ShowNotificationWithTimeLeftToEndOfWorkday(ISettingsSet settingsSet, ActivityTracker activityTracker, BreakTracker breakTracker)
        {
            this.settingsSet = settingsSet;
            this.activityTracker = activityTracker;
            this.breakTracker = breakTracker;
        }

        public Task Execute(IJobExecutionContext context)
        {
            var workday = Workday.CreateBasedOn(this.settingsSet.WorkdayDefinition, this.activityTracker.GetEndedActivities(), this.breakTracker.GetEndedBreaks());

            if (workday.TimeLeftToWorkActively < TimeSpan.FromMinutes(60))
            {

                this.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                        "Zbliża się koniec Twojego Dnia Pracy",
                        $"Ppozostało Ci {(int)workday.TimeLeftToWorkActively.TotalMinutes} minut Aktywnej Pracy"));
            } else if (workday.TimeLeftToWorkActively < TimeSpan.FromMinutes(10))
            {
                this.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                        "Twój Dzień Pracy się zakończył",
                        $"Zapisz pracę i kończ na dziś :)"));
            }

            return Task.CompletedTask;
        }

        private void OpenSimpleNotificationPageInNewWindow(SimpleNotificationViewModel simpleNotificationViewModel)
        {
            // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread?view=net-maui-8.0
            // Needed to show notification on main thread otherwise it will throw exception
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window breakRevokingPopupWindow = new Window(new SimpleNotificationPage(simpleNotificationViewModel));
                breakRevokingPopupWindow.Title = $"Track Your Day - {simpleNotificationViewModel.Title}";
                breakRevokingPopupWindow.Width = 600;
                breakRevokingPopupWindow.Height = 170;
                Application.Current.OpenWindow(breakRevokingPopupWindow);

                var localWindow = (breakRevokingPopupWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window);

                localWindow.ExtendsContentIntoTitleBar = false;
                var handle = WindowNative.GetWindowHandle(localWindow);
                var id = Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                switch (appWindow.Presenter)
                {
                    case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                        overlappedPresenter.IsResizable = false;
                        overlappedPresenter.IsMaximizable = false;
                        overlappedPresenter.IsMinimizable = false;
                        overlappedPresenter.IsAlwaysOnTop = true;

                        //overlappedPresenter.SetBorderAndTitleBar(true, false);
                        overlappedPresenter.Restore();
                        break;
                }
            });
        }
    }
}
