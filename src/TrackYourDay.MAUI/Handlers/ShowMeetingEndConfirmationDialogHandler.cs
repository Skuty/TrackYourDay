using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Commands;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.MAUI.MauiPages;

namespace TrackYourDay.MAUI.Handlers;

/// <summary>
/// Shows confirmation dialog when meeting end is detected.
/// </summary>
internal sealed class ShowMeetingEndConfirmationDialogHandler 
    : INotificationHandler<MeetingEndConfirmationRequestedEvent>
{
    public Task Handle(MeetingEndConfirmationRequestedEvent notification, CancellationToken cancellationToken)
    {
        var path = $"/MeetingEndConfirmation/{notification.PendingMeeting.Meeting.Guid}";
        MauiPageFactory.OpenWebPageInNewWindow(path, 500, 300);

        return Task.CompletedTask;
    }
}
