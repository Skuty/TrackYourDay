using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Analytics
{
    public class GropuedActivity
    {
        private List<Guid> processedActivities;
        private List<Guid> processedBreaks;
        private List<TimePeriod> includedPeriods;
        private List<TimePeriod> excludedPeriods;

        public DateOnly Date { get; }

        public string ActivityDescription { get; }

        public TimeSpan Duration { get; private set; }

        public static GropuedActivity CreateForDate(DateOnly date)
        {
            return new GropuedActivity(date);
        }

        public GropuedActivity(DateOnly date)
        {
            this.processedActivities = new List<Guid>();
            this.processedBreaks = new List<Guid>();
            this.includedPeriods = new List<TimePeriod>();
            this.excludedPeriods = new List<TimePeriod>();
            this.Date = date;
            this.Duration = TimeSpan.Zero;
        }

        internal void Include(EndedActivity activityToInclude) 
        {
            if (!this.processedActivities.Contains(activityToInclude.Guid))
            {
                var periodToInclude = new TimePeriod(activityToInclude.StartDate, activityToInclude.EndDate);
                this.includedPeriods.Add(periodToInclude);
                this.processedActivities.Add(activityToInclude.Guid);

                foreach (var excludedPeriod in this.excludedPeriods)
                {
                    if (periodToInclude.IsOverlappingWith(excludedPeriod))
                    {
                        this.Duration += periodToInclude.GetOverlappingDuration(excludedPeriod);
                    }
                    else
                    {
                        this.Duration += periodToInclude.Duration;

                    }
                }
            }
        }

        internal void ReduceBy(EndedBreak breakToReduce)
        {
            if (!this.processedBreaks.Contains(breakToReduce.Guid))
            {
                var periodToExclude = new TimePeriod(breakToReduce.BreakStartedAt, breakToReduce.BreakEndedAt);
                this.excludedPeriods.Add(periodToExclude);
                this.processedBreaks.Add(breakToReduce.Guid);

                foreach (var includedPeriod in this.includedPeriods)
                {
                    if (periodToExclude.IsOverlappingWith(includedPeriod))
                    {
                        this.Duration -= periodToExclude.GetOverlappingDuration(includedPeriod);
                    }
                }
            }
        }
    }
}
