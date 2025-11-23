using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.MAUI.MauiPages;

namespace TrackYourDay.MAUI.Handlers
{
    internal class ShowUINotificationWhenMeetingEndedEventHandler : INotificationHandler<MeetingEndedEvent>
    {
        public Task Handle(MeetingEndedEvent _event, CancellationToken cancellationToken)
        {
            var path = $"/MeetingDescription/{_event.EndedMeeting.Guid}";
            MauiPageFactory.OpenWebPageInNewWindow(path, 500, 400);

            return Task.CompletedTask;
        }
    }
}
