using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Web.Services;

/// <summary>
/// Cache for recently ended meetings to support UI popups.
/// Keeps meetings in memory for a short period to avoid database lookups.
/// </summary>
public interface IRecentMeetingsCache
{
    /// <summary>
    /// Adds a recently ended meeting to the cache.
    /// </summary>
    void Add(EndedMeeting meeting);
    
    /// <summary>
    /// Retrieves a meeting by its GUID. Returns null if not found or expired.
    /// </summary>
    EndedMeeting? Get(Guid meetingGuid);
    
    /// <summary>
    /// Removes a meeting from the cache after it's been processed.
    /// </summary>
    void Remove(Guid meetingGuid);
}
