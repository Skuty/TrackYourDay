using MediatR;
using Microsoft.UI;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using WinRT.Interop;

namespace TrackYourDay.MAUI.Handlers
{
    internal class ShowUINotificationWhenMeetingEndedEventHandler : INotificationHandler<MeetingEndedEvent>
    {
        private readonly MsTeamsMeetingTracker meetingTracker;

        public ShowUINotificationWhenMeetingEndedEventHandler(MsTeamsMeetingTracker meetingTracker)
        {
            this.meetingTracker = meetingTracker;
        }

        public Task Handle(MeetingEndedEvent _event, CancellationToken cancellationToken)
        {
            this.OpenDialogPageInNewWindow(_event.EndedMeeting.Guid);

            return Task.CompletedTask;
        }

        private void OpenDialogPageInNewWindow(Guid meetingGuid)
        {
            // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread?view=net-maui-8.0
            // Needed to show notification on main thread otherwise it will throw exception
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window meetingDescriptionPopupWindow = new Window(new MeetingDescriptionPage(meetingGuid, this.meetingTracker));
                meetingDescriptionPopupWindow.Title = $"Track Your Day - Meeting Description {meetingGuid}";
                meetingDescriptionPopupWindow.Width = 500;
                meetingDescriptionPopupWindow.Height = 300;
                Application.Current.OpenWindow(meetingDescriptionPopupWindow);

                var localWindow = (meetingDescriptionPopupWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window);

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

                        overlappedPresenter.Restore();
                        break;
                }
            });
        }
    }
}
