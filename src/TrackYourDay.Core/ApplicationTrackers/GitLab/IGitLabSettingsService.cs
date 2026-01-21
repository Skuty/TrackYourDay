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
