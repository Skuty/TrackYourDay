
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Settings
{
    public interface ISettingsSet
    {
        ActivitiesSettings ActivitiesSettings { get; }

        BreaksSettings BreaksSettings { get; }

        WorkdayDefinition WorkdayDefinition { get; }
    }
}