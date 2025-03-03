using MediatR;
using TrackYourDay.Core.SystemTrackers.Events;

namespace TrackYourDay.Core.Insights.Workdays
{
    internal class WhenActivityEndedThenUpdateWorkdayReadModel
        : INotificationHandler<PeriodicActivityEndedEvent>
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public WhenActivityEndedThenUpdateWorkdayReadModel(WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public Task Handle(PeriodicActivityEndedEvent notification, CancellationToken cancellationToken)
        {
            var workday = workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today));
            var newWorkday = workday.Include(notification.EndedActivity);
            workdayReadModelRepository.AddOrUpdate(newWorkday);

            return Task.CompletedTask;
        }
    }
}
