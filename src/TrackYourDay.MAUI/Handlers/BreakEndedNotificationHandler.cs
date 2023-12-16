using MediatR;
using Microsoft.UI;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Breaks.Notifications;
using WinRT.Interop;

namespace TrackYourDay.MAUI.Handlers
{
    internal class BreakEndedNotificationHandler : INotificationHandler<BreakEndedNotifcation>
    {
        private readonly BreakTracker breakTracker;

        public BreakEndedNotificationHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }

        public Task Handle(BreakEndedNotifcation notification, CancellationToken cancellationToken)
        {
            this.OpenDialogPageInNewWindow(notification.EndedBreak.BreakGuid);

            return Task.CompletedTask;
        }

        private void OpenDialogPageInNewWindow(Guid breakGuid)
        {
            // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread?view=net-maui-8.0
            // Needed to show notification on main thread otherwise it will throw exception
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window breakRevokingPopupWindow = new Window(new BreakRevokePage(breakGuid, this.breakTracker));
                breakRevokingPopupWindow.Title = $"Track Your Day - Revoking Break {breakGuid}";
                breakRevokingPopupWindow.Width = 400;
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
