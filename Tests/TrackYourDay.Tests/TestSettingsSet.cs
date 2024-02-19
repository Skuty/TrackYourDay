using TrackYourDay.Core;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests
{
    internal static class TestSettingsSet
    {
        public static BreaksSettings BreakSettings => BreaksSettings.CreateDefaultSettings();

        public static WorkdayDefinition WorkdayDefinition => WorkdayDefinition.CreateDefaultDefinition();
    }
}
