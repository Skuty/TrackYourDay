using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class ActivityNameSummaryStrategy : ISummaryStrategy
    {
        private readonly ILogger<ActivityNameSummaryStrategy> _logger;

        public ActivityNameSummaryStrategy(ILogger<ActivityNameSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public string StrategyName => "Activity Name Groups";

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<TrackableItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var itemsList = items.ToList();
            if (!itemsList.Any())
            {
                _logger.LogInformation("No items to generate summary for.");
                return Array.Empty<GroupedActivity>();
            }

            var groups = new Dictionary<string, GroupedActivity>();
            var firstItemDate = DateOnly.FromDateTime(itemsList.First().StartDate.Date);

            foreach (var item in itemsList)
            {
                var description = item.GetDescription();

                if (!groups.TryGetValue(description, out var group))
                {
                    group = GroupedActivity.CreateEmptyWithDescriptionForDate(firstItemDate, description);
                    groups[description] = group;
                }
                group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
            }

            return groups.Values.ToList().AsReadOnly();
        }

        public void Dispose() { }
    }
}
