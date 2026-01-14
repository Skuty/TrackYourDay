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
            var enabled = settingsService.GetSetting("GitLab.Enabled", false);
            var fetchInterval = settingsService.GetSetting("GitLab.FetchIntervalMinutes", 15);
            var cbThreshold = settingsService.GetSetting("GitLab.CircuitBreakerThreshold", 5);
            var cbDuration = settingsService.GetSetting("GitLab.CircuitBreakerDurationMinutes", 5);

            return new GitLabSettings
            {
                ApiUrl = apiUrl,
                ApiKey = apiKey,
                Enabled = enabled,
                FetchIntervalMinutes = fetchInterval,
                CircuitBreakerThreshold = cbThreshold,
                CircuitBreakerDurationMinutes = cbDuration
            };
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
