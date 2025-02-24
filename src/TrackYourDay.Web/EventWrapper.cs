using MediatR;
using TrackYourDay.Core.Workdays.Events;

namespace TrackYourDay.Web
{
    public class EventWrapper : INotificationHandler<WorkdayUpdatedEvent>
    {
        public event Action<WorkdayUpdatedEvent>? OnWorkdayUpdated;

        public Task Handle(WorkdayUpdatedEvent notification, CancellationToken cancellationToken)
        {
            OnWorkdayUpdated?.Invoke(notification);
            return Task.CompletedTask;
        }
    }
}
