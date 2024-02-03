using TrackYourDay.Core;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests
{
    internal static class TestSettingsSet
    {
        public static BreaksSettings BreakSettings => new BreaksSettings();

        public static WorkdayDefinition WorkdayDefinition => new WorkdayDefinition();
    }
}
