using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.Persistence;
using static TrackYourDay.Core.Persistence.DatabaseConstants;

namespace TrackYourDay.MAUI.Infrastructure.Persistence
{
    public sealed class JiraIssueRepository : IJiraIssueRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<JiraIssueRepository> _logger;

        public JiraIssueRepository(
            ISqliteConnectionFactory connectionFactory, 
            ILogger<JiraIssueRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);
            ArgumentNullException.ThrowIfNull(logger);
            
            _connectionString = connectionFactory.CreateConnectionString(DatabaseName);
            _logger = logger;
            EnsureTableExists();
        }

        public async Task UpdateCurrentStateAsync(IEnumerable<JiraIssueState> currentIssues, CancellationToken cancellationToken)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var markCommand = connection.CreateCommand();
                markCommand.Transaction = transaction;
                markCommand.CommandText = "UPDATE JiraIssues SET IsPotentiallyRemoved = 1";
                await markCommand.ExecuteNonQueryAsync(cancellationToken);

                foreach (var issue in currentIssues)
                {
                    var upsertCommand = connection.CreateCommand();
                    upsertCommand.Transaction = transaction;
                    upsertCommand.CommandText = @"
                        INSERT INTO JiraIssues (IssueKey, IssueId, Summary, Status, IssueType, ProjectKey, Updated, Created, AssigneeDisplayName, DataJson, IsPotentiallyRemoved)
                        VALUES ($key, $id, $summary, $status, $issueType, $projectKey, $updated, $created, $assignee, $dataJson, 0)
                        ON CONFLICT(IssueKey) DO UPDATE SET
                            IssueId = excluded.IssueId,
                            Summary = excluded.Summary,
                            Status = excluded.Status,
                            IssueType = excluded.IssueType,
                            ProjectKey = excluded.ProjectKey,
                            Updated = excluded.Updated,
                            Created = excluded.Created,
                            AssigneeDisplayName = excluded.AssigneeDisplayName,
                            DataJson = excluded.DataJson,
                            IsPotentiallyRemoved = 0";

                    upsertCommand.Parameters.AddWithValue("$key", issue.Key);
                    upsertCommand.Parameters.AddWithValue("$id", issue.Id);
                    upsertCommand.Parameters.AddWithValue("$summary", issue.Summary);
                    upsertCommand.Parameters.AddWithValue("$status", issue.Status);
                    upsertCommand.Parameters.AddWithValue("$issueType", issue.IssueType);
                    upsertCommand.Parameters.AddWithValue("$projectKey", issue.ProjectKey);
                    upsertCommand.Parameters.AddWithValue("$updated", issue.Updated.ToString("O"));
                    upsertCommand.Parameters.AddWithValue("$created", issue.Created?.ToString("O") ?? (object)DBNull.Value);
                    upsertCommand.Parameters.AddWithValue("$assignee", issue.AssigneeDisplayName ?? (object)DBNull.Value);
                    upsertCommand.Parameters.AddWithValue("$dataJson", JsonSerializer.Serialize(issue));

                    await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                var deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM JiraIssues WHERE IsPotentiallyRemoved = 1";
                var deletedCount = await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogInformation("Updated Jira current state: {Count} issues, {Deleted} removed", 
                    currentIssues.Count(), deletedCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<JiraIssueState>> GetCurrentIssuesAsync(CancellationToken cancellationToken)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
                SELECT DataJson 
                FROM JiraIssues 
                WHERE IsPotentiallyRemoved = 0
                ORDER BY Updated DESC";

            var issues = new List<JiraIssueState>();

            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var json = reader.GetString(0);
                var issue = JsonSerializer.Deserialize<JiraIssueState>(json);
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private void EnsureTableExists()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS JiraIssues (
                    IssueKey TEXT PRIMARY KEY,
                    IssueId TEXT NOT NULL,
                    Summary TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    IssueType TEXT NOT NULL,
                    ProjectKey TEXT NOT NULL,
                    Updated TEXT NOT NULL,
                    Created TEXT,
                    AssigneeDisplayName TEXT,
                    DataJson TEXT NOT NULL,
                    IsPotentiallyRemoved INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_jira_issues_updated 
                ON JiraIssues(Updated);
                CREATE INDEX IF NOT EXISTS idx_jira_issues_project 
                ON JiraIssues(ProjectKey);";

            createTableCommand.ExecuteNonQuery();
            _logger.LogInformation("JiraIssues table initialized");
        }
    }
}
