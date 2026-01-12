using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.MAUI.MauiPages;
using TrackYourDay.Web.Services;

namespace TrackYourDay.MAUI.Handlers;

/// <summary>
/// Shows confirmation dialog when meeting end is detected.
/// Stores event data for UI consumption. Window allows minimize and can be moved behind other windows.
/// </summary>
internal sealed class ShowMeetingEndConfirmationDialogHandler 
    : INotificationHandler<MeetingEndConfirmationRequestedEvent>
{
    private readonly ActiveMeetingConfirmationsService _confirmationsService;

    public ShowMeetingEndConfirmationDialogHandler(ActiveMeetingConfirmationsService confirmationsService)
    {
        _confirmationsService = confirmationsService;
    }

    public Task Handle(MeetingEndConfirmationRequestedEvent notification, CancellationToken cancellationToken)
    {
        var meeting = notification.PendingMeeting.Meeting;
        
        _confirmationsService.Store(
            meeting.Guid, 
            meeting.Title, 
            meeting.StartDate, 
            notification.PendingMeeting.DetectedAt);

        var path = $"/MeetingEndConfirmation/{meeting.Guid}";
        MauiPageFactory.OpenWebPageInNewWindow(path, 600, 340, allowMinimize: true, alwaysOnTop: false);

        return Task.CompletedTask;
    }
}
