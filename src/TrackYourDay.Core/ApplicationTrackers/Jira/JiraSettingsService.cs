using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public class JiraSettingsService : IJiraSettingsService
    {
        private const string API_URL_KEY = "Jira.ApiUrl";
        private const string API_KEY_KEY = "Jira.ApiKey";
        private const string LAST_SYNC_KEY = "Jira.LastSyncTimestamp";

        private readonly IGenericSettingsService settingsService;

        public JiraSettingsService(IGenericSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public JiraSettings GetSettings()
        {
            var apiUrl = settingsService.GetEncryptedSetting(API_URL_KEY, string.Empty);
            var apiKey = settingsService.GetEncryptedSetting(API_KEY_KEY, string.Empty);
            var enabled = settingsService.GetSetting("Jira.Enabled", false);
            var fetchInterval = settingsService.GetSetting("Jira.FetchIntervalMinutes", 15);
            var cbThreshold = settingsService.GetSetting("Jira.CircuitBreakerThreshold", 5);
            var cbDuration = settingsService.GetSetting("Jira.CircuitBreakerDurationMinutes", 5);
            var lastSyncStr = settingsService.GetSetting(LAST_SYNC_KEY, string.Empty);
            
            DateTime? lastSync = null;
            if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                lastSync = parsed;
            }

            return new JiraSettings
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

        public void UpdateSettings(string apiUrl, string apiKey, bool enabled, int fetchIntervalMinutes, int circuitBreakerThreshold, int circuitBreakerDurationMinutes)
        {
            // Validate inputs when enabling integration
            if (enabled)
            {
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    throw new System.ArgumentException("API URL is required when enabling Jira integration.", nameof(apiUrl));
                }
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new System.ArgumentException("API Key is required when enabling Jira integration.", nameof(apiKey));
                }
            }

            if (fetchIntervalMinutes < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(fetchIntervalMinutes), "Fetch interval must be at least 1 minute.");
            }
            if (circuitBreakerThreshold < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(circuitBreakerThreshold), "Circuit breaker threshold must be at least 1.");
            }
            if (circuitBreakerDurationMinutes < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(circuitBreakerDurationMinutes), "Circuit breaker duration must be at least 1 minute.");
            }

            settingsService.SetEncryptedSetting(API_URL_KEY, apiUrl ?? string.Empty);
            settingsService.SetEncryptedSetting(API_KEY_KEY, apiKey ?? string.Empty);
            settingsService.SetSetting("Jira.Enabled", enabled);
            settingsService.SetSetting("Jira.FetchIntervalMinutes", fetchIntervalMinutes);
            settingsService.SetSetting("Jira.CircuitBreakerThreshold", circuitBreakerThreshold);
            settingsService.SetSetting("Jira.CircuitBreakerDurationMinutes", circuitBreakerDurationMinutes);
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
