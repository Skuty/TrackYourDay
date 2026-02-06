using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Web.Events;

public sealed class MeetingCheckPostponedEventHandler : INotificationHandler<MeetingCheckPostponedEvent>
{
    private readonly EventWrapperForComponents _eventWrapperForComponents;

    public MeetingCheckPostponedEventHandler(EventWrapperForComponents eventWrapperForComponents)
    {
        _eventWrapperForComponents = eventWrapperForComponents;
    }

    public Task Handle(MeetingCheckPostponedEvent notification, CancellationToken cancellationToken)
    {
        _eventWrapperForComponents.OperationalBarOnMeetingCheckPostponed(notification);
        return Task.CompletedTask;
    }
}
