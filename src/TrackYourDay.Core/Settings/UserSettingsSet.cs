using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Settings
{
    //TODO: avoid shared seeting set. Remove it and create single generic storage but dedicated settings per module/tracker/insight
    public record class UserSettingsSet(
        ActivitiesSettings ActivitiesSettings,
        BreaksSettings BreaksSettings,
        WorkdayDefinition WorkdayDefinition,
        GitLabSettings GitLabSettings) : ISettingsSet
    {
    }
}
