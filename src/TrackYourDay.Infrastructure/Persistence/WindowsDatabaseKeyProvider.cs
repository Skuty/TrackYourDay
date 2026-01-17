using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Infrastructure.Persistence;

/// <summary>
/// Provides database encryption keys derived from Windows user identity.
/// Uses SHA256 hash of current user's SID for deterministic per-user encryption.
/// </summary>
public sealed class WindowsDatabaseKeyProvider : IDatabaseKeyProvider
{
    /// <inheritdoc />
    public string GetDatabaseKey()
    {
        var userSid = WindowsIdentity.GetCurrent().User?.Value 
            ?? throw new InvalidOperationException("Cannot retrieve Windows user SID for database encryption");
        
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(userSid));
        return Convert.ToBase64String(keyBytes);
    }
}
