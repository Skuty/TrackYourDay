using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.MAUI.Infrastructure.Persistence
{
    public sealed class JiraActivityRepository : IJiraActivityRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<JiraActivityRepository> _logger;

        public JiraActivityRepository(string databasePath, ILogger<JiraActivityRepository> logger)
        {
            _connectionString = $"Data Source={databasePath}";
            _logger = logger;
            EnsureTableExists();
        }

        public async Task<bool> TryAppendAsync(JiraActivity activity, CancellationToken cancellationToken)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT OR IGNORE INTO JiraActivities (Guid, OccurrenceDate, Description, DataJson)
                VALUES ($guid, $occurrenceDate, $description, $dataJson)";

            insertCommand.Parameters.AddWithValue("$guid", activity.Guid.ToString());
            insertCommand.Parameters.AddWithValue("$occurrenceDate", activity.OccurrenceDate);
            insertCommand.Parameters.AddWithValue("$description", activity.Description);
            insertCommand.Parameters.AddWithValue("$dataJson", JsonSerializer.Serialize(activity));

            var rowsAffected = await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            
            if (rowsAffected > 0)
            {
                _logger.LogDebug("Appended Jira activity {Guid}", activity.Guid);
                return true;
            }

            _logger.LogDebug("Jira activity {Guid} already exists", activity.Guid);
            return false;
        }

        public async Task<IReadOnlyCollection<JiraActivity>> GetActivitiesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
                SELECT DataJson 
                FROM JiraActivities 
                WHERE DATE(OccurrenceDate) >= DATE($fromDate) 
                  AND DATE(OccurrenceDate) <= DATE($toDate)
                ORDER BY OccurrenceDate ASC";

            selectCommand.Parameters.AddWithValue("$fromDate", fromDate.ToString("yyyy-MM-dd"));
            selectCommand.Parameters.AddWithValue("$toDate", toDate.ToString("yyyy-MM-dd"));

            var activities = new List<JiraActivity>();

            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var json = reader.GetString(0);
                var activity = JsonSerializer.Deserialize<JiraActivity>(json);
                if (activity != null)
                {
                    activities.Add(activity);
                }
            }

            return activities;
        }

        private void EnsureTableExists()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS JiraActivities (
                    Guid TEXT PRIMARY KEY,
                    OccurrenceDate TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    DataJson TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_jira_occurrence_date 
                ON JiraActivities(OccurrenceDate);";

            createTableCommand.ExecuteNonQuery();
            _logger.LogInformation("JiraActivities table initialized");
        }
    }
}
