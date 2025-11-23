using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers
{
    public class SqliteActivityRepository : IActivityRepository
    {
        private readonly string databaseFileName;
        private readonly ConcurrentDictionary<DateOnly, List<EndedActivity>> cache = new();

        public SqliteActivityRepository(string? customDatabasePath = null)
        {
            if (customDatabasePath != null)
            {
                this.databaseFileName = customDatabasePath;
            }
            else
            {
                var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory($"{appDataPath}");
                }

                this.databaseFileName = $"{appDataPath}\\TrackYourDayActivities.db";
            }
            
            InitializeStructure();
        }

        public void Save(EndedActivity activity)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO activities (Date, StartDate, EndDate, ActivityDescription, ActivityType)
                VALUES (@date, @startDate, @endDate, @description, @type)";
            
            insertCommand.Parameters.AddWithValue("@date", activity.StartDate.Date.ToString("yyyy-MM-dd"));
            insertCommand.Parameters.AddWithValue("@startDate", activity.StartDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@endDate", activity.EndDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@description", activity.ActivityType.ActivityDescription);
            insertCommand.Parameters.AddWithValue("@type", activity.ActivityType.GetType().Name);
            insertCommand.ExecuteNonQuery();

            // Update cache
            var date = DateOnly.FromDateTime(activity.StartDate.Date);
            if (!cache.ContainsKey(date))
            {
                cache[date] = new List<EndedActivity>();
            }
            cache[date].Add(activity);
        }

        public IReadOnlyCollection<EndedActivity> GetActivitiesForDate(DateOnly date)
        {
            if (cache.TryGetValue(date, out var cachedActivities))
            {
                return cachedActivities.AsReadOnly();
            }

            var activities = new List<EndedActivity>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT StartDate, EndDate, ActivityDescription, ActivityType 
                FROM activities 
                WHERE Date = @date
                ORDER BY StartDate";
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var startDate = DateTime.Parse(reader.GetString(0));
                var endDate = DateTime.Parse(reader.GetString(1));
                var description = reader.GetString(2);
                var type = reader.GetString(3);

                // Reconstruct the SystemState based on the type
                SystemState systemState = type switch
                {
                    nameof(SystemLockedState) => SystemStateFactory.SystemLockedState(),
                    _ => SystemStateFactory.FocusOnApplicationState(description)
                };

                activities.Add(new EndedActivity(startDate, endDate, systemState));
            }

            cache[date] = activities;
            return activities.AsReadOnly();
        }

        public IReadOnlyCollection<EndedActivity> GetActivitiesBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var activities = new List<EndedActivity>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT StartDate, EndDate, ActivityDescription, ActivityType 
                FROM activities 
                WHERE Date >= @startDate AND Date <= @endDate
                ORDER BY StartDate";
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var start = DateTime.Parse(reader.GetString(0));
                var end = DateTime.Parse(reader.GetString(1));
                var description = reader.GetString(2);
                var type = reader.GetString(3);

                SystemState systemState = type switch
                {
                    nameof(SystemLockedState) => SystemStateFactory.SystemLockedState(),
                    _ => SystemStateFactory.FocusOnApplicationState(description)
                };

                activities.Add(new EndedActivity(start, end, systemState));
            }

            return activities.AsReadOnly();
        }

        public void Clear()
        {
            cache.Clear();
            
            if (File.Exists(databaseFileName))
            {
                File.Delete(databaseFileName);
            }

            InitializeStructure();
        }

        public long GetDatabaseSizeInBytes()
        {
            if (File.Exists(databaseFileName))
            {
                return new FileInfo(databaseFileName).Length;
            }
            return 0;
        }

        public int GetTotalRecordCount()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM activities";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS activities (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    ActivityDescription TEXT NOT NULL,
                    ActivityType TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_activities_date ON activities(Date);";
            command.ExecuteNonQuery();
        }
    }
}
