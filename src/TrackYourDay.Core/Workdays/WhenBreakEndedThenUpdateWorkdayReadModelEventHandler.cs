using MediatR;
using TrackYourDay.Core.Breaks.Events;

namespace TrackYourDay.Core.Workdays
{
    internal class WhenBreakEndedThenUpdateWorkdayReadModelEventHandler 
        : INotificationHandler<BreakEndedEvent>
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public WhenBreakEndedThenUpdateWorkdayReadModelEventHandler(WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public Task Handle(BreakEndedEvent notification, CancellationToken cancellationToken)
        {
            var workday = this.workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today));
            workday.Include(notification);
            this.workdayReadModelRepository.AddOrUpdate(workday);

            return Task.CompletedTask
        }
    }
}
