using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings
{
    [Trait("Category", "Unit")]
    public class MsTeamsMeetingsTrackerTests
    {
        private readonly IClock _clock;
        private readonly Mock<IPublisher> _publisherMock;
        private readonly Mock<IMeetingDiscoveryStrategy> _meetingDiscoveryStrategyMock;
        private readonly Mock<IMeetingStateCache> _stateCacheMock;
        private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
        private readonly MsTeamsMeetingTracker _msTeamsMeetingsTracker;

        public MsTeamsMeetingsTrackerTests()
        {
            _clock = new Clock();
            _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
            _publisherMock = new Mock<IPublisher>();
            _meetingDiscoveryStrategyMock = new Mock<IMeetingDiscoveryStrategy>();
            _stateCacheMock = new Mock<IMeetingStateCache>();
            _msTeamsMeetingsTracker = new MsTeamsMeetingTracker(
                _clock, 
                _publisherMock.Object, 
                _meetingDiscoveryStrategyMock.Object, 
                _stateCacheMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void GivenMeetingIsNotStarted_WhenMeetingIsStarted_ThenMeetingStartedEventIsPublished()
        {
            // Given
            var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
            _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns((StartedMeeting?)null);
            _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns(meeting);

            // When
            _msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            _publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Once);
            _stateCacheMock.Verify(x => x.SetOngoingMeeting(meeting), Times.Once);
        }

        [Fact]
        public void GivenMeetingIsOngoing_WhenSameMeetingRecognized_ThenMeetingStartedEventIsNotPublished()
        {
            // Given
            var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
            _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(meeting);
            _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns(new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting"));

            // When
            _msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            _publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public void GivenMeetingIsStarted_WhenMeetingEnds_ThenMeetingEndedEventIsPublished()
        {
            // Given
            var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
            _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(meeting);
            _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting?)null);

            // When
            _msTeamsMeetingsTracker.RecognizeActivity();

            // Then
            _publisherMock.Verify(x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None), Times.Once);
            _stateCacheMock.Verify(x => x.ClearMeetingState(), Times.Once);
        }

        [Fact]
        public void GivenEndedMeeting_WhenSettingDescription_ThenDescriptionIsSet()
        {
            // Given
            var meetingGuid = Guid.NewGuid();
            var meeting = new StartedMeeting(meetingGuid, _clock.Now, "Test meeting");
            _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(meeting);
            _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns(meeting);
            _msTeamsMeetingsTracker.RecognizeActivity();
            
            _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting?)null);
            _msTeamsMeetingsTracker.RecognizeActivity();

            // When
            var endedMeeting = _msTeamsMeetingsTracker.GetEndedMeetings().First(m => m.Guid == meetingGuid);
            endedMeeting.SetCustomDescription("Discussed project requirements");

            // Then
            Assert.Equal("Discussed project requirements", endedMeeting.CustomDescription);
            Assert.Equal("Discussed project requirements", endedMeeting.GetDescription());
        }

        [Fact]
        public void GivenEndedMeetingWithoutDescription_WhenGettingDescription_ThenReturnsMeetingTitle()
        {
            // Given
            var meetingGuid = Guid.NewGuid();
            var meeting = new StartedMeeting(meetingGuid, _clock.Now, "Test meeting");
            _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(meeting);
            _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns(meeting);
            _msTeamsMeetingsTracker.RecognizeActivity();
            
            _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting?)null);
            _msTeamsMeetingsTracker.RecognizeActivity();

            // When
            var endedMeeting = _msTeamsMeetingsTracker.GetEndedMeetings().First(m => m.Guid == meetingGuid);

            // Then
            Assert.Null(endedMeeting.CustomDescription);
            Assert.Equal("Test meeting", endedMeeting.GetDescription());
        }
    }
}
