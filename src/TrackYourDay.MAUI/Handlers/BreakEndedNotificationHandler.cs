using MediatR;
using Microsoft.UI;
using TrackYourDay.Core.Breaks.Notifications;
using WinRT.Interop;

namespace TrackYourDay.MAUI.Handlers
{
    internal class BreakEndedNotificationHandler : INotificationHandler<BreakEndedNotifcation>
    {
        public Task Handle(BreakEndedNotifcation notification, CancellationToken cancellationToken)
        {
            this.OpenDialogPageInNewWindow($"/breakRevoke/{Guid.NewGuid()}");

            return Task.CompletedTask;
        }

        private void OpenDialogPageInNewWindow(string path)
        {
            // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread?view=net-maui-8.0
            // Needed to show notification on main thread otherwise it will throw exception
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window breakRevokingPopupWindow = new Window(new DialogPage(path));
                breakRevokingPopupWindow.Title = "Break Revoking";
                breakRevokingPopupWindow.Width = 800;
                breakRevokingPopupWindow.Height = 600;
                Application.Current.OpenWindow(breakRevokingPopupWindow);

                var localWindow = (breakRevokingPopupWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window);

                localWindow.ExtendsContentIntoTitleBar = false;
                var handle = WindowNative.GetWindowHandle(localWindow);
                var id = Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                switch (appWindow.Presenter)
                {
                    case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                        overlappedPresenter.IsMaximizable = false;
                        overlappedPresenter.IsMinimizable = false;
                        overlappedPresenter.IsAlwaysOnTop = true;

                        overlappedPresenter.SetBorderAndTitleBar(false, false);
                        overlappedPresenter.Restore();
                        break;
                }
            });
        }
    }
}
