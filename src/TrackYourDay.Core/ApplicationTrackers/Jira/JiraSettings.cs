
namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public record class JiraSettings(string ApiUrl, string ApiKey)
    {
        internal static JiraSettings CreateDefaultSettings()
        {
            return new JiraSettings(string.Empty, string.Empty);
        }
    }
}