using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Analytics
{
    public class GropuedActivity
    {
        private List<Guid> processedEvents;
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
            this.processedEvents = new List<Guid>();
            this.includedPeriods = new List<TimePeriod>();
            this.excludedPeriods = new List<TimePeriod>();
            this.Date = date;
            this.Duration = TimeSpan.Zero;
        }

        internal void Include(Guid eventGuid, TimePeriod periodToInclude) 
        {
            if (!this.processedEvents.Contains(eventGuid))
            {
                this.includedPeriods.Add(periodToInclude);
                this.processedEvents.Add(eventGuid);

                if (this.excludedPeriods.Any())
                {
                    foreach (var excludedPeriod in this.excludedPeriods)
                    {
                        if (periodToInclude.IsOverlappingWith(excludedPeriod))
                        {
                            TimeSpan durationToAdd = periodToInclude.Duration - periodToInclude.GetOverlappingDuration(excludedPeriod);
                            this.Duration += durationToAdd > TimeSpan.Zero ? durationToAdd : TimeSpan.Zero;
                        }
                        else
                        {
                            this.Duration += periodToInclude.Duration;

                        }
                    }
                } 
                else
                {
                    this.Duration += periodToInclude.Duration;

                }
            }
        }

        internal void ReduceBy(Guid eventGuid, TimePeriod periodToExclude)
        {
            if (!this.processedEvents.Contains(eventGuid))
            {
                this.excludedPeriods.Add(periodToExclude);
                this.processedEvents.Add(eventGuid);

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
