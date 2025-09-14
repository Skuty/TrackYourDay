using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

namespace TrackYourDay.Core.Settings
{
    public class SqliteGenericSettingsRepository : IGenericSettingsRepository
    {
        private readonly string databaseFileName;
        private readonly ConcurrentDictionary<string, string> cache = new();
        private bool isLoaded = false;

        public SqliteGenericSettingsRepository()
        {
            var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory($"{appDataPath}");
            }

            this.databaseFileName = $"{appDataPath}\\TrackYourDayGeneric.db";
        }

        public string? GetSetting(string key)
        {
            EnsureLoaded();
            return cache.TryGetValue(key, out var value) ? value : null;
        }

        public void SetSetting(string key, string value)
        {
            EnsureLoaded();
            cache[key] = value;
        }

        public bool HasSetting(string key)
        {
            EnsureLoaded();
            return cache.ContainsKey(key);
        }

        public void RemoveSetting(string key)
        {
            EnsureLoaded();
            cache.TryRemove(key, out _);
        }

        public IEnumerable<string> GetAllKeys()
        {
            EnsureLoaded();
            return cache.Keys.ToList();
        }

        public void Save()
        {
            InitializeStructure();

            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            // Clear existing settings
            var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM generic_settings";
            clearCommand.ExecuteNonQuery();

            // Insert all current settings
            foreach (var kvp in cache)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO generic_settings (SettingKey, SettingValue) VALUES (@key, @value)";
                insertCommand.Parameters.AddWithValue("@key", kvp.Key);
                insertCommand.Parameters.AddWithValue("@value", kvp.Value);
                insertCommand.ExecuteNonQuery();
            }
        }

        public void Load()
        {
            InitializeStructure();
            cache.Clear();

            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT SettingKey, SettingValue FROM generic_settings";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);
                cache[key] = value;
            }

            isLoaded = true;
        }

        public void Clear()
        {
            cache.Clear();
            
            if (File.Exists(databaseFileName))
            {
                File.Delete(databaseFileName);
            }

            InitializeStructure();
            isLoaded = true;
        }

        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS generic_settings (
                    SettingKey NVARCHAR PRIMARY KEY,
                    SettingValue NVARCHAR NOT NULL
                );";
            command.ExecuteNonQuery();
        }

        private void EnsureLoaded()
        {
            if (!isLoaded)
            {
                Load();
            }
        }
    }
}
