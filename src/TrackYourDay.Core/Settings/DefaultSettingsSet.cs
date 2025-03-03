using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Settings
{
    public sealed record class DefaultSettingsSet : ISettingsSet
    {
        public DefaultSettingsSet()
        {
            
        }

        public ActivitiesSettings ActivitiesSettings => ActivitiesSettings.CreateDefaultSettings();

        public BreaksSettings BreaksSettings => BreaksSettings.CreateDefaultSettings();

        public WorkdayDefinition WorkdayDefinition => WorkdayDefinition.CreateSampleCompanyDefinition();
    }
}
