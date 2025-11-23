using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace TrackYourDay.Core.Persistence
{
    public class SqliteHistoricalDataRepository<T> : IHistoricalDataRepository<T> where T : class
    {
        private readonly string databaseFileName;
        private readonly string typeName;

        public SqliteHistoricalDataRepository()
        {
            var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory($"{appDataPath}");
            }

            this.databaseFileName = $"{appDataPath}\\TrackYourDayGeneric.db";
            this.typeName = typeof(T).Name;
            
            InitializeStructure();
        }

        public void Save(T item)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            // Extract Guid and Date from the item using reflection
            var guidProperty = typeof(T).GetProperty("Guid");
            var startDateProperty = typeof(T).GetProperty("StartDate") ?? 
                                   typeof(T).GetProperty("BreakStartedAt");
            
            if (guidProperty == null || startDateProperty == null)
            {
                throw new InvalidOperationException($"Type {typeName} must have Guid and StartDate/BreakStartedAt properties");
            }

            var guid = (Guid)guidProperty.GetValue(item)!;
            var startDate = (DateTime)startDateProperty.GetValue(item)!;
            var date = startDate.Date.ToString("yyyy-MM-dd");
            var dataJson = JsonConvert.SerializeObject(item, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO historical_data (Guid, TypeName, Date, DataJson)
                VALUES (@guid, @typeName, @date, @dataJson)";
            
            insertCommand.Parameters.AddWithValue("@guid", guid.ToString());
            insertCommand.Parameters.AddWithValue("@typeName", typeName);
            insertCommand.Parameters.AddWithValue("@date", date);
            insertCommand.Parameters.AddWithValue("@dataJson", dataJson);
            insertCommand.ExecuteNonQuery();
        }

        public IReadOnlyCollection<T> GetForDate(DateOnly date)
        {
            var items = new List<T>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DataJson 
                FROM historical_data 
                WHERE Date = @date AND TypeName = @typeName
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

        public IReadOnlyCollection<T> GetBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var items = new List<T>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DataJson 
                FROM historical_data 
                WHERE Date >= @startDate AND Date <= @endDate AND TypeName = @typeName
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

        public void Clear()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM historical_data WHERE TypeName = @typeName";
            clearCommand.Parameters.AddWithValue("@typeName", typeName);
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
            command.CommandText = "SELECT COUNT(*) FROM historical_data WHERE TypeName = @typeName";
            command.Parameters.AddWithValue("@typeName", typeName);
            return Convert.ToInt32(command.ExecuteScalar());
        }

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
                    Date TEXT NOT NULL,
                    DataJson TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_historical_data_date_type ON historical_data(Date, TypeName);
                CREATE INDEX IF NOT EXISTS idx_historical_data_guid ON historical_data(Guid);";
            command.ExecuteNonQuery();
        }
    }
}
