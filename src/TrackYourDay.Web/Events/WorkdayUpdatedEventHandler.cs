using MediatR;
using TrackYourDay.Core.Insights.Workdays.Events;

namespace TrackYourDay.Web.Events
{
    public class WorkdayUpdatedEventHandler : INotificationHandler<WorkdayUpdatedEvent>
    {
        private readonly EventWrapperForComponents eventWrapperForComponents;

        public WorkdayUpdatedEventHandler(EventWrapperForComponents eventWrapperForComponents)
        {
            this.eventWrapperForComponents = eventWrapperForComponents;
        }

        public Task Handle(WorkdayUpdatedEvent notification, CancellationToken cancellationToken)
        {
            this.eventWrapperForComponents.OperationalBarOnWorkdayUpdated(notification);

            return Task.CompletedTask;
        }
    }
}
