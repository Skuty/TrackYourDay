using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Settings
{
    public record class UserSettingsSet(
        ActivitiesSettings ActivitiesSettings,
        BreaksSettings BreaksSettings,
        WorkdayDefinition WorkdayDefinition) : ISettingsSet
    {
    }
}
