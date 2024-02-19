
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    public class UserSettingsSet : ISettingsSet
    {
        private readonly ActivitiesSettings activitiesSettings;
        private readonly BreaksSettings breaksSettings;
        private readonly WorkdayDefinition workdayDefinition;

        public UserSettingsSet(ActivitiesSettings activitiesSettings, BreaksSettings breaksSettings, WorkdayDefinition workdayDefinition)
        {
            this.activitiesSettings = activitiesSettings;
            this.breaksSettings = breaksSettings;
            this.workdayDefinition = workdayDefinition;
        }

        public ActivitiesSettings ActivitiesSettings => this.activitiesSettings;

        public BreaksSettings BreaksSettings => this.breaksSettings;

        public WorkdayDefinition WorkdayDefinition => this.workdayDefinition;
    }
}
