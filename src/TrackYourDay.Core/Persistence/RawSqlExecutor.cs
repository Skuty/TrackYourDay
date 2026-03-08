using Microsoft.Data.Sqlite;
using System.Text;
using static TrackYourDay.Core.Persistence.DatabaseConstants;

namespace TrackYourDay.Core.Persistence;

/// <inheritdoc />
public sealed class RawSqlExecutor : IRawSqlExecutor
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly string _databaseFileName;

    public RawSqlExecutor(ISqliteConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;

        var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");
        _databaseFileName = $"{appDataPath}\\{DatabaseName}";
    }

    /// <inheritdoc />
    public async Task<RawSqlResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return new RawSqlResult(false, "SQL statement cannot be empty.");

        try
        {
            var connectionString = _connectionFactory.CreateConnectionString(_databaseFileName);
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var trimmed = sql.TrimStart();
            if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);
            }

            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return new RawSqlResult(true, $"Success. {affected} row(s) affected.");
        }
        catch (SqliteException ex)
        {
            return new RawSqlResult(false, $"SQLite error ({ex.SqliteErrorCode}): {ex.Message}");
        }
        catch (Exception ex)
        {
            return new RawSqlResult(false, $"Error: {ex.Message}");
        }
    }

    private static async Task<RawSqlResult> ExecuteReaderAsync(
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var columns = Enumerable.Range(0, reader.FieldCount)
                                .Select(reader.GetName)
                                .ToArray();
        sb.AppendLine(string.Join("\t", columns));
        sb.AppendLine(new string('-', Math.Max(columns.Sum(c => c.Length) + columns.Length, 20)));

        var rowCount = 0;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var values = Enumerable.Range(0, reader.FieldCount)
                                   .Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString() ?? string.Empty);
            sb.AppendLine(string.Join("\t", values));
            rowCount++;
        }

        sb.AppendLine();
        sb.Append($"({rowCount} row(s) returned)");

        return new RawSqlResult(true, sb.ToString());
    }
}
