using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings
{
    [Trait("Category", "Unit")]
    public class MsTeamsMeetingsTrackerTests
    {
        private IClock clock;
        private Mock<IPublisher> publisherMock;
        private Mock<IMeetingDiscoveryStrategy> meetingDiscoveryStrategy;
        private Mock<ILogger<ActivityTracker>> loggerMock;
        private MsTeamsMeetingTracker msTeamsMeetingsTracker;

        public MsTeamsMeetingsTrackerTests()
        {
            this.clock = new Clock();
            this.loggerMock = new Mock<ILogger<ActivityTracker>>();
            this.publisherMock = new Mock<IPublisher>();
            this.meetingDiscoveryStrategy = new Mock<IMeetingDiscoveryStrategy>();
        }

        [Fact]
        public void WhenMeetingStartIsRecognized_ThenMeetingStartedEventIsPublished()
        {
            // Given


            // When
            this.msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void WhenMeetingEndIsRecognized_ThenMeetingEndedEventIsPublished()
        {
            // Given

            // When
            this.msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Once);
        }
    }
}
