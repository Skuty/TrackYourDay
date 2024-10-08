﻿using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TrackYourDay.Core.Workdays
{
    public class WorkdayReadModelRepository
    {
        private readonly ConcurrentDictionary<DateOnly, Workday> workdays;
        private readonly DateTime instanceCreateDate;
        public WorkdayReadModelRepository() 
        {
            this.instanceCreateDate = DateTime.Now;
            this.workdays = new ConcurrentDictionary<DateOnly, Workday>();
        }

        public Workday Get(DateOnly date)
        {
            this.workdays.GetOrAdd(date, Workday.CreateEmpty(date));

            return this.workdays[date];

            // TODO: Here should be workday with definition based on default settings, not raw like this
            //TODO: This was hotfixefd but it shouldnt be like it throw new Exception("Result is null here on raw launch. This should be fixed, probably by reyturning here empty object.");
        }

        public void AddOrUpdate(Workday workday) 
        {
            if (this.workdays.TryGetValue(workday.Date, out Workday existingWorkday))
            {
                this.workdays.TryUpdate(workday.Date, workday, existingWorkday);
            }
        }
    }
}
    