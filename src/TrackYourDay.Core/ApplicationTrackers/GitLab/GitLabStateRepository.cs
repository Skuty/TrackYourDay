using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;
using TrackYourDay.Core.Persistence;
using static TrackYourDay.Core.Persistence.DatabaseConstants;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab;

/// <summary>
/// Repository for persisting GitLab state snapshots using existing database.
/// </summary>
public sealed class GitLabStateRepository : IGitLabStateRepository
{
    private readonly string _databasePath;
    private readonly ISqliteConnectionFactory _connectionFactory;

    public GitLabStateRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        
        var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _databasePath = Path.Combine(appDataPath, DatabaseName);
    }

    public async Task SaveAsync(GitLabStateSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionFactory.CreateConnectionString(_databasePath));
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var dataJson = JsonConvert.SerializeObject(snapshot, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO historical_data (Guid, TypeName, DataJson)
            VALUES (@guid, @typeName, @dataJson)";
        
        command.Parameters.AddWithValue("@guid", snapshot.Guid.ToString());
        command.Parameters.AddWithValue("@typeName", nameof(GitLabStateSnapshot));
        command.Parameters.AddWithValue("@dataJson", dataJson);
        
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitLabStateSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionFactory.CreateConnectionString(_databasePath));
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DataJson 
            FROM historical_data 
            WHERE TypeName = @typeName
            ORDER BY ROWID DESC 
            LIMIT 1";

        command.Parameters.AddWithValue("@typeName", nameof(GitLabStateSnapshot));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var dataJson = reader.GetString(0);
            return JsonConvert.DeserializeObject<GitLabStateSnapshot>(dataJson, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }

        return null;
    }

    public async Task<List<GitLabStateSnapshot>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionFactory.CreateConnectionString(_databasePath));
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DataJson 
            FROM historical_data 
            WHERE TypeName = @typeName
            ORDER BY ROWID DESC";
        
        command.Parameters.AddWithValue("@typeName", nameof(GitLabStateSnapshot));

        var snapshots = new List<GitLabStateSnapshot>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var dataJson = reader.GetString(0);
            var snapshot = JsonConvert.DeserializeObject<GitLabStateSnapshot>(dataJson, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            
            if (snapshot != null && snapshot.CapturedAt >= startDate && snapshot.CapturedAt <= endDate)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }
}
