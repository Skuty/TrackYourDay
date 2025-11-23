using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public class SqliteBreakRepository : IBreakRepository
    {
        private readonly string databaseFileName;
        private readonly ConcurrentDictionary<DateOnly, List<EndedBreak>> cache = new();

        public SqliteBreakRepository(string? customDatabasePath = null)
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

                this.databaseFileName = $"{appDataPath}\\TrackYourDayBreaks.db";
            }
            
            InitializeStructure();
        }

        public void Save(EndedBreak endedBreak)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO breaks (Guid, Date, BreakStartedAt, BreakEndedAt, BreakDescription)
                VALUES (@guid, @date, @startedAt, @endedAt, @description)";
            
            insertCommand.Parameters.AddWithValue("@guid", endedBreak.Guid.ToString());
            insertCommand.Parameters.AddWithValue("@date", endedBreak.BreakStartedAt.Date.ToString("yyyy-MM-dd"));
            insertCommand.Parameters.AddWithValue("@startedAt", endedBreak.BreakStartedAt.ToString("o"));
            insertCommand.Parameters.AddWithValue("@endedAt", endedBreak.BreakEndedAt.ToString("o"));
            insertCommand.Parameters.AddWithValue("@description", endedBreak.BreakDescription);
            insertCommand.ExecuteNonQuery();

            // Update cache
            var date = DateOnly.FromDateTime(endedBreak.BreakStartedAt.Date);
            if (!cache.ContainsKey(date))
            {
                cache[date] = new List<EndedBreak>();
            }
            cache[date].Add(endedBreak);
        }

        public IReadOnlyCollection<EndedBreak> GetBreaksForDate(DateOnly date)
        {
            if (cache.TryGetValue(date, out var cachedBreaks))
            {
                return cachedBreaks.AsReadOnly();
            }

            var breaks = new List<EndedBreak>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, BreakStartedAt, BreakEndedAt, BreakDescription 
                FROM breaks 
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

            cache[date] = breaks;
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
                FROM breaks 
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
            command.CommandText = "SELECT COUNT(*) FROM breaks";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS breaks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT UNIQUE NOT NULL,
                    Date TEXT NOT NULL,
                    BreakStartedAt TEXT NOT NULL,
                    BreakEndedAt TEXT NOT NULL,
                    BreakDescription TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_breaks_date ON breaks(Date);";
            command.ExecuteNonQuery();
        }
    }
}
