using System.Collections.Concurrent;
using MediatR;
using TrackYourDay.Core.Insights.Workdays.Events;

namespace TrackYourDay.Core.Insights.Workdays
{
    public class WorkdayReadModelRepository
    {
        private readonly ConcurrentDictionary<DateOnly, Workday> workdays;
        private readonly DateTime instanceCreateDate;
        private readonly IMediator mediator;

        public WorkdayReadModelRepository(IMediator mediator)
        {
            instanceCreateDate = DateTime.Now;
            workdays = new ConcurrentDictionary<DateOnly, Workday>();
            this.mediator = mediator;
        }

        public Workday Get(DateOnly date)
        {
            workdays.GetOrAdd(date, Workday.CreateEmpty(date));

            return workdays[date];

            // TODO: Here should be workday with definition based on default settings, not raw like this
            //TODO: This was hotfixefd but it shouldnt be like it throw new Exception("Result is null here on raw launch. This should be fixed, probably by reyturning here empty object.");
        }

        public void AddOrUpdate(Workday workday)
        {
            if (workdays.TryGetValue(workday.Date, out Workday existingWorkday))
            {
                workdays.TryUpdate(workday.Date, workday, existingWorkday);
            }
            else
            {
                workdays[workday.Date] = workday;
            }

            mediator.Publish(new WorkdayUpdatedEvent(Guid.NewGuid(), workday));
        }
    }
}