using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.MAUI.MauiPages;
using TrackYourDay.Web.Services;

namespace TrackYourDay.MAUI.Handlers;

/// <summary>
/// Shows confirmation dialog when meeting end is detected and caches pending meeting.
/// </summary>
internal sealed class ShowMeetingEndConfirmationDialogHandler 
    : INotificationHandler<MeetingEndConfirmationRequestedEvent>
{
    private readonly IRecentMeetingsCache _cache;

    public ShowMeetingEndConfirmationDialogHandler(IRecentMeetingsCache cache)
    {
        _cache = cache;
    }

    public Task Handle(MeetingEndConfirmationRequestedEvent notification, CancellationToken cancellationToken)
    {
        _cache.AddPending(notification.PendingMeeting);
        
        var path = $"/MeetingEndConfirmation/{notification.PendingMeeting.Meeting.Guid}";
        MauiPageFactory.OpenWebPageInNewWindow(path, 500, 300);

        return Task.CompletedTask;
    }
}
