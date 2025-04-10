using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Settings
{
    public interface ISettingsSet
    {
        ActivitiesSettings ActivitiesSettings { get; }

        BreaksSettings BreaksSettings { get; }

        WorkdayDefinition WorkdayDefinition { get; }

        GitLabSettings GitLabSettings { get; }

        JiraSettings JiraSettings { get; }
    }

    public class JiraSettings
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
    }
}