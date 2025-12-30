using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class JiraKeySummaryStrategy : ISummaryStrategy
    {
        private readonly ILogger<JiraKeySummaryStrategy> _logger;
        private static readonly Regex JiraKeyRegex = new Regex(@"[A-Z][A-Z0-9]+-\d+", RegexOptions.Compiled);
        public JiraKeySummaryStrategy(ILogger<JiraKeySummaryStrategy> logger)
        {
            _logger = logger;
        }

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<TrackedActivity> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var itemsList = items.ToList();
            if (!itemsList.Any())
            {
                _logger?.LogInformation("No items to generate summary for.");
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
                    var description = item.GetDescription();
                    var match = JiraKeyRegex.Match(description);
                    var key = match.Success ? match.Value : "No Jira Key";
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, key);
                        groups[key] = group;
                    }
                    group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
                }
                result.AddRange(groups.Values);
            }
            return result.AsReadOnly();
        }

        public string StrategyName => "Jira Key Grouping";

        public void Dispose() { }
    }
}
