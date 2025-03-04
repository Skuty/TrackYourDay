using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class MsTeamsMeetingTracker
    {
        private IClock clock;
        private IPublisher publisher;
        private IMeetingDiscoveryStrategy meetingDiscoveryStrategy;
        private ILogger<MsTeamsMeetingTracker> logger;
        private StartedMeeting ongoingMeeting;
        private List<EndedMeeting> endedMeetings;

        public MsTeamsMeetingTracker(IClock clock, IPublisher publisher, IMeetingDiscoveryStrategy meetingDiscoveryStrategy, ILogger<MsTeamsMeetingTracker> logger)
        {
            this.clock = clock;
            this.publisher = publisher;
            this.meetingDiscoveryStrategy = meetingDiscoveryStrategy;
            this.logger = logger;
            this.endedMeetings = new List<EndedMeeting>();
        }

        public void RecognizeActivity()
        {
            var recognizedMeeting = this.meetingDiscoveryStrategy.RecognizeMeeting();

            if (this.ongoingMeeting is null
                && recognizedMeeting is StartedMeeting newMeeting)
            {
                this.ongoingMeeting = newMeeting;
                this.publisher.Publish(new MeetingStartedEvent(Guid.NewGuid(), newMeeting), CancellationToken.None);
                
                return;
            }

            if (this.ongoingMeeting is not null
                && recognizedMeeting is StartedMeeting ongoingMeeting
                && recognizedMeeting.Title == ongoingMeeting.Title)
            {
                return;
            }

            if (this.ongoingMeeting is not null
                && recognizedMeeting is null)
            {
                var endedMeeting = this.ongoingMeeting.End(this.clock.Now);
                this.ongoingMeeting = null;

                this.endedMeetings.Add(endedMeeting);
                this.publisher.Publish(new MeetingEndedEvent(Guid.NewGuid(), endedMeeting), CancellationToken.None);

                return;
            }

            return;
        }
    }
}
