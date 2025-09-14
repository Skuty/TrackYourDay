namespace TrackYourDay.Core.Settings
{
    public interface IGenericSettingsService
    {
        /// <summary>
        /// Gets a setting value by key. Returns default value if key doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of the setting value</typeparam>
        /// <param name="key">The setting key</param>
        /// <param name="defaultValue">Default value to return if key doesn't exist</param>
        /// <returns>The setting value or default value</returns>
        T GetSetting<T>(string key, T defaultValue = default);

        /// <summary>
        /// Sets a setting value by key.
        /// </summary>
        /// <typeparam name="T">The type of the setting value</typeparam>
        /// <param name="key">The setting key</param>
        /// <param name="value">The setting value</param>
        void SetSetting<T>(string key, T value);

        /// <summary>
        /// Gets an encrypted setting value by key. Returns default value if key doesn't exist.
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <param name="defaultValue">Default value to return if key doesn't exist</param>
        /// <returns>The decrypted setting value or default value</returns>
        string GetEncryptedSetting(string key, string defaultValue = "");

        /// <summary>
        /// Sets an encrypted setting value by key.
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <param name="value">The setting value to encrypt and store</param>
        void SetEncryptedSetting(string key, string value);

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
        /// Persists all settings to storage.
        /// </summary>
        void PersistSettings();

        /// <summary>
        /// Loads settings from storage.
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// Clears all settings from memory and storage.
        /// </summary>
        void ClearAllSettings();
    }
}
