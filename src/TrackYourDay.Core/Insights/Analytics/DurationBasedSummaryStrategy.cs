using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class DurationBasedSummaryStrategy : ISummaryStrategy
    {
        private readonly ILogger<DurationBasedSummaryStrategy> _logger;

        public DurationBasedSummaryStrategy(ILogger<DurationBasedSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public string StrategyName => "Duration Based Groups";

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<TrackedActivity> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var itemsList = items.ToList();
            if (!itemsList.Any())
            {
                _logger.LogInformation("No items to generate summary for.");
                return Array.Empty<GroupedActivity>();
            }

            var itemsByDate = itemsList
                .GroupBy(a => DateOnly.FromDateTime(a.StartDate.Date))
                .OrderBy(g => g.Key);

            var result = new List<GroupedActivity>();

            foreach (var dailyItems in itemsByDate)
            {
                var date = dailyItems.Key;
                var groups = new Dictionary<string, GroupedActivity>();

                foreach (var item in dailyItems)
                {
                    var duration = item.EndDate - item.StartDate;
                    var durationCategory = GetDurationCategory(duration);

                    if (!groups.TryGetValue(durationCategory, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, durationCategory);
                        groups[durationCategory] = group;
                    }
                    group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
                }

                result.AddRange(groups.Values);
            }

            return result.AsReadOnly();
        }

        private string GetDurationCategory(TimeSpan duration)
        {
            if (duration.TotalMinutes <= 15)
                return "Quick Tasks (0-15min)";
            if (duration.TotalMinutes <= 45)
                return "Short Tasks (16-45min)";
            if (duration.TotalMinutes <= 120)
                return "Medium Tasks (46-120min)";
            if (duration.TotalHours <= 4)
                return "Long Tasks (2-4h)";
            return "Extended Tasks (>4h)";
        }

        public void Dispose() { }
    }
}