
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    public class UserSettingsSet : ISettingsSet
    {
        public UserSettingsSet(ActivitiesSettings activitiesSettings, BreaksSettings breaksSettings, WorkdayDefinition workdayDefinition)
        {
            ActivitiesSettings = activitiesSettings;
            BreaksSettings = breaksSettings;
            WorkdayDefinition = workdayDefinition;
        }

        public ActivitiesSettings ActivitiesSettings { get; set; }

        public BreaksSettings BreaksSettings { get; set; }

        public WorkdayDefinition WorkdayDefinition { get; set; }
    }
}
