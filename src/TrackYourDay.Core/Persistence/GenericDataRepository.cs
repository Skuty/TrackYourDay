using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Persistence
{
    /// <summary>
    /// Generic repository that handles both current session data (from trackers) 
    /// and historical persisted data (from SQLite database).
    /// Type-specific repository that implements IHistoricalDataRepository<T>.
    /// Supports the Specification Pattern for flexible querying.
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
        /// Updates an existing item in the database by its Guid.
        /// </summary>
        public void Update(T item)
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

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE historical_data 
                SET DataJson = @dataJson
                WHERE Guid = @guid AND TypeName = @typeName";
            
            updateCommand.Parameters.AddWithValue("@guid", guid.ToString());
            updateCommand.Parameters.AddWithValue("@typeName", typeName);
            updateCommand.Parameters.AddWithValue("@dataJson", dataJson);
            
            var rowsAffected = updateCommand.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"No record found with Guid {guid} for type {typeName}");
            }
        }

        /// <summary>
        /// Queries data using a specification pattern.
        /// Applies the specification to both in-memory tracker data (if today) and database data.
        /// </summary>
        public IReadOnlyCollection<T> Find(ISpecification<T> specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            var items = new List<T>();

            // Get from database using specification
            items.AddRange(GetFromDatabaseBySpecification(specification));

            // If we have current session data, filter it using the specification
            if (getCurrentSessionData != null)
            {
                var currentData = getCurrentSessionData()
                    .Where(item => specification.IsSatisfiedBy(item))
                    .ToList();
                
                // Add current session items that aren't already in the results (avoid duplicates)
                var existingGuids = items
                    .Select(item => GetGuidFromItem(item))
                    .ToHashSet();
                
                items.AddRange(currentData.Where(item => !existingGuids.Contains(GetGuidFromItem(item))));
            }

            return items.AsReadOnly();
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
        /// Queries the database using a specification pattern.
        /// </summary>
        private IReadOnlyCollection<T> GetFromDatabaseBySpecification(ISpecification<T> specification)
        {
            var items = new List<T>();

            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            
            // Build query with specification
            var whereClause = specification.GetSqlWhereClause();
            command.CommandText = $@"
                SELECT DataJson 
                FROM historical_data 
                WHERE TypeName = @typeName 
                  AND ({whereClause})
                ORDER BY Id";
            
            command.Parameters.AddWithValue("@typeName", typeName);
            
            // Add specification parameters
            var specParams = specification.GetSqlParameters();
            foreach (var param in specParams)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }

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
        /// Extracts the Guid from an entity using reflection.
        /// </summary>
        private Guid GetGuidFromItem(T item)
        {
            var guidProperty = typeof(T).GetProperty("Guid");
            if (guidProperty == null)
                return Guid.Empty;
            
            return (Guid)guidProperty.GetValue(item)!;
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
