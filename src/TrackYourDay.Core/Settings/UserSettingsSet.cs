
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    public class UserSettingsSet : ISettingsSet
    {
        public UserSettingsSet()
        {
            
        }

        public ActivitiesSettings ActivitiesSettings { get; set; }

        public BreaksSettings BreaksSettings { get; set; }

        public WorkdayDefinition WorkdayDefinition { get; set; }

    }
}
