using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public class GitLabSettingsService : IGitLabSettingsService
    {
        private const string API_URL_KEY = "GitLab.ApiUrl";
        private const string API_KEY_KEY = "GitLab.ApiKey";
        private const string LAST_SYNC_KEY = "GitLab.LastSyncTimestamp";

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
            var lastSyncStr = settingsService.GetSetting(LAST_SYNC_KEY, string.Empty);
            
            DateTime? lastSync = null;
            if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                lastSync = parsed;
            }

            return new GitLabSettings
            {
                ApiUrl = apiUrl,
                ApiKey = apiKey,
                Enabled = enabled,
                FetchIntervalMinutes = fetchInterval,
                CircuitBreakerThreshold = cbThreshold,
                CircuitBreakerDurationMinutes = cbDuration,
                LastSyncTimestamp = lastSync
            };
        }

        public void UpdateSettings(string apiUrl, string apiKey)
        {
            settingsService.SetEncryptedSetting(API_URL_KEY, apiUrl ?? string.Empty);
            settingsService.SetEncryptedSetting(API_KEY_KEY, apiKey ?? string.Empty);
        }

        public void UpdateSettings(string apiUrl, string apiKey, bool enabled, params object[] otherParams)
        {
            // Maintain backward-compatible behavior: if otherParams provided include expected values, map them accordingly.
            int fetchIntervalMinutes = 15;
            int circuitBreakerThreshold = 5;
            int circuitBreakerDurationMinutes = 5;

            if (otherParams != null && otherParams.Length >= 3)
            {
                if (otherParams[0] is int fi) fetchIntervalMinutes = fi;
                if (otherParams[1] is int cbt) circuitBreakerThreshold = cbt;
                if (otherParams[2] is int cbd) circuitBreakerDurationMinutes = cbd;
            }

            settingsService.SetEncryptedSetting(API_URL_KEY, apiUrl ?? string.Empty);
            settingsService.SetEncryptedSetting(API_KEY_KEY, apiKey ?? string.Empty);
            settingsService.SetSetting("GitLab.Enabled", enabled);
            settingsService.SetSetting("GitLab.FetchIntervalMinutes", fetchIntervalMinutes);
            settingsService.SetSetting("GitLab.CircuitBreakerThreshold", circuitBreakerThreshold);
            settingsService.SetSetting("GitLab.CircuitBreakerDurationMinutes", circuitBreakerDurationMinutes);
        }

        public void UpdateLastSyncTimestamp(DateTime timestamp)
        {
            settingsService.SetSetting(LAST_SYNC_KEY, timestamp.ToString("O"));
        }

        public void PersistSettings()
        {
            settingsService.PersistSettings();
        }
    }
}
