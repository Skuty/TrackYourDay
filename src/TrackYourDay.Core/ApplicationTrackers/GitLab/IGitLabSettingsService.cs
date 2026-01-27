namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public interface IGitLabSettingsService
    {
        /// <summary>
        /// Gets the current GitLab settings.
        /// </summary>
        /// <returns>The GitLab settings</returns>
        GitLabSettings GetSettings();

        /// <summary>
        /// Updates the GitLab API URL and key.
        /// </summary>
        /// <param name="apiUrl">The GitLab API URL</param>
        /// <param name="apiKey">The GitLab API key</param>
        void UpdateSettings(string apiUrl, string apiKey);

        /// <summary>
        /// Updates the GitLab settings including enabled flag and fetch interval and circuit breaker settings.
        /// </summary>
        /// <param name="apiUrl">The GitLab API URL</param>
        /// <param name="apiKey">The GitLab API key</param>
        /// <param name="enabled">Whether GitLab integration is enabled</param>
        /// <param name="fetchIntervalMinutes">Fetch interval in minutes</param>
        /// <param name="circuitBreakerThreshold">Circuit breaker threshold</param>
        /// <param name="circuitBreakerDurationMinutes">Circuit breaker duration in minutes</param>
        void UpdateSettings(string apiUrl, string apiKey, bool enabled, int fetchIntervalMinutes, int circuitBreakerThreshold, int circuitBreakerDurationMinutes);

        /// <summary>
        /// Updates the last successful sync timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp of the last successful sync</param>
        void UpdateLastSyncTimestamp(DateTime timestamp);

        /// <summary>
        /// Persists the GitLab settings.
        /// </summary>
        void PersistSettings();
    }
}
