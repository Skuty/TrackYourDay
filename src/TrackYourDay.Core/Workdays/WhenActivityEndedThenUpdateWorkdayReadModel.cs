using MediatR;
using TrackYourDay.Core.Activities.Events;

namespace TrackYourDay.Core.Workdays
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
            var workday = this.workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today));
            var newWorkday = workday.Include(notification.EndedActivity);
            this.workdayReadModelRepository.AddOrUpdate(newWorkday);

            return Task.CompletedTask;
        }
    }
}
