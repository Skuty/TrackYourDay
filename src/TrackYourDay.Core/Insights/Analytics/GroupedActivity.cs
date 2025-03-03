using System.Net.Http.Headers;

namespace TrackYourDay.Core.Insights.Analytics
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
            processedEvents = new List<Guid>();
            includedPeriods = new List<TimePeriod>();
            excludedPeriods = new List<TimePeriod>();
            Date = date;
            Duration = TimeSpan.Zero;
        }

        public GroupedActivity(DateOnly date, string description) : this(date)
        {
            Description = description;
        }

        // TODO: We are using guid to identify was TimePeriod already exlucded,
        // but we should compare to other excluded time period because guid can differ and we will substitute twice the same time
        // Guid should not be interesting for us at all, or at least we shouldnt be considering it as condition
        internal void Include(Guid eventGuid, TimePeriod periodToInclude) 
        {
            if (!processedEvents.Contains(eventGuid))
            {
                includedPeriods.Add(periodToInclude);
                processedEvents.Add(eventGuid);

                if (excludedPeriods.Any())
                {
                    foreach (var excludedPeriod in excludedPeriods)
                    {
                        if (periodToInclude.IsOverlappingWith(excludedPeriod))
                        {
                            TimeSpan durationToAdd = periodToInclude.Duration - periodToInclude.GetOverlappingDuration(excludedPeriod);
                            Duration += durationToAdd > TimeSpan.Zero ? durationToAdd : TimeSpan.Zero;
                        }
                        else
                        {
                            Duration += periodToInclude.Duration;

                        }
                    }
                } 
                else
                {
                    Duration += periodToInclude.Duration;

                }
            }
        }

        internal void ReduceBy(Guid eventGuid, TimePeriod periodToExclude)
        {
            if (!processedEvents.Contains(eventGuid))
            {
                excludedPeriods.Add(periodToExclude);
                processedEvents.Add(eventGuid);

                foreach (var includedPeriod in includedPeriods)
                {
                    if (periodToExclude.IsOverlappingWith(includedPeriod))
                    {
                        Duration -= periodToExclude.GetOverlappingDuration(includedPeriod);
                    }
                }
            }
        }
    }
}
