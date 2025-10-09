using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.SystemTrackers;
using System.Text.RegularExpressions;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class ContextBasedSummaryStrategy : ISummaryStrategy
    {
        private readonly ILogger<ContextBasedSummaryStrategy> _logger;
        private static readonly Dictionary<string, string[]> ContextKeywords = new()
        {
            { "Meeting", new[] { "meeting", "sync", "standup", "call", "review", "retro", "planning" } },
            { "Coding", new[] { "code", "coding", "develop", "fix", "bug", "feature", "refactor", "implement" } },
            { "Email", new[] { "email", "mail", "inbox", "send", "reply" } },
            { "Documentation", new[] { "doc", "documentation", "spec", "writeup", "readme" } },
            { "Testing", new[] { "test", "testing", "qa", "verify", "assert" } },
            { "Design", new[] { "design", "mockup", "ui", "ux", "prototype" } },
            { "Other", new string[0] }
        };

        public ContextBasedSummaryStrategy(ILogger<ContextBasedSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public string StrategyName => "Context Based Groups";

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
                    var description = activity.GetDescription().ToLowerInvariant();
                    var context = GetContext(description);
                    if (!groups.TryGetValue(context, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, context);
                        groups[context] = group;
                    }
                    group.Include(activity.Guid, new TimePeriod(activity.StartDate, activity.EndDate));
                }
                result.AddRange(groups.Values);
            }
            return result.AsReadOnly();
        }

        private string GetContext(string description)
        {
            foreach (var kvp in ContextKeywords)
            {
                if (kvp.Key == "Other") continue;
                foreach (var keyword in kvp.Value)
                {
                    if (Regex.IsMatch(description, $"\\b{Regex.Escape(keyword)}\\b"))
                        return kvp.Key;
                }
            }
            return "Other";
        }

        public void Dispose() { }
    }
}
