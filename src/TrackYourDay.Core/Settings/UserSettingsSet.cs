using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Workdays;

namespace TrackYourDay.Core.Settings
{
    public record class UserSettingsSet(
        ActivitiesSettings ActivitiesSettings,
        BreaksSettings BreaksSettings,
        WorkdayDefinition WorkdayDefinition) : ISettingsSet
    {
    }
}
