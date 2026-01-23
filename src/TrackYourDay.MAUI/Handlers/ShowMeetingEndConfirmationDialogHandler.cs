using MediatR;
using System.Web;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.MAUI.MauiPages;

namespace TrackYourDay.MAUI.Handlers;

/// <summary>
/// Shows confirmation dialog when meeting end is detected.
/// Passes meeting data via URL parameters. Window allows minimize and can be moved behind other windows.
/// </summary>
internal sealed class ShowMeetingEndConfirmationDialogHandler 
    : INotificationHandler<MeetingEndConfirmationRequestedEvent>
{
    public Task Handle(MeetingEndConfirmationRequestedEvent notification, CancellationToken cancellationToken)
    {
        var encodedTitle = HttpUtility.UrlEncode(notification.MeetingTitle);
        var path = $"/MeetingEndConfirmation/{notification.MeetingGuid}?title={encodedTitle}";
        MauiPageFactory.OpenWebPageInNewWindow(path, 600, 480, allowMinimize: true, alwaysOnTop: false);

        return Task.CompletedTask;
    }
}
