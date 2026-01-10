using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class MsTeamsMeetingTracker : IMsTeamsMeetingService
    {
        private readonly IClock _clock;
        private readonly IPublisher _publisher;
        private readonly IMeetingDiscoveryStrategy _meetingDiscoveryStrategy;
        private readonly IMeetingStateCache _stateCache;
        private readonly ILogger<MsTeamsMeetingTracker> _logger;
        private readonly List<EndedMeeting> _endedMeetings;

        public MsTeamsMeetingTracker(
            IClock clock, 
            IPublisher publisher, 
            IMeetingDiscoveryStrategy meetingDiscoveryStrategy,
            IMeetingStateCache stateCache,
            ILogger<MsTeamsMeetingTracker> logger)
        {
            _clock = clock;
            _publisher = publisher;
            _meetingDiscoveryStrategy = meetingDiscoveryStrategy;
            _stateCache = stateCache;
            _logger = logger;
            _endedMeetings = new List<EndedMeeting>();
        }

        public async Task ConfirmMeetingEndAsync(Guid meetingGuid, CancellationToken cancellationToken = default)
        {
            var pending = _stateCache.GetPendingEndMeeting();

            if (pending == null || pending.Meeting.Guid != meetingGuid)
            {
                _logger.LogWarning("No pending meeting found for Guid: {MeetingGuid}", meetingGuid);
                return;
            }

            var endedMeeting = pending.Meeting.End(_clock.Now);
            _stateCache.ClearMeetingState();
            _endedMeetings.Add(endedMeeting);

            await _publisher.Publish(new MeetingEndedEvent(Guid.NewGuid(), endedMeeting), cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Meeting confirmed ended: {MeetingTitle}", endedMeeting.Title);
        }

        public StartedMeeting? GetOngoingMeeting()
        {
            return _stateCache.GetOngoingMeeting();
        }

        public PendingEndMeeting? GetPendingEndMeeting()
        {
            return _stateCache.GetPendingEndMeeting();
        }

        public void RecognizeActivity()
        {
            var recognizedMeeting = _meetingDiscoveryStrategy.RecognizeMeeting();
            var ongoingMeeting = _stateCache.GetOngoingMeeting();
            var pendingEnd = _stateCache.GetPendingEndMeeting();

            // Handle pending end confirmation
            if (pendingEnd != null)
            {
                // Meeting recognized again - cancel pending end
                if (recognizedMeeting is StartedMeeting && recognizedMeeting.Title == pendingEnd.Meeting.Title)
                {
                    _stateCache.SetPendingEndMeeting(null);
                    _logger.LogInformation("Meeting end cancelled - meeting still active: {MeetingTitle}", pendingEnd.Meeting.Title);
                    return;
                }

                // Pending end expired - auto-confirm
                if (pendingEnd.IsExpired(_clock))
                {
                    var endedMeeting = pendingEnd.Meeting.End(_clock.Now);
                    _stateCache.ClearMeetingState();
                    _endedMeetings.Add(endedMeeting);
                    _publisher.Publish(new MeetingEndedEvent(Guid.NewGuid(), endedMeeting), CancellationToken.None);
                    _logger.LogInformation("Meeting auto-confirmed ended after timeout: {MeetingTitle}", endedMeeting.Title);
                    return;
                }

                // Still waiting for confirmation
                return;
            }

            // No meeting ongoing, new meeting detected
            if (ongoingMeeting is null && recognizedMeeting is StartedMeeting newMeeting)
            {
                _stateCache.SetOngoingMeeting(newMeeting);
                _publisher.Publish(new MeetingStartedEvent(Guid.NewGuid(), newMeeting), CancellationToken.None);
                _logger.LogInformation("Meeting started: {MeetingTitle}", newMeeting.Title);
                return;
            }

            // Ongoing meeting still recognized
            if (ongoingMeeting is not null 
                && recognizedMeeting is StartedMeeting 
                && recognizedMeeting.Title == ongoingMeeting.Title)
            {
                return;
            }

            // Ongoing meeting no longer detected - request confirmation
            if (ongoingMeeting is not null && recognizedMeeting is null)
            {
                var pending = new PendingEndMeeting
                {
                    Meeting = ongoingMeeting,
                    DetectedAt = _clock.Now
                };
                _stateCache.SetPendingEndMeeting(pending);
                _stateCache.SetOngoingMeeting(null);
                _publisher.Publish(new MeetingEndConfirmationRequestedEvent(Guid.NewGuid(), pending), CancellationToken.None);
                _logger.LogInformation("Meeting end detected, awaiting confirmation: {MeetingTitle}", ongoingMeeting.Title);
                return;
            }
        }

        public IReadOnlyCollection<EndedMeeting> GetEndedMeetings()
        {
            return _endedMeetings.AsReadOnly();
        }
    }
}
