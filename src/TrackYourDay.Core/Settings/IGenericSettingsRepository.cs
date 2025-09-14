namespace TrackYourDay.Core.Settings
{
    public interface IGenericSettingsRepository
    {
        /// <summary>
        /// Gets a setting value by key.
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <returns>The setting value or null if key doesn't exist</returns>
        string? GetSetting(string key);

        /// <summary>
        /// Sets a setting value by key.
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <param name="value">The setting value</param>
        void SetSetting(string key, string value);

        /// <summary>
        /// Checks if a setting key exists.
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <returns>True if the key exists, false otherwise</returns>
        bool HasSetting(string key);

        /// <summary>
        /// Removes a setting by key.
        /// </summary>
        /// <param name="key">The setting key</param>
        void RemoveSetting(string key);

        /// <summary>
        /// Gets all setting keys.
        /// </summary>
        /// <returns>Collection of all setting keys</returns>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        /// Saves all settings to persistent storage.
        /// </summary>
        void Save();

        /// <summary>
        /// Loads all settings from persistent storage.
        /// </summary>
        void Load();

        /// <summary>
        /// Clears all settings from memory and persistent storage.
        /// </summary>
        void Clear();
    }
}
