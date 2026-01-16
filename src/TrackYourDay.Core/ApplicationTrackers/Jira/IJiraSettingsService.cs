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
        /// Updates the last successful sync timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp of the last successful sync</param>
        void UpdateLastSyncTimestamp(DateTime timestamp);

        /// <summary>
        /// Gets the sync start date. Returns LastSyncTimestamp if available, otherwise returns 2 days ago.
        /// </summary>
        /// <returns>The date to start syncing from</returns>
        DateTime GetSyncStartDate();

        /// <summary>
        /// Persists the Jira settings.
        /// </summary>
        void PersistSettings();
    }
}
