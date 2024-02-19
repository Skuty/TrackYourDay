using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    public record class UserSettingsSet(
        ActivitiesSettings ActivitiesSettings,
        BreaksSettings BreaksSettings,
        WorkdayDefinition WorkdayDefinition) : ISettingsSet
    {
    }
}
