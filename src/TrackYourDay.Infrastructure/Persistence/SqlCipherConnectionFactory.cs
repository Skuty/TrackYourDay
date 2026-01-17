using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Infrastructure.Persistence;

/// <summary>
/// Creates SQLite connection strings with SQLCipher encryption enabled.
/// </summary>
public sealed class SqlCipherConnectionFactory : ISqliteConnectionFactory
{
    private readonly IDatabaseKeyProvider _keyProvider;

    public SqlCipherConnectionFactory(IDatabaseKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <inheritdoc />
    public string CreateConnectionString(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        
        var key = _keyProvider.GetDatabaseKey();
        return $"Data Source={databasePath};Password={key}";
    }
}
