using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class MsTeamsMeetingTracker
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

        public void RecognizeActivity()
        {
            var recognizedMeeting = _meetingDiscoveryStrategy.RecognizeMeeting();
            var ongoingMeeting = _stateCache.GetOngoingMeeting();

            if (ongoingMeeting is null && recognizedMeeting is StartedMeeting newMeeting)
            {
                _stateCache.SetOngoingMeeting(newMeeting);
                _publisher.Publish(new MeetingStartedEvent(Guid.NewGuid(), newMeeting), CancellationToken.None);
                _logger.LogInformation("Meeting started: {MeetingTitle}", newMeeting.Title);
                return;
            }

            if (ongoingMeeting is not null 
                && recognizedMeeting is StartedMeeting 
                && recognizedMeeting.Title == ongoingMeeting.Title)
            {
                return;
            }

            if (ongoingMeeting is not null && recognizedMeeting is null)
            {
                var endedMeeting = ongoingMeeting.End(_clock.Now);
                _stateCache.ClearMeetingState();
                _endedMeetings.Add(endedMeeting);
                _publisher.Publish(new MeetingEndedEvent(Guid.NewGuid(), endedMeeting), CancellationToken.None);
                _logger.LogInformation("Meeting ended: {MeetingTitle}", endedMeeting.Title);
                return;
            }
        }

        public IReadOnlyCollection<EndedMeeting> GetEndedMeetings()
        {
            return _endedMeetings.AsReadOnly();
        }
    }
}
