using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Workdays;

namespace TrackYourDay.Tests
{
    internal static class TestSettingsSet
    {
        public static BreaksSettings BreakSettings => BreaksSettings.CreateDefaultSettings();

        public static WorkdayDefinition WorkdayDefinition => WorkdayDefinition.CreateDefaultDefinition();
    }
}
