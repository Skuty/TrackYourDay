using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class SqliteMeetingRepository : IMeetingRepository
    {
        private readonly string databaseFileName;
        private readonly ConcurrentDictionary<DateOnly, List<EndedMeeting>> cache = new();

        public SqliteMeetingRepository(string? customDatabasePath = null)
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

                this.databaseFileName = $"{appDataPath}\\TrackYourDayMeetings.db";
            }
            
            InitializeStructure();
        }

        public void Save(EndedMeeting meeting)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO meetings (Guid, Date, StartDate, EndDate, Title)
                VALUES (@guid, @date, @startDate, @endDate, @title)";
            
            insertCommand.Parameters.AddWithValue("@guid", meeting.Guid.ToString());
            insertCommand.Parameters.AddWithValue("@date", meeting.StartDate.Date.ToString("yyyy-MM-dd"));
            insertCommand.Parameters.AddWithValue("@startDate", meeting.StartDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@endDate", meeting.EndDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@title", meeting.Title);
            insertCommand.ExecuteNonQuery();

            // Update cache
            var date = DateOnly.FromDateTime(meeting.StartDate.Date);
            if (!cache.ContainsKey(date))
            {
                cache[date] = new List<EndedMeeting>();
            }
            cache[date].Add(meeting);
        }

        public IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date)
        {
            if (cache.TryGetValue(date, out var cachedMeetings))
            {
                return cachedMeetings.AsReadOnly();
            }

            var meetings = new List<EndedMeeting>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, StartDate, EndDate, Title 
                FROM meetings 
                WHERE Date = @date
                ORDER BY StartDate";
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var guid = Guid.Parse(reader.GetString(0));
                var startDate = DateTime.Parse(reader.GetString(1));
                var endDate = DateTime.Parse(reader.GetString(2));
                var title = reader.GetString(3);

                meetings.Add(new EndedMeeting(guid, startDate, endDate, title));
            }

            cache[date] = meetings;
            return meetings.AsReadOnly();
        }

        public IReadOnlyCollection<EndedMeeting> GetMeetingsBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var meetings = new List<EndedMeeting>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, StartDate, EndDate, Title 
                FROM meetings 
                WHERE Date >= @startDate AND Date <= @endDate
                ORDER BY StartDate";
            command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var guid = Guid.Parse(reader.GetString(0));
                var start = DateTime.Parse(reader.GetString(1));
                var end = DateTime.Parse(reader.GetString(2));
                var title = reader.GetString(3);

                meetings.Add(new EndedMeeting(guid, start, end, title));
            }

            return meetings.AsReadOnly();
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
            command.CommandText = "SELECT COUNT(*) FROM meetings";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS meetings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT UNIQUE NOT NULL,
                    Date TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    Title TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_meetings_date ON meetings(Date);";
            command.ExecuteNonQuery();
        }
    }
}
