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

            var groups = new Dictionary<string, GroupedActivity>();
            var firstActivityDate = DateOnly.FromDateTime(activitiesList.First().StartDate.Date);

            foreach (var activity in activitiesList)
            {
                var description = activity.GetDescription();

                if (!groups.TryGetValue(description, out var group))
                {
                    group = GroupedActivity.CreateEmptyWithDescriptionForDate(firstActivityDate, description);
                    groups[description] = group;
                }
                group.Include(activity.Guid, new TimePeriod(activity.StartDate, activity.EndDate));
            }

            return groups.Values.ToList().AsReadOnly();
        }

        public void Dispose() { }
    }
}
