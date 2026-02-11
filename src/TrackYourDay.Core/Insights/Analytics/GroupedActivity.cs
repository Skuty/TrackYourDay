using System.Net.Http.Headers;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class GroupedActivity
    {
        private readonly List<TrackedOccurrence> includedOccurrences;
        private readonly List<TrackedOccurrence> excludedOccurrences;
        private readonly HashSet<Guid> processedEventIds;

        public DateOnly Date { get; }

        public string? Description { get; }

        /// <summary>
        /// Calculates wall-clock time duration by merging overlapping periods and subtracting breaks.
        /// </summary>
        public TimeSpan Duration 
        { 
            get 
            {
                var mergedIncluded = MergeOverlappingPeriods(
                    includedOccurrences.Select(o => o.Period).ToList()
                );
                
                var totalIncluded = TimeSpan.FromTicks(mergedIncluded.Sum(p => p.Duration.Ticks));
                
                var totalExcluded = TimeSpan.Zero;
                foreach (var excluded in excludedOccurrences)
                {
                    foreach (var included in mergedIncluded)
                    {
                        if (excluded.Period.IsOverlappingWith(included))
                        {
                            totalExcluded += excluded.Period.GetOverlappingDuration(included);
                        }
                    }
                }
                
                var result = totalIncluded - totalExcluded;
                return result > TimeSpan.Zero ? result : TimeSpan.Zero;
            }
        }

        public static GroupedActivity CreateEmptyForDate(DateOnly date)
        {
            return new GroupedActivity(date);
        }

        public static GroupedActivity CreateEmptyWithDescriptionForDate(DateOnly date, string description)
        {
            return new GroupedActivity(date, description);
        }

        public GroupedActivity(DateOnly date)
        {
            includedOccurrences = new List<TrackedOccurrence>();
            excludedOccurrences = new List<TrackedOccurrence>();
            processedEventIds = new HashSet<Guid>();
            Date = date;
            Description = null;
        }

        public GroupedActivity(DateOnly date, string? description) : this(date)
        {
            Description = description;
        }

        internal void Include(Guid eventId, TimePeriod period) 
        {
            if (!processedEventIds.Contains(eventId))
            {
                includedOccurrences.Add(new TrackedOccurrence(eventId, period));
                processedEventIds.Add(eventId);
            }
        }

        internal void ReduceBy(Guid eventId, TimePeriod period)
        {
            if (!processedEventIds.Contains(eventId))
            {
                excludedOccurrences.Add(new TrackedOccurrence(eventId, period));
                processedEventIds.Add(eventId);
            }
        }

        /// <summary>
        /// Gets all included activity occurrences for timeline rendering.
        /// </summary>
        public IReadOnlyList<TrackedOccurrence> GetIncludedOccurrences()
        {
            return includedOccurrences.AsReadOnly();
        }

        /// <summary>
        /// Gets all excluded periods (breaks) for timeline rendering.
        /// </summary>
        public IReadOnlyList<TrackedOccurrence> GetExcludedOccurrences()
        {
            return excludedOccurrences.AsReadOnly();
        }

        private List<TimePeriod> MergeOverlappingPeriods(List<TimePeriod> periods)
        {
            if (periods.Count == 0) 
                return new List<TimePeriod>();
            
            var sorted = periods.OrderBy(p => p.StartDate).ToList();
            var merged = new List<TimePeriod> { sorted[0] };
            
            for (int i = 1; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var last = merged[^1];
                
                if (current.StartDate <= last.EndDate)
                {
                    // Overlapping or adjacent → merge
                    var newEndDate = current.EndDate > last.EndDate ? current.EndDate : last.EndDate;
                    merged[^1] = new TimePeriod(last.StartDate, newEndDate);
                }
                else
                {
                    // Non-overlapping → add new period
                    merged.Add(current);
                }
            }
            
            return merged;
        }
    }
}
