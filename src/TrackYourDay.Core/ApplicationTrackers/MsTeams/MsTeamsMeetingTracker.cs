using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Singleton tracker for MS Teams meeting lifecycle.
/// Blocking state machine: PENDING state blocks all new meeting recognition.
/// Auto-confirmation handled in UI layer (not Core).
/// NOT thread-safe - caller must ensure single-threaded access.
/// </summary>
public sealed class MsTeamsMeetingTracker
{
    private readonly IClock _clock;
    private readonly IPublisher _publisher;
    private readonly IMeetingDiscoveryStrategy _meetingDiscoveryStrategy;
    private readonly ILogger<MsTeamsMeetingTracker> _logger;
    
    private StartedMeeting? _ongoingMeeting;
    private StartedMeeting? _pendingEndMeeting;
    private DateTime? _pendingEndDetectedAt;
    private Guid? _matchedRuleId;
    private readonly List<EndedMeeting> _endedMeetings = [];

    public MsTeamsMeetingTracker(
        IClock clock, 
        IPublisher publisher, 
        IMeetingDiscoveryStrategy meetingDiscoveryStrategy,
        ILogger<MsTeamsMeetingTracker> logger)
    {
        _clock = clock;
        _publisher = publisher;
        _meetingDiscoveryStrategy = meetingDiscoveryStrategy;
        _logger = logger;
    }

    public void RecognizeActivity()
    {
        var pendingEnd = _pendingEndMeeting;

        // PENDING STATE: Blocking behavior - do not recognize any new meetings
        if (pendingEnd != null)
        {
            var (recognizedMeeting, _) = _meetingDiscoveryStrategy.RecognizeMeeting(_ongoingMeeting, _matchedRuleId);
            
            // Case A: Same meeting re-detected → Cancel pending end
            if (recognizedMeeting != null && recognizedMeeting.Title == pendingEnd.Title)
            {
                _pendingEndMeeting = null;
                _pendingEndDetectedAt = null;
                _ongoingMeeting = pendingEnd;
                _logger.LogInformation("Meeting end cancelled: {Title}", pendingEnd.Title);
                return;
            }
            
            // Case B: Different meeting detected → BLOCK (log warning)
            if (recognizedMeeting != null && recognizedMeeting.Title != pendingEnd.Title)
            {
                _logger.LogWarning(
                    "New meeting '{NewTitle}' detected while awaiting confirmation for '{OldTitle}'. " +
                    "New meeting ignored until user responds.",
                    recognizedMeeting.Title,
                    pendingEnd.Title
                );
                return;
            }
            
            // Case C: No meeting detected → Continue waiting
            return;
        }

        // Not in PENDING state - proceed with recognition
        var (recognized, matchedRuleId) = _meetingDiscoveryStrategy.RecognizeMeeting(_ongoingMeeting, _matchedRuleId);
        var ongoing = _ongoingMeeting;

        // IDLE STATE: No ongoing meeting
        if (ongoing == null && recognized != null)
        {
            _ongoingMeeting = recognized;
            _matchedRuleId = matchedRuleId;
            _publisher.Publish(new MeetingStartedEvent(Guid.NewGuid(), recognized), CancellationToken.None);
            _logger.LogInformation("Meeting started: {Title}", recognized.Title);
            return;
        }

        // ACTIVE STATE: Ongoing meeting continues
        if (ongoing != null && recognized != null && recognized.Title == ongoing.Title)
        {
            return;
        }

        // ACTIVE → PENDING: Meeting window closed
        if (ongoing != null && recognized == null)
        {
            _pendingEndMeeting = ongoing;
            _pendingEndDetectedAt = _clock.Now;
            _ongoingMeeting = null;
            
            _publisher.Publish(
                new MeetingEndConfirmationRequestedEvent(
                    ongoing.Guid,
                    ongoing.Title),
                CancellationToken.None
            );
            
            _logger.LogInformation("Meeting end detected, awaiting confirmation: {Title}", ongoing.Title);
        }
    }

    public async Task ConfirmMeetingEndAsync(
        Guid meetingGuid,
        string? customDescription = null,
        CancellationToken cancellationToken = default)
    {
        var pending = _pendingEndMeeting;

        if (pending == null || pending.Guid != meetingGuid)
        {
            _logger.LogWarning("No pending meeting for Guid: {Guid}", meetingGuid);
            return;
        }

        var endedMeeting = pending.End(_clock.Now);

        if (!string.IsNullOrWhiteSpace(customDescription))
        {
            if (customDescription.Length > 500)
                throw new ArgumentException("Description cannot exceed 500 characters", nameof(customDescription));
            
            endedMeeting.SetCustomDescription(customDescription);
        }

        _pendingEndMeeting = null;
        _pendingEndDetectedAt = null;
        _ongoingMeeting = null;
        _matchedRuleId = null;
        _endedMeetings.Add(endedMeeting);

        await _publisher.Publish(new MeetingEndedEvent(Guid.NewGuid(), endedMeeting), cancellationToken)
            .ConfigureAwait(false);
        
        _logger.LogInformation("Meeting confirmed: {Description}", endedMeeting.GetDescription());
    }

    public void CancelPendingEnd(Guid meetingGuid)
    {
        var pending = _pendingEndMeeting;

        if (pending?.Guid == meetingGuid)
        {
            _ongoingMeeting = pending;
            _pendingEndMeeting = null;
            _pendingEndDetectedAt = null;
            _logger.LogInformation("Pending end cancelled: {Title}", pending.Title);
        }
    }

    public StartedMeeting? GetOngoingMeeting() => _ongoingMeeting;

    public IReadOnlyCollection<EndedMeeting> GetEndedMeetings() => _endedMeetings.AsReadOnly();
}
