using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Insights.Workdays;

namespace TrackYourDay.Tests.TestHelpers
{
    public static class TestSettingsSet
    {
        public static WorkdayDefinition WorkdayDefinition => new WorkdayDefinition(
            WorkdayDuration: TimeSpan.FromHours(8),
            AllowedBreakDuration: TimeSpan.FromMinutes(50))
        {
            BreakDefinitions = new List<BreakDefinition>
            {
                new BreakDefinition(TimeSpan.FromMinutes(15), "Short break"),
                new BreakDefinition(TimeSpan.FromMinutes(30), "Lunch break"),
                new BreakDefinition(TimeSpan.FromMinutes(5), "Micro break")
            }
        };
    }
}
