using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Core.Settings
{
    //TODO: Try to replace with settings per m  odule and see what changes it implicates now and in future
    public class SqlLiteSettingsRepository : ISettingsRepository
    {
        private readonly string databaseFileName;

        public SqlLiteSettingsRepository()
        {
            var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory($"{appDataPath}");
            }

            this.databaseFileName = $"{appDataPath}\\TrackYourDay.db";
        }

        public void Reset()
        {
            if (File.Exists(this.databaseFileName))
            {
                File.Delete(this.databaseFileName);
            }

            this.InitializeStructure();
        }

        private void InitializeStructure()
        {
            using (var connection = new SqliteConnection($"Data Source={this.databaseFileName}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS settings (
                       SettingsSetName NVARCHAR NOT NULL,
                       SettingsSetContent NVARCHAR NOT NULL);
                ";
                command.ExecuteNonQuery();
            }
        }

        public ISettingsSet Get()
        {
            this.InitializeStructure();
            //throw new Exception("Firstrun after serializing settings is ok, in second defualt values ar eback");
            string settingsSetContent = string.Empty;
            using (var connection = new SqliteConnection($"Data Source={this.databaseFileName}"))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =
                @"
                    SELECT SettingsSetContent FROM settings ORDER BY rowid DESC LIMIT 1;
                ";

                settingsSetContent = (string)command.ExecuteScalar();
            }
            
            if (string.IsNullOrEmpty(settingsSetContent))
            {
                return new DefaultSettingsSet();
            }

            try
            {
                var deserialedSettings = JsonSerializer.Deserialize<UserSettingsSet>(settingsSetContent);
                if (deserialedSettings is not null)
                {
                    //Here are deserialized seetings that probably are overwritting new settings for today
                    if (deserialedSettings.JiraSettings is null)
                    {
                        // While adding new settings, then in db those settings are null, so above check passes but we have empty values later
                        deserialedSettings = deserialedSettings with { JiraSettings = JiraSettings.CreateDefaultSettings() };
                        return deserialedSettings;

                    }
                    return deserialedSettings;
                } 
                else
                {
                    return new DefaultSettingsSet();
                }
            } catch (Exception ex)
            {
                return new DefaultSettingsSet();
            }
        }

        public void Save(ISettingsSet settings)
        {
            this.InitializeStructure();
            var serializedSettings = JsonSerializer.Serialize(settings);
            using (var connection = new SqliteConnection($"Data Source={this.databaseFileName}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @$"
                    INSERT INTO settings (SettingsSetName, SettingsSetContent)
                    VALUES ('UserSettingsSet', '{serializedSettings}');
                ";
                command.ExecuteNonQuery();
            }
        }

        public class InMemorySettingsRepository : ISettingsRepository
        {
            private ISettingsSet settings;

            public InMemorySettingsRepository()
            {
                this.settings = new DefaultSettingsSet();
            }

            public ISettingsSet Get()
            {
                return this.settings;
            }

            public void Reset()
            {
                this.settings = new DefaultSettingsSet();
            }

            public void Save(ISettingsSet settings)
            {
                this.settings = settings;
            }
        }
    }
}
