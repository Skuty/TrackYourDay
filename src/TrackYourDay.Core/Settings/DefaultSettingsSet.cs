using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    public record class DefaultSettingsSet : ISettingsSet
    {
        public ActivitiesSettings ActivitiesSettings => ActivitiesSettings.CreateDefaultSettings();

        public BreaksSettings BreaksSettings => new BreaksSettings();

        public WorkdayDefinition WorkdayDefinition => new WorkdayDefinition();
    }
}
