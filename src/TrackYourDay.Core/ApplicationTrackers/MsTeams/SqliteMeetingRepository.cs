using Microsoft.Data.Sqlite;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class SqliteMeetingRepository : IMeetingRepository
    {
        private readonly string databaseFileName;

        public SqliteMeetingRepository()
        {
            var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory($"{appDataPath}");
            }

            this.databaseFileName = $"{appDataPath}\\TrackYourDayGeneric.db";
            InitializeStructure();
        }

        public void Save(EndedMeeting meeting)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO historical_meetings (Guid, Date, StartDate, EndDate, Title, Description)
                VALUES (@guid, @date, @startDate, @endDate, @title, @description)";
            
            insertCommand.Parameters.AddWithValue("@guid", meeting.Guid.ToString());
            insertCommand.Parameters.AddWithValue("@date", meeting.StartDate.Date.ToString("yyyy-MM-dd"));
            insertCommand.Parameters.AddWithValue("@startDate", meeting.StartDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@endDate", meeting.EndDate.ToString("o"));
            insertCommand.Parameters.AddWithValue("@title", meeting.Title);
            insertCommand.Parameters.AddWithValue("@description", meeting.Description);
            insertCommand.ExecuteNonQuery();
        }

        public IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date)
        {
            var meetings = new List<EndedMeeting>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, StartDate, EndDate, Title, Description 
                FROM historical_meetings 
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
                var description = reader.GetString(4);

                var meeting = new EndedMeeting(guid, startDate, endDate, title);
                meeting.SetDescription(description);
                meetings.Add(meeting);
            }

            return meetings.AsReadOnly();
        }

        public IReadOnlyCollection<EndedMeeting> GetMeetingsBetweenDates(DateOnly startDate, DateOnly endDate)
        {
            var meetings = new List<EndedMeeting>();
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Guid, StartDate, EndDate, Title, Description 
                FROM historical_meetings 
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
                var title = reader.GetString(3);
                var description = reader.GetString(4);

                var meeting = new EndedMeeting(guid, startDateVal, endDateVal, title);
                meeting.SetDescription(description);
                meetings.Add(meeting);
            }

            return meetings.AsReadOnly();
        }

        public void Clear()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = "DELETE FROM historical_meetings";
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
            command.CommandText = "SELECT COUNT(*) FROM historical_meetings";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void InitializeStructure()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFileName}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS historical_meetings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Guid TEXT UNIQUE NOT NULL,
                    Date TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Description TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_historical_meetings_date ON historical_meetings(Date);";
            command.ExecuteNonQuery();
        }
    }
}
