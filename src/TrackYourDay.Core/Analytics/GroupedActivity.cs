using System.Net.Http.Headers;

namespace TrackYourDay.Core.Analytics
{
    public class GroupedActivity
    {
        private List<Guid> processedEvents;
        private List<TimePeriod> includedPeriods;
        private List<TimePeriod> excludedPeriods;

        public DateOnly Date { get; }

        public string Description { get; }

        public TimeSpan Duration { get; private set; }

        public static GroupedActivity CreateEmptyForDate(DateOnly date)
        {
            return new GroupedActivity(date);
        }

        public static GroupedActivity CreateEmptyWithDescriptionForDate(DateOnly date, string description)
        {
            return new GroupedActivity(date, description);
        }


        private static GroupedActivity CombineWith(GroupedActivity activityToCombine)
        {
            throw new NotImplementedException("Just an idea how we can do cascaded grouping of different grouped activities to other grouped groups without implementing new type");
        }

        public GroupedActivity(DateOnly date)
        {
            this.processedEvents = new List<Guid>();
            this.includedPeriods = new List<TimePeriod>();
            this.excludedPeriods = new List<TimePeriod>();
            this.Date = date;
            this.Duration = TimeSpan.Zero;
        }

        public GroupedActivity(DateOnly date, string description) : this(date)
        {
            this.Description = description;
        }

        // TODO: We are using guid to identify was TimePeriod already exlucded,
        // but we should compare to other excluded time period because guid can differ and we will substitute twice the same time
        // Guid should not be interesting for us at all, or at least we shouldnt be considering it as condition
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
