using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers
{
    public class SqliteActivityRepository : IActivityRepository
    {
        private readonly string databaseFileName;

        public SqliteActivityRepository()
        {
            var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory($"{appDataPath}");
            }

            this.databaseFileName = $"{appDataPath}\\TrackYourDayGeneric.db";
            InitializeStructure();
        }

        public void Save(EndedActivity activity)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO historical_activities (Guid, Date, StartDate, EndDate, ActivityTypeJson, ActivityDescription)
                VALUES (@guid, @date, @startDate, @endDate, @activityTypeJson, @description)";
            
            insertCommand.Parameters.AddWithValue("@guid", activity.Guid.ToString());
            insertCommand.Parameters.AddWithValue("@date", activity.StartDate.Date.ToString("yyyy-MM-dd"));
            insertCommand.Parameters.AddWithValue("@startDate", activity.StartDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@endDate", activity.EndDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@activityTypeJson", JsonConvert.SerializeObject(activity.ActivityType));
            insertCommand.Parameters.AddWithValue("@description", activity.ActivityType.ActivityDescription);
            insertCommand.ExecuteNonQuery();
        }

        public IReadOnlyCollection<EndedActivity> GetActivitiesForDate(DateOnly date)
        {
            var activities = new List<EndedActivity>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, StartDate, EndDate, ActivityTypeJson 
                FROM historical_activities 
                WHERE Date = @date
                ORDER BY StartDate";
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var guid = Guid.Parse(reader.GetString(0));
                var startDate = DateTime.Parse(reader.GetString(1));
                var endDate = DateTime.Parse(reader.GetString(2));
                var activityTypeJson = reader.GetString(3);
                var activityType = JsonConvert.DeserializeObject<SystemState>(activityTypeJson, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

                var activity = new EndedActivity(startDate, endDate, activityType) { Guid = guid };
                activities.Add(activity);
            }

            return activities.AsReadOnly();
        }

        public IReadOnlyCollection<EndedActivity> GetActivitiesBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var activities = new List<EndedActivity>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, StartDate, EndDate, ActivityTypeJson 
                FROM historical_activities 
                WHERE Date >= @startDate AND Date <= @endDate
                ORDER BY StartDate";
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var guid = Guid.Parse(reader.GetString(0));
                var startDateVal = DateTime.Parse(reader.GetString(1));
                var endDateVal = DateTime.Parse(reader.GetString(2));
                var activityTypeJson = reader.GetString(3);
                var activityType = JsonConvert.DeserializeObject<SystemState>(activityTypeJson, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

                var activity = new EndedActivity(startDateVal, endDateVal, activityType) { Guid = guid };
                activities.Add(activity);
            }

            return activities.AsReadOnly();
        }

        public void Clear()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM historical_activities";
            clearCommand.ExecuteNonQuery();
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
            command.CommandText = "SELECT COUNT(*) FROM historical_activities";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS historical_activities (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT UNIQUE NOT NULL,
                    Date TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    ActivityTypeJson TEXT NOT NULL,
                    ActivityDescription TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_historical_activities_date ON historical_activities(Date);";
            command.ExecuteNonQuery();
        }
    }
}
