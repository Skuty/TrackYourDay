using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Insights.Workdays;

namespace TrackYourDay.Tests
{
    internal static class TestSettingsSet
    {
        public static BreaksSettings BreakSettings => BreaksSettings.CreateDefaultSettings();

        public static WorkdayDefinition WorkdayDefinition => WorkdayDefinition.CreateDefaultDefinition();
    }
}
