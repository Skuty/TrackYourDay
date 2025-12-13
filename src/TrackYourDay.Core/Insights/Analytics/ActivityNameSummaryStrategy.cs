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

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<EndedActivity> activities)
        {
            if (activities == null) throw new ArgumentNullException(nameof(activities));
            var activitiesList = activities.ToList();
            if (!activitiesList.Any())
            {
                _logger.LogInformation("No activities to generate summary for.");
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

                    if (!groups.TryGetValue(description, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, description);
                        groups[description] = group;
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
