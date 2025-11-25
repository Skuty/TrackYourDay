using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Persistence
{
    /// <summary>
    /// Generic repository that handles both current session data (from trackers) 
    /// and historical persisted data (from SQLite database).
    /// Type-specific repository that implements IHistoricalDataRepository<T>.
    /// </summary>
    public class GenericDataRepository<T> : IHistoricalDataRepository<T> where T : class
    {
        private readonly string databaseFileName;
        private readonly IClock clock;
        private readonly string typeName;
        private readonly Func<IReadOnlyCollection<T>>? getCurrentSessionData;

        public GenericDataRepository(
            IClock clock,
            Func<IReadOnlyCollection<T>>? getCurrentSessionDataProvider = null)
        {
            this.clock = clock;
            this.typeName = typeof(T).Name;
            this.getCurrentSessionData = getCurrentSessionDataProvider;

            var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory($"{appDataPath}");
            }

            this.databaseFileName = $"{appDataPath}\\TrackYourDayGeneric.db";
            InitializeStructure();
        }

        /// <summary>
        /// Saves an item to the database.
        /// </summary>
        public void Save(T item)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var guidProperty = typeof(T).GetProperty("Guid");
            
            if (guidProperty == null)
            {
                throw new InvalidOperationException($"Type {typeName} must have a Guid property");
            }

            var guid = (Guid)guidProperty.GetValue(item)!;
            var dataJson = JsonConvert.SerializeObject(item, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO historical_data (Guid, TypeName, DataJson)
                VALUES (@guid, @typeName, @dataJson)";
            
            insertCommand.Parameters.AddWithValue("@guid", guid.ToString());
            insertCommand.Parameters.AddWithValue("@typeName", typeName);
            insertCommand.Parameters.AddWithValue("@dataJson", dataJson);
            insertCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets data for a specific date. 
        /// If the date is today and tracker is available, returns from in-memory tracker.
        /// Otherwise, returns from persisted database.
        /// </summary>
        public IReadOnlyCollection<T> GetForDate(DateOnly date)
        {
            var today = DateOnly.FromDateTime(clock.Now.Date);

            // If requesting today's data and we have a tracker, get from tracker (in-memory)
            if (date == today && getCurrentSessionData != null)
            {
                return getCurrentSessionData();
            }

            // For historical data, query from database
            return GetFromDatabase(date);
        }

        /// <summary>
        /// Gets data between two dates.
        /// Combines tracker data (for today) with database data (for historical dates).
        /// </summary>
        public IReadOnlyCollection<T> GetBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var today = DateOnly.FromDateTime(clock.Now.Date);

            // If the range includes today, combine database + tracker data
            if (endDate >= today && getCurrentSessionData != null)
            {
                var allData = new List<T>();

                // Get historical data from database (if start date is before today)
                if (startDate < today)
                {
                    var dbEndDate = endDate >= today ? today.AddDays(-1) : endDate;
                    allData.AddRange(GetFromDatabaseBetweenDates(startDate, dbEndDate));
                }

                // Add today's data from tracker (if today is in range)
                if (endDate >= today)
                {
                    allData.AddRange(getCurrentSessionData());
                }

                return allData;
            }

            // All historical data - get from database only
            return GetFromDatabaseBetweenDates(startDate, endDate);
        }

        /// <summary>
        /// Clears all data of type T from the database.
        /// </summary>
        public void Clear()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM historical_data WHERE TypeName = @typeName";
            clearCommand.Parameters.AddWithValue("@typeName", typeName);
            clearCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets the total size of the database file in bytes.
        /// </summary>
        public long GetDatabaseSizeInBytes()
        {
            if (File.Exists(databaseFileName))
            {
                return new FileInfo(databaseFileName).Length;
            }
            return 0;
        }

        /// <summary>
        /// Gets the total count of records for type T in the database.
        /// </summary>
        public int GetTotalRecordCount()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM historical_data WHERE TypeName = @typeName";
            command.Parameters.AddWithValue("@typeName", typeName);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Queries the database for data on a specific date using JSON extraction.
        /// </summary>
        private IReadOnlyCollection<T> GetFromDatabase(DateOnly date)
        {
            var items = new List<T>();

            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DataJson 
                FROM historical_data 
                WHERE TypeName = @typeName 
                  AND (
                    date(json_extract(DataJson, '$.StartDate')) = @date 
                    OR date(json_extract(DataJson, '$.BreakStartedAt')) = @date
                  )
                ORDER BY Id";
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@typeName", typeName);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var dataJson = reader.GetString(0);
                var item = JsonConvert.DeserializeObject<T>(dataJson, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items.AsReadOnly();
        }

        /// <summary>
        /// Queries the database for data between two dates using JSON extraction.
        /// </summary>
        private IReadOnlyCollection<T> GetFromDatabaseBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var items = new List<T>();

            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DataJson 
                FROM historical_data 
                WHERE TypeName = @typeName 
                  AND (
                    (date(json_extract(DataJson, '$.StartDate')) >= @startDate 
                     AND date(json_extract(DataJson, '$.StartDate')) <= @endDate)
                    OR 
                    (date(json_extract(DataJson, '$.BreakStartedAt')) >= @startDate 
                     AND date(json_extract(DataJson, '$.BreakStartedAt')) <= @endDate)
                  )
                ORDER BY Id";
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@typeName", typeName);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var dataJson = reader.GetString(0);
                var item = JsonConvert.DeserializeObject<T>(dataJson, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items.AsReadOnly();
        }

        /// <summary>
        /// Initializes the database schema if it doesn't exist.
        /// </summary>
        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS historical_data (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT NOT NULL,
                    TypeName TEXT NOT NULL,
                    DataJson TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_historical_data_guid ON historical_data(Guid);
                CREATE INDEX IF NOT EXISTS idx_historical_data_type ON historical_data(TypeName);";
            command.ExecuteNonQuery();
        }
    }
}
