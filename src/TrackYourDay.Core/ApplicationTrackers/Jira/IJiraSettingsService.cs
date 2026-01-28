namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public interface IJiraSettingsService
    {
        /// <summary>
        /// Gets the current Jira settings.
        /// </summary>
        /// <returns>The Jira settings</returns>
        JiraSettings GetSettings();

        /// <summary>
        /// Updates the Jira API URL and key.
        /// </summary>
        /// <param name="apiUrl">The Jira API URL</param>
        /// <param name="apiKey">The Jira API key</param>
        void UpdateSettings(string apiUrl, string apiKey);

        /// <summary>
        /// Updates the Jira settings including enabled flag and fetch interval and circuit breaker settings.
        /// </summary>
        /// <param name="apiUrl">The Jira API URL</param>
        /// <param name="apiKey">The Jira API key</param>
        /// <param name="enabled">Whether Jira integration is enabled</param>
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
        /// Persists the Jira settings.
        /// </summary>
        void PersistSettings();
    }
}
