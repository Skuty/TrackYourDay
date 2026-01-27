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
        /// Updates the Jira settings including enabled flag and other parameters.
        /// </summary>
        /// <param name="apiUrl">The Jira API URL</param>
        /// <param name="apiKey">The Jira API key</param>
        /// <param name="enabled">Whether Jira integration is enabled</param>
        /// <param name="otherParams">Other optional parameters</param>
        void UpdateSettings(string apiUrl, string apiKey, bool enabled, params object[] otherParams);

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
