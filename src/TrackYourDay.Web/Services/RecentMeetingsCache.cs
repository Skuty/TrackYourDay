using System.Collections.Concurrent;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Web.Services;

/// <summary>
/// Thread-safe in-memory cache for recently ended meetings.
/// Automatically expires entries after a configured time window.
/// </summary>
public sealed class RecentMeetingsCache : IRecentMeetingsCache
{
    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<Guid, PendingCacheEntry> _pendingCache = new();
    private readonly TimeSpan _expirationWindow;

    public RecentMeetingsCache(TimeSpan? expirationWindow = null)
    {
        // Default: keep meetings for 5 minutes (enough time for user to respond to popup)
        _expirationWindow = expirationWindow ?? TimeSpan.FromMinutes(5);
    }

    public void Add(EndedMeeting meeting)
    {
        if (meeting == null) throw new ArgumentNullException(nameof(meeting));
        
        var entry = new CacheEntry(meeting, DateTime.UtcNow.Add(_expirationWindow));
        _cache.TryAdd(meeting.Guid, entry);
        
        // Clean up expired entries opportunistically
        CleanupExpired();
    }

    public EndedMeeting? Get(Guid meetingGuid)
    {
        if (_cache.TryGetValue(meetingGuid, out var entry))
        {
            if (DateTime.UtcNow <= entry.ExpiresAt)
            {
                return entry.Meeting;
            }
            
            // Remove expired entry
            _cache.TryRemove(meetingGuid, out _);
        }
        
        return null;
    }

    public void Remove(Guid meetingGuid)
    {
        _cache.TryRemove(meetingGuid, out _);
    }

    public void AddPending(PendingEndMeeting pending)
    {
        if (pending == null) throw new ArgumentNullException(nameof(pending));

        var entry = new PendingCacheEntry(pending, DateTime.UtcNow.Add(_expirationWindow));
        _pendingCache.TryAdd(pending.Meeting.Guid, entry);

        CleanupExpiredPending();
    }

    public PendingEndMeeting? GetPending(Guid meetingGuid)
    {
        if (_pendingCache.TryGetValue(meetingGuid, out var entry))
        {
            if (DateTime.UtcNow <= entry.ExpiresAt)
            {
                return entry.Pending;
            }

            _pendingCache.TryRemove(meetingGuid, out _);
        }

        return null;
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private void CleanupExpiredPending()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _pendingCache
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _pendingCache.TryRemove(key, out _);
        }
    }

    private sealed record CacheEntry(EndedMeeting Meeting, DateTime ExpiresAt);
    private sealed record PendingCacheEntry(PendingEndMeeting Pending, DateTime ExpiresAt);
}
