using System.Collections.Concurrent;

namespace TrackYourDay.Web.Services;

    /// <summary>
    /// Stores meeting confirmation data for active popup windows.
    /// Scoped to pending confirmations only—cleared on user action.
    /// </summary>
    public sealed class ActiveMeetingConfirmationsService
    {
        private readonly ConcurrentDictionary<Guid, MeetingConfirmationData> _active = new();

        /// <summary>
        /// Stores meeting data when confirmation popup is opened.
        /// </summary>
        public void Store(Guid meetingGuid, string meetingTitle)
        {
            var data = new MeetingConfirmationData(meetingGuid, meetingTitle);
            _active.TryAdd(meetingGuid, data);
        }

    /// <summary>
    /// Retrieves meeting data for active confirmation popup.
    /// Returns null if confirmation expired or was already handled.
    /// </summary>
    public MeetingConfirmationData? Get(Guid meetingGuid)
    {
        return _active.TryGetValue(meetingGuid, out var data) ? data : null;
    }

    /// <summary>
    /// Removes meeting data after user responds or timeout.
    /// </summary>
    public void Remove(Guid meetingGuid)
    {
        _active.TryRemove(meetingGuid, out _);
    }
}

/// <summary>
/// Immutable data for meeting confirmation UI.
/// Contains only data needed for display—no domain logic.
/// </summary>
public sealed record MeetingConfirmationData(
    Guid MeetingGuid,
    string MeetingTitle);
