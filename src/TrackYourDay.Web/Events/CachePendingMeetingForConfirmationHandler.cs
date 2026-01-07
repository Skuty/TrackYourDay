using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Web.Services;

namespace TrackYourDay.Web.Events;

/// <summary>
/// Caches pending meetings for confirmation dialogs.
/// </summary>
internal sealed class CachePendingMeetingForConfirmationHandler 
    : INotificationHandler<MeetingEndConfirmationRequestedEvent>
{
    private readonly IRecentMeetingsCache _cache;

    public CachePendingMeetingForConfirmationHandler(IRecentMeetingsCache cache)
    {
        _cache = cache;
    }

    public Task Handle(MeetingEndConfirmationRequestedEvent notification, CancellationToken cancellationToken)
    {
        _cache.AddPending(notification.PendingMeeting);
        return Task.CompletedTask;
    }
}
