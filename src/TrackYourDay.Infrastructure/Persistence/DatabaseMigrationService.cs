using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Infrastructure.Persistence;

/// <summary>
/// Handles migration of existing unencrypted SQLite databases to encrypted format.
/// </summary>
public sealed class DatabaseMigrationService(
    IDatabaseKeyProvider keyProvider,
    ILogger<DatabaseMigrationService> logger)
{
    /// <summary>
    /// Migrates an unencrypted database to encrypted format using SQLCipher's PRAGMA rekey.
    /// Creates a backup before migration. If migration fails, backup is preserved.
    /// </summary>
    /// <param name="databasePath">Path to the database file to encrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task MigrateToEncryptedAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        
        if (!File.Exists(databasePath))
        {
            logger.LogInformation("Database {DatabasePath} does not exist, skipping migration", databasePath);
            return;
        }

        var backupPath = $"{databasePath}.backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        
        try
        {
            File.Copy(databasePath, backupPath, overwrite: false);
            logger.LogInformation("Created backup at {BackupPath}", backupPath);

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA rekey = '{keyProvider.GetDatabaseKey()}'";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Successfully encrypted database {DatabasePath}", databasePath);
            
            File.Delete(backupPath);
            logger.LogDebug("Deleted backup {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to migrate database {DatabasePath}. Backup preserved at {BackupPath}", 
                databasePath, backupPath);
            throw;
        }
    }

    /// <summary>
    /// Checks if a database is already encrypted by attempting to open it without a password.
    /// </summary>
    /// <param name="databasePath">Path to the database file.</param>
    /// <returns>True if encrypted, false if unencrypted or file doesn't exist.</returns>
    public bool IsEncrypted(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        
        if (!File.Exists(databasePath))
        {
            return false;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master";
            command.ExecuteScalar();
            
            return false;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 26)
        {
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not determine encryption status of {DatabasePath}", databasePath);
            return false;
        }
    }
}
