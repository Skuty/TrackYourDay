using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.Persistence;

namespace TrackYourDay.MAUI.Infrastructure.Persistence
{
    public sealed class GitLabActivityRepository : IGitLabActivityRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<GitLabActivityRepository> _logger;

        public GitLabActivityRepository(
            ISqliteConnectionFactory connectionFactory, 
            ILogger<GitLabActivityRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);
            ArgumentNullException.ThrowIfNull(logger);
            
            _connectionString = connectionFactory.CreateConnectionString("TrackYourDay.db");
            _logger = logger;
            EnsureTableExists();
        }

        public async Task<bool> TryAppendAsync(GitLabActivity activity, CancellationToken cancellationToken)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT OR IGNORE INTO GitLabActivities (Guid, OccurrenceDate, Description, DataJson)
                VALUES ($guid, $occurrenceDate, $description, $dataJson)";

            insertCommand.Parameters.AddWithValue("$guid", activity.Guid.ToString());
            insertCommand.Parameters.AddWithValue("$occurrenceDate", activity.OccuranceDate);
            insertCommand.Parameters.AddWithValue("$description", activity.Description);
            insertCommand.Parameters.AddWithValue("$dataJson", JsonSerializer.Serialize(activity));

            var rowsAffected = await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            
            if (rowsAffected > 0)
            {
                _logger.LogDebug("Appended GitLab activity {Guid}", activity.Guid);
                return true;
            }

            _logger.LogDebug("GitLab activity {Guid} already exists", activity.Guid);
            return false;
        }

        public async Task<IReadOnlyCollection<GitLabActivity>> GetActivitiesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
                SELECT DataJson 
                FROM GitLabActivities 
                WHERE DATE(OccurrenceDate) >= DATE($fromDate) 
                  AND DATE(OccurrenceDate) <= DATE($toDate)
                ORDER BY OccurrenceDate ASC";

            selectCommand.Parameters.AddWithValue("$fromDate", fromDate.ToString("yyyy-MM-dd"));
            selectCommand.Parameters.AddWithValue("$toDate", toDate.ToString("yyyy-MM-dd"));

            var activities = new List<GitLabActivity>();

            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var json = reader.GetString(0);
                var activity = JsonSerializer.Deserialize<GitLabActivity>(json);
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
                CREATE TABLE IF NOT EXISTS GitLabActivities (
                    Guid TEXT PRIMARY KEY,
                    OccurrenceDate TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    DataJson TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_gitlab_occurrence_date 
                ON GitLabActivities(OccurrenceDate);";

            createTableCommand.ExecuteNonQuery();
            _logger.LogInformation("GitLabActivities table initialized");
        }
    }
}
