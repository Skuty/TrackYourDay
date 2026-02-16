using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Singleton tracker for MS Teams meeting lifecycle.
/// Blocking state machine: PENDING state blocks all new meeting recognition.
/// Auto-confirmation handled in UI layer (not Core).
/// Thread-safe via Quartz DisallowConcurrentExecution on calling job.
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
    private DateTime? _postponedUntil;
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

    public async Task RecognizeActivityAsync()
    {
        // Check if postpone is active
        if (_postponedUntil.HasValue)
        {
            if (_clock.Now < _postponedUntil.Value)
            {
                // Still postponed - skip all detection logic
                return;
            }
            
            // Postpone expired - clear and continue
            _postponedUntil = null;
            _logger.LogInformation("Meeting check postpone expired, resuming detection");
        }

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
            await _publisher.Publish(new MeetingStartedEvent(Guid.NewGuid(), recognized), CancellationToken.None)
                .ConfigureAwait(false);
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
            
            await _publisher.Publish(
                new MeetingEndConfirmationRequestedEvent(
                    ongoing.Guid,
                    ongoing.Title,
                    ongoing.StartDate),
                CancellationToken.None
            ).ConfigureAwait(false);
            
            _logger.LogInformation("Meeting end detected, awaiting confirmation: {Title}", ongoing.Title);
        }
    }

    /// <summary>
    /// Confirms the end of a pending meeting with validation.
    /// </summary>
    /// <param name="meetingGuid">Unique identifier of the meeting to confirm.</param>
    /// <param name="customDescription">Optional custom description (max 500 chars).</param>
    /// <param name="customEndTime">Optional custom end time. Must be between meeting start and current time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no pending meeting exists.</exception>
    public async Task ConfirmMeetingEndAsync(
        Guid meetingGuid,
        string? customDescription = null,
        DateTime? customEndTime = null,
        CancellationToken cancellationToken = default)
    {
        var pending = _pendingEndMeeting;

        if (pending == null || pending.Guid != meetingGuid)
        {
            _logger.LogWarning("No pending meeting for Guid: {Guid}", meetingGuid);
            throw new InvalidOperationException($"No pending meeting found with ID {meetingGuid}");
        }

        // Clear any active postpone
        _postponedUntil = null;

        var endTime = customEndTime ?? _clock.Now;
        var now = _clock.Now;
        
        // Validation: End time cannot be before start time
        if (endTime < pending.StartDate)
        {
            throw new ArgumentException(
                $"End time ({endTime:HH:mm}) cannot be before meeting start time ({pending.StartDate:HH:mm})", 
                nameof(customEndTime));
        }

        // Validation: End time cannot be in the future
        if (endTime > now)
        {
            throw new ArgumentException(
                $"End time ({endTime:HH:mm}) cannot be in the future (current time: {now:HH:mm})", 
                nameof(customEndTime));
        }

        var endedMeeting = pending.End(endTime);

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

    /// <summary>
    /// Manually ends a currently ongoing meeting with validation.
    /// Can be used to end meetings that are still active (not in pending state).
    /// </summary>
    /// <param name="meetingGuid">Unique identifier of the meeting to end.</param>
    /// <param name="customDescription">Optional custom description (max 500 chars).</param>
    /// <param name="customEndTime">Optional custom end time. Must be between meeting start and current time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when meeting with given ID is not found.</exception>
    public async Task EndMeetingManuallyAsync(
        Guid meetingGuid,
        string? customDescription = null,
        DateTime? customEndTime = null,
        CancellationToken cancellationToken = default)
    {
        var ongoing = _ongoingMeeting;

        if (ongoing == null || ongoing.Guid != meetingGuid)
        {
            _logger.LogWarning("No ongoing meeting for Guid: {Guid}", meetingGuid);
            throw new InvalidOperationException($"No meeting found with ID {meetingGuid}");
        }

        // Clear any active postpone
        _postponedUntil = null;

        var endTime = customEndTime ?? _clock.Now;
        var now = _clock.Now;
        
        // Validation: End time cannot be before start time
        if (endTime < ongoing.StartDate)
        {
            throw new ArgumentException(
                $"End time ({endTime:HH:mm}) cannot be before meeting start time ({ongoing.StartDate:HH:mm})", 
                nameof(customEndTime));
        }

        // Validation: End time cannot be in the future
        if (endTime > now)
        {
            throw new ArgumentException(
                $"End time ({endTime:HH:mm}) cannot be in the future (current time: {now:HH:mm})", 
                nameof(customEndTime));
        }

        var endedMeeting = ongoing.End(endTime);

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
        
        _logger.LogInformation("Meeting manually ended: {Description}", endedMeeting.GetDescription());
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

    /// <summary>
    /// Cancels a recognized meeting that was incorrectly detected (false positive).
    /// Clears all meeting state without recording it as an ended meeting.
    /// </summary>
    /// <param name="meetingGuid">Meeting GUID to validate and cancel</param>
    /// <exception cref="ArgumentException">Thrown if meeting GUID doesn't match pending or ongoing meeting</exception>
    public void CancelRecognizedMeeting(Guid meetingGuid)
    {
        var targetMeeting = _pendingEndMeeting ?? _ongoingMeeting;
        
        if (targetMeeting == null || targetMeeting.Guid != meetingGuid)
        {
            throw new ArgumentException("Meeting GUID does not match pending or ongoing meeting", nameof(meetingGuid));
        }

        var meetingTitle = targetMeeting.Title;
        
        // Clear all meeting state
        _ongoingMeeting = null;
        _pendingEndMeeting = null;
        _pendingEndDetectedAt = null;
        _postponedUntil = null;
        _matchedRuleId = null;
        
        _logger.LogInformation("Meeting recognition cancelled (false positive): {Title}", meetingTitle);
    }

    /// <summary>
    /// Postpones meeting end detection checks until the specified time.
    /// </summary>
    /// <param name="meetingGuid">Meeting GUID to validate against pending/ongoing meeting</param>
    /// <param name="postponeUntil">DateTime when checks should resume</param>
    /// <exception cref="ArgumentException">Thrown if meeting GUID doesn't match or postpone time is invalid</exception>
    public async Task PostponeCheckAsync(Guid meetingGuid, DateTime postponeUntil)
    {
        if (postponeUntil <= _clock.Now)
        {
            throw new ArgumentException("Postpone time must be in the future", nameof(postponeUntil));
        }

        if (postponeUntil > _clock.Now.AddHours(24))
        {
            throw new ArgumentException("Postpone time cannot exceed 24 hours", nameof(postponeUntil));
        }

        var targetMeeting = _pendingEndMeeting ?? _ongoingMeeting;
        if (targetMeeting == null || targetMeeting.Guid != meetingGuid)
        {
            throw new ArgumentException("Meeting GUID does not match pending or ongoing meeting", nameof(meetingGuid));
        }

        _postponedUntil = postponeUntil;
        
        // Clear pending state and restore to ongoing if was pending
        if (_pendingEndMeeting != null)
        {
            _ongoingMeeting = _pendingEndMeeting;
            _pendingEndMeeting = null;
            _pendingEndDetectedAt = null;
        }

        await _publisher.Publish(new MeetingCheckPostponedEvent(meetingGuid, postponeUntil), CancellationToken.None)
            .ConfigureAwait(false);

        _logger.LogInformation("Meeting check postponed until {PostponeUntil}: {Title}", 
            postponeUntil, targetMeeting.Title);
    }

    public StartedMeeting? GetOngoingMeeting() => _ongoingMeeting;

    public IReadOnlyCollection<EndedMeeting> GetEndedMeetings() => _endedMeetings.AsReadOnly();
}
