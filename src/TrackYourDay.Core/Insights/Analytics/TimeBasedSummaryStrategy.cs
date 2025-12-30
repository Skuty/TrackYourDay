using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class TimeBasedSummaryStrategy : ISummaryStrategy
    {
        private readonly ILogger<TimeBasedSummaryStrategy> _logger;

        public TimeBasedSummaryStrategy(ILogger<TimeBasedSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public string StrategyName => "Time of Day Groups";

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<TrackableItem> items)
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
                    var timeOfDay = GetTimeOfDay(item.StartDate.TimeOfDay);
                    if (!groups.TryGetValue(timeOfDay, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, timeOfDay);
                        groups[timeOfDay] = group;
                    }
                    group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
                }

                result.AddRange(groups.Values);
            }

            return result.AsReadOnly();
        }

        private string GetTimeOfDay(TimeSpan time)
        {
            if (time.Hours >= 5 && time.Hours < 12)
                return "Morning (5:00-11:59)";
            if (time.Hours >= 12 && time.Hours < 17)
                return "Afternoon (12:00-16:59)";
            if (time.Hours >= 17 && time.Hours < 22)
                return "Evening (17:00-21:59)";
            return "Night (22:00-4:59)";
        }

        public void Dispose() { }
    }
}