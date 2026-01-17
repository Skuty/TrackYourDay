namespace TrackYourDay.Core.Persistence;

/// <summary>
/// Creates SQLite connection strings with appropriate security configuration.
/// </summary>
public interface ISqliteConnectionFactory
{
    /// <summary>
    /// Creates a connection string for the specified database path.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <returns>Connection string with security parameters applied.</returns>
    string CreateConnectionString(string databasePath);
}
