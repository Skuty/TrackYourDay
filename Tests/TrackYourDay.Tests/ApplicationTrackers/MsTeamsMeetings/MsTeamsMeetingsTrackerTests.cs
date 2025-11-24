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
        private Mock<IMeetingRepository> meetingRepositoryMock;
        private MsTeamsMeetingTracker msTeamsMeetingsTracker;

        public MsTeamsMeetingsTrackerTests()
        {
            this.clock = new Clock();
            this.loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
            this.publisherMock = new Mock<IPublisher>();
            this.meetingDiscoveryStrategy = new Mock<IMeetingDiscoveryStrategy>();
            this.meetingRepositoryMock = new Mock<IMeetingRepository>();
            this.msTeamsMeetingsTracker = new MsTeamsMeetingTracker(this.clock, this.publisherMock.Object, this.meetingDiscoveryStrategy.Object, this.loggerMock.Object, this.meetingRepositoryMock.Object);
        }

        [Fact]
        public void GivenMeetingIsNotStarted_WhenMeetingIsStarted_ThenMeetingStartedEventIsPublished()
        {
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

        [Fact]
        public void GivenEndedMeeting_WhenSettingDescription_ThenDescriptionIsSet()
        {
            // Given
            var meetingGuid = Guid.NewGuid();
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns(new StartedMeeting(meetingGuid, this.clock.Now, "Test meeting"));
            this.msTeamsMeetingsTracker.RecognizeActivity();
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting)null);
            this.msTeamsMeetingsTracker.RecognizeActivity();

            // When
            var endedMeeting = this.msTeamsMeetingsTracker.GetEndedMeetings().First(m => m.Guid == meetingGuid);
            endedMeeting.SetDescription("Discussed project requirements");

            // Then
            Assert.Equal("Discussed project requirements", endedMeeting.Description);
            Assert.Equal("Discussed project requirements", endedMeeting.GetDescription());
        }

        [Fact]
        public void GivenEndedMeetingWithoutDescription_WhenGettingDescription_ThenReturnsMeetingTitle()
        {
            // Given
            var meetingGuid = Guid.NewGuid();
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns(new StartedMeeting(meetingGuid, this.clock.Now, "Test meeting"));
            this.msTeamsMeetingsTracker.RecognizeActivity();
            this.meetingDiscoveryStrategy.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting)null);
            this.msTeamsMeetingsTracker.RecognizeActivity();

            // When
            var endedMeeting = this.msTeamsMeetingsTracker.GetEndedMeetings().First(m => m.Guid == meetingGuid);

            // Then
            Assert.Equal(string.Empty, endedMeeting.Description);
            Assert.Equal("Test meeting", endedMeeting.GetDescription());
        }
    }
}
