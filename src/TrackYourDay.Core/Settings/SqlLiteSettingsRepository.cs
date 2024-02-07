﻿using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace TrackYourDay.Core.Settings
{
    //TODO: Try to replace with settings per module and see what changes it implicates now and in future
    public class SqlLiteSettingsRepository : ISettingsRepository
    {
        private readonly string databaseFileName;

        public SqlLiteSettingsRepository()
        {
            this.databaseFileName = "TrackYourDay.db";    
        }

        private void InitializeStructure()
        {
            using (var connection = new SqliteConnection($"Data Source={this.databaseFileName}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE [IF NOT EXISTS] [settings].SettingsSet (
   	                    SettingsSetName NVARCHAR(MAX) NOT NULL,
	                    SettingsSetContent NVARCHAR(MAX) NOT NULL
                    );                
                ";
                command.ExecuteNonQuery();
            }
        }

        public ISettingsSet Get()
        {
            this.InitializeStructure();

            string settingsSetContent = string.Empty;
            using (var connection = new SqliteConnection($"Data Source={this.databaseFileName}"))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =
                @"
                    SELECT TOP 1 SettingsSetContent FROM [settings].SettingsSet ORDER BY rowid DESC
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
                    INSERT INTO [settings].SettingsSet
                    VALUES ('UserSettingsSet', '${serializedSettings}');
                ";
                command.ExecuteNonQuery();
            }
        }
    }
}
