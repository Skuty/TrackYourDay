using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public class GitLabSettingsService : IGitLabSettingsService
    {
        private const string API_URL_KEY = "GitLab.ApiUrl";
        private const string API_KEY_KEY = "GitLab.ApiKey";

        private readonly IGenericSettingsService settingsService;

        public GitLabSettingsService(IGenericSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public GitLabSettings GetSettings()
        {
            var apiUrl = settingsService.GetEncryptedSetting(API_URL_KEY, string.Empty);
            var apiKey = settingsService.GetEncryptedSetting(API_KEY_KEY, string.Empty);

            return new GitLabSettings(apiUrl, apiKey);
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
