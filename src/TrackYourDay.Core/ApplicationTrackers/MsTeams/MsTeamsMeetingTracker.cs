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
        private IMeetingRepository? meetingRepository;
        private StartedMeeting ongoingMeeting;
        private List<EndedMeeting> endedMeetings;

        public MsTeamsMeetingTracker(IClock clock, IPublisher publisher, IMeetingDiscoveryStrategy meetingDiscoveryStrategy, ILogger<MsTeamsMeetingTracker> logger, IMeetingRepository? meetingRepository = null)
        {
            this.clock = clock;
            this.publisher = publisher;
            this.meetingDiscoveryStrategy = meetingDiscoveryStrategy;
            this.logger = logger;
            this.meetingRepository = meetingRepository;
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
                this.logger.LogInformation("Meeting started: {0}", newMeeting);
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
                
                // Persist to database
                meetingRepository?.Save(endedMeeting);
                
                this.publisher.Publish(new MeetingEndedEvent(Guid.NewGuid(), endedMeeting), CancellationToken.None);
                this.logger.LogInformation("Meeting ended: {0}", endedMeeting);

                return;
            }

            return;
        }

        public IReadOnlyCollection<EndedMeeting> GetEndedMeetings()
        {
            return this.endedMeetings.AsReadOnly();
        }

        public IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date)
        {
            if (meetingRepository == null)
            {
                // Fallback to in-memory meetings for today
                if (date == DateOnly.FromDateTime(clock.Now.Date))
                {
                    return GetEndedMeetings();
                }
                return Array.Empty<EndedMeeting>();
            }

            // If requesting today's data, combine in-memory and persisted data
            if (date == DateOnly.FromDateTime(clock.Now.Date))
            {
                var persistedMeetings = meetingRepository.GetMeetingsForDate(date);
                var inMemoryMeetings = endedMeetings.ToList();
                var allMeetings = persistedMeetings.Concat(inMemoryMeetings)
                    .GroupBy(m => m.Guid)
                    .Select(g => g.First())
                    .ToList();
                return allMeetings.AsReadOnly();
            }

            return meetingRepository.GetMeetingsForDate(date);
        }
    }
}
