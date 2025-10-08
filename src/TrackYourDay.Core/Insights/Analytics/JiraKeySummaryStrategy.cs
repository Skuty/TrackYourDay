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

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<EndedActivity> activities)
        {
            if (activities == null) throw new ArgumentNullException(nameof(activities));
            var activitiesList = activities.ToList();
            if (!activitiesList.Any())
            {
                _logger?.LogInformation("No activities to generate summary for.");
                return Array.Empty<GroupedActivity>();
            }

            var activitiesByDate = activitiesList
                .GroupBy(a => DateOnly.FromDateTime(a.StartDate.Date))
                .OrderBy(g => g.Key);

            var result = new List<GroupedActivity>();

            foreach (var dailyActivities in activitiesByDate)
            {
                var date = dailyActivities.Key;
                var groups = new Dictionary<string, GroupedActivity>();
                foreach (var activity in dailyActivities)
                {
                    var description = activity.GetDescription();
                    var match = JiraKeyRegex.Match(description);
                    var key = match.Success ? match.Value : "No Jira Key";
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, key);
                        groups[key] = group;
                    }
                    group.Include(activity.Guid, new TimePeriod(activity.StartDate, activity.EndDate));
                }
                result.AddRange(groups.Values);
            }
            return result.AsReadOnly();
        }

        public void Dispose() { }
    }
}
