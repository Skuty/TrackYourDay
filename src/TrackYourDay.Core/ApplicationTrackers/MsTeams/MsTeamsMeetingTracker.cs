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

    public StartedMeeting? GetOngoingMeeting() => _ongoingMeeting;

    public IReadOnlyCollection<EndedMeeting> GetEndedMeetings() => _endedMeetings.AsReadOnly();
}
