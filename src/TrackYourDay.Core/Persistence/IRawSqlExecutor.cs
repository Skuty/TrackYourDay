namespace TrackYourDay.Core.Persistence;

/// <summary>
/// Result of a raw SQL statement execution.
/// </summary>
public record RawSqlResult(bool Success, string Message);

/// <summary>
/// Executes raw SQL statements against the application database.
/// </summary>
public interface IRawSqlExecutor
{
    /// <summary>
    /// Executes a raw SQL statement and returns the result.
    /// SELECT statements return rows as tab-separated values.
    /// Other statements return the number of affected rows.
    /// </summary>
    Task<RawSqlResult> ExecuteAsync(string sql, CancellationToken cancellationToken = default);
}
