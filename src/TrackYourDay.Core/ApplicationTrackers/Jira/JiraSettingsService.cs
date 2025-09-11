using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public class JiraSettingsService : IJiraSettingsService
    {
        private const string API_URL_KEY = "Jira.ApiUrl";
        private const string API_KEY_KEY = "Jira.ApiKey";

        private readonly IGenericSettingsService settingsService;

        public JiraSettingsService(IGenericSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public JiraSettings GetSettings()
        {
            var apiUrl = settingsService.GetEncryptedSetting(API_URL_KEY, string.Empty);
            var apiKey = settingsService.GetEncryptedSetting(API_KEY_KEY, string.Empty);

            return new JiraSettings(apiUrl, apiKey);
        }

        public void UpdateSettings(string apiUrl, string apiKey)
        {
            settingsService.SetEncryptedSetting(API_URL_KEY, apiUrl ?? string.Empty);
            settingsService.SetEncryptedSetting(API_KEY_KEY, apiKey ?? string.Empty);
        }

        public void PersistSettings()
        {
            settingsService.PersistSettings();
        }
    }
}
