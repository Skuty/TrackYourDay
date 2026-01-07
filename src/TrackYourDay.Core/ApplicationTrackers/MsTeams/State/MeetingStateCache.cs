namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

/// <summary>
/// Thread-safe in-memory cache for ongoing meeting state.
/// Persists across Scoped tracker instances.
/// </summary>
public sealed class MeetingStateCache : IMeetingStateCache
{
    private readonly object _lock = new();
    private StartedMeeting? _ongoingMeeting;
    private Guid? _matchedRuleId;
    private PendingEndMeeting? _pendingEndMeeting;

    public StartedMeeting? GetOngoingMeeting()
    {
        lock (_lock)
        {
            return _ongoingMeeting;
        }
    }

    public void SetOngoingMeeting(StartedMeeting? meeting)
    {
        lock (_lock)
        {
            _ongoingMeeting = meeting;
        }
    }

    public Guid? GetMatchedRuleId()
    {
        lock (_lock)
        {
            return _matchedRuleId;
        }
    }

    public void SetMatchedRuleId(Guid? ruleId)
    {
        lock (_lock)
        {
            _matchedRuleId = ruleId;
        }
    }

    public void ClearMeetingState()
    {
        lock (_lock)
        {
            _ongoingMeeting = null;
            _matchedRuleId = null;
            _pendingEndMeeting = null;
        }
    }

    public PendingEndMeeting? GetPendingEndMeeting()
    {
        lock (_lock)
        {
            return _pendingEndMeeting;
        }
    }

    public void SetPendingEndMeeting(PendingEndMeeting? pending)
    {
        lock (_lock)
        {
            _pendingEndMeeting = pending;
        }
    }
}
