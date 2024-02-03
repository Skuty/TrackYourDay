
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    internal class UserSettingsSet : ISettingsSet
    {
        public ActivitiesSettings ActivitiesSettings { get; init; }

        public BreaksSettings BreaksSettings { get; init; }

        public WorkdayDefinition WorkdayDefinition { get; init; }

    }
}
