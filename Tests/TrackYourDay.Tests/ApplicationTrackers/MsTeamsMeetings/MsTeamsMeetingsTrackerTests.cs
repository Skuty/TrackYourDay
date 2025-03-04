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
        private Mock<ILogger<MsTeamsMeetingTracker>> loggerMock;
        private MsTeamsMeetingTracker msTeamsMeetingsTracker;

        public MsTeamsMeetingsTrackerTests()
        {
            this.clock = new Clock();
            this.loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
            this.publisherMock = new Mock<IPublisher>();
            this.meetingDiscoveryStrategy = new Mock<IMeetingDiscoveryStrategy>();
            this.msTeamsMeetingsTracker = new MsTeamsMeetingTracker(this.clock, this.publisherMock.Object, this.meetingDiscoveryStrategy.Object, this.loggerMock.Object);
        }

        [Fact]
        public void GivenMeetingIsNotStarted_WhenMeetingIsStarted_ThenMeetingStartedEventIsPublished()
        {
            new ProcessBasedMeetingRecognizingStrategy(null).RecognizeMeeting();
            // Given
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns(new StartedMeeting(Guid.NewGuid(), this.clock.Now, "Test meeting"));

            // When
            this.msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenMeetingIsNotStarted_WhenMeetingIsStarted_ThenMeetingStartedEventIsNotPublished()
        {
            // Given
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns(new StartedMeeting(Guid.NewGuid(), this.clock.Now, "Test meeting"));
            this.msTeamsMeetingsTracker.RecognizeActivity();
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns(new StartedMeeting(Guid.NewGuid(), this.clock.Now, "Test meeting"));
            this.publisherMock.Invocations.Clear();

            // When
            this.msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public void GivenMeetingIsStarted_WhenMeetingEnds_ThenMeetingEndedEventIsPublished()
        {
            // Given
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns(new StartedMeeting(Guid.NewGuid(), this.clock.Now, "Test meeting"));
            this.msTeamsMeetingsTracker.RecognizeActivity();
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting)null);

            // When
            this.msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None), Times.Once);
        }
    }
}
