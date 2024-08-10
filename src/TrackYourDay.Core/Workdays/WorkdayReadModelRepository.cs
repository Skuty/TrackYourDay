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
            var result = this.workdays.Find(wd => wd.Date.Equals(date));
            if (result is not null) 
            {
                return result;
            } else
            {
                // TODO: Here should be workday with definition based on default settings, not raw like this
                return Workday.CreateEmpty();
            }
            //TODO: This was hotfixefd but it shouldnt be like it throw new Exception("Result is null here on raw launch. This should be fixed, probably by reyturning here empty object.");
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
    