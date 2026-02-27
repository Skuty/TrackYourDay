using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Web.Events;

public class MeetingCanceledEventHandler : INotificationHandler<MeetingCanceledEvent>
{
    private readonly EventWrapperForComponents _eventWrapperForComponents;

    public MeetingCanceledEventHandler(EventWrapperForComponents eventWrapperForComponents)
    {
        _eventWrapperForComponents = eventWrapperForComponents;
    }

    public Task Handle(MeetingCanceledEvent notification, CancellationToken cancellationToken)
    {
        _eventWrapperForComponents.OperationalBarOnMeetingCanceled(notification);
        return Task.CompletedTask;
    }
}
