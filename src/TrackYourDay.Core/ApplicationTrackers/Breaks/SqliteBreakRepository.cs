using Microsoft.Data.Sqlite;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public class SqliteBreakRepository : IBreakRepository
    {
        private readonly string databaseFileName;

        public SqliteBreakRepository()
        {
            var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory($"{appDataPath}");
            }

            this.databaseFileName = $"{appDataPath}\\TrackYourDayGeneric.db";
            
            InitializeStructure();
        }

        public void Save(EndedBreak endedBreak)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO historical_breaks (Guid, Date, BreakStartedAt, BreakEndedAt, BreakDescription)
                VALUES (@guid, @date, @startedAt, @endedAt, @description)";
            
            insertCommand.Parameters.AddWithValue("@guid", endedBreak.Guid.ToString());
            insertCommand.Parameters.AddWithValue("@date", endedBreak.BreakStartedAt.Date.ToString("yyyy-MM-dd"));
            insertCommand.Parameters.AddWithValue("@startedAt", endedBreak.BreakStartedAt.ToString("o"));
            insertCommand.Parameters.AddWithValue("@endedAt", endedBreak.BreakEndedAt.ToString("o"));
            insertCommand.Parameters.AddWithValue("@description", endedBreak.BreakDescription);
            insertCommand.ExecuteNonQuery();
        }

        public IReadOnlyCollection<EndedBreak> GetBreaksForDate(DateOnly date)
        {
            var breaks = new List<EndedBreak>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, BreakStartedAt, BreakEndedAt, BreakDescription 
                FROM historical_breaks 
                WHERE Date = @date
                ORDER BY BreakStartedAt";
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var guid = Guid.Parse(reader.GetString(0));
                var startedAt = DateTime.Parse(reader.GetString(1));
                var endedAt = DateTime.Parse(reader.GetString(2));
                var description = reader.GetString(3);

                breaks.Add(new EndedBreak(guid, startedAt, endedAt, description));
            }

            return breaks.AsReadOnly();
        }

        public IReadOnlyCollection<EndedBreak> GetBreaksBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var breaks = new List<EndedBreak>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, BreakStartedAt, BreakEndedAt, BreakDescription 
                FROM historical_breaks 
                WHERE Date >= @startDate AND Date <= @endDate
                ORDER BY BreakStartedAt";
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var guid = Guid.Parse(reader.GetString(0));
                var startedAt = DateTime.Parse(reader.GetString(1));
                var endedAt = DateTime.Parse(reader.GetString(2));
                var description = reader.GetString(3);

                breaks.Add(new EndedBreak(guid, startedAt, endedAt, description));
            }

            return breaks.AsReadOnly();
        }

        public void Clear()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM historical_breaks";
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
            command.CommandText = "SELECT COUNT(*) FROM historical_breaks";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS historical_breaks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT UNIQUE NOT NULL,
                    Date TEXT NOT NULL,
                    BreakStartedAt TEXT NOT NULL,
                    BreakEndedAt TEXT NOT NULL,
                    BreakDescription TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_historical_breaks_date ON historical_breaks(Date);";
            command.ExecuteNonQuery();
        }
    }
}
