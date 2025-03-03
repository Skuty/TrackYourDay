using MediatR;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;

namespace TrackYourDay.Core.Insights.Workdays
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
            var workday = workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today));
            var newWorkday = workday.Include(notification.EndedBreak);
            workdayReadModelRepository.AddOrUpdate(newWorkday);

            return Task.CompletedTask;
        }
    }
}
