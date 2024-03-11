using MediatR;
using TrackYourDay.Core.Activities.Events;

namespace TrackYourDay.Core.Workdays
{
    public class WorkdayReadModelRepository
    {
        private readonly List<Workday> workdays;

        public WorkdayReadModelRepository() 
        { 
            this.workdays = new List<Workday>();
        }

        public Workday Get(DateOnly date)
        {
            return this.workdays.Find(wd => wd.Date.Equals(date));
        }

        public void AddOrUpdate(Workday workday) 
        {
            var existingWorkday = this.workdays.Find(wd => workday.Date.Equals(workday.Date));
            if (existingWorkday != null)
            {
                this.workdays.Remove(existingWorkday);
                this.workdays.Add(workday);
            } else
            {
                this.workdays.Add(workday);
            }
        }
    }
}
    