namespace TrackYourDay.Core.Persistence;

/// <summary>
/// Provides encryption keys for database security.
/// </summary>
public interface IDatabaseKeyProvider
{
    /// <summary>
    /// Gets the encryption key for database protection.
    /// </summary>
    /// <returns>Base64-encoded encryption key derived from system-specific entropy.</returns>
    string GetDatabaseKey();
}
