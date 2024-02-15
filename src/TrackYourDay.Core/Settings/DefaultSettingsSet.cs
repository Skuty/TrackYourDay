using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    public record class DefaultSettingsSet : ISettingsSet
    {
        public DefaultSettingsSet()
        {
            
        }

        public ActivitiesSettings ActivitiesSettings => ActivitiesSettings.CreateDefaultSettings();

        public BreaksSettings BreaksSettings => BreaksSettings.CreateDefaultSettings();

        Exception("StackOverFlowHere > To much calls,rest of app persisting should works")
        public WorkdayDefinition WorkdayDefinition => WorkdayDefinition.CreateDefaultDefinition();
    }
}
