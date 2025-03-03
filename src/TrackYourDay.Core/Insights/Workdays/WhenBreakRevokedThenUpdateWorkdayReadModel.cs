using MediatR;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;

namespace TrackYourDay.Core.Insights.Workdays
{
    internal class WhenBreakRevokedThenUpdateWorkdayReadModel 
        : INotificationHandler<BreakRevokedEvent>
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public WhenBreakRevokedThenUpdateWorkdayReadModel(WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public Task Handle(BreakRevokedEvent notification, CancellationToken cancellationToken)
        {
            var workday = workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today));
            var newWorkday = workday.Include(notification.RevokedBreak);
            workdayReadModelRepository.AddOrUpdate(newWorkday);

            return Task.CompletedTask;
        }
    }
}
