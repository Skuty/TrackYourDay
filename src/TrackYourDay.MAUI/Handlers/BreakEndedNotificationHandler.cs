using MediatR;
using TrackYourDay.Core.Breaks.Notifications;

namespace TrackYourDay.MAUI.Handlers
{
    internal class BreakEndedNotificationHandler : INotificationHandler<BreakEndedNotifcation>
    {
        public Task Handle(BreakEndedNotifcation notification, CancellationToken cancellationToken)
        {
            // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread?view=net-maui-8.0
            // Needed to show notification on main thread otherwise it will throw exception
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window breakRevokingPopupWindow = new Window(new PopupDialogPage());
                Application.Current.OpenWindow(breakRevokingPopupWindow);
            });

            return Task.CompletedTask;
        }
    }
}
