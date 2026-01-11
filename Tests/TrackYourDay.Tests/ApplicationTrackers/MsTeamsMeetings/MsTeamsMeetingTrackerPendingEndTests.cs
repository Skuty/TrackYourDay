using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public sealed class MsTeamsMeetingTrackerPendingEndTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _meetingDiscoveryStrategyMock;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly MsTeamsMeetingTracker _tracker;

    public MsTeamsMeetingTrackerPendingEndTests()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(x => x.Now).Returns(new DateTime(2026, 1, 7, 10, 0, 0));
        
        _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
        _publisherMock = new Mock<IPublisher>();
        _meetingDiscoveryStrategyMock = new Mock<IMeetingDiscoveryStrategy>();
        
        _tracker = new MsTeamsMeetingTracker(
            _clockMock.Object,
            _publisherMock.Object,
            _meetingDiscoveryStrategyMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GivenOngoingMeeting_WhenNoLongerRecognized_ThenPublishesPendingEndEvent()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));

        // When
        _tracker.RecognizeActivity();

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndConfirmationRequestedEvent>(e => e.PendingMeeting.Meeting.Guid == meeting.Guid),
                CancellationToken.None),
            Times.Once);
        
        _tracker.GetOngoingMeeting().Should().BeNull();
        _tracker.GetPendingEndMeeting().Should().NotBeNull();
        _tracker.GetPendingEndMeeting()!.Meeting.Guid.Should().Be(meeting.Guid);
    }

    [Fact]
    public void GivenPendingEnd_WhenMeetingRecognizedAgain_ThenCancelsPendingEnd()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        var sameMeeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(It.IsAny<StartedMeeting>(), It.IsAny<Guid?>()))
            .Returns((sameMeeting, matchedRuleId));

        // When
        _tracker.RecognizeActivity();

        // Then
        _tracker.GetPendingEndMeeting().Should().BeNull();
        _tracker.GetOngoingMeeting().Should().NotBeNull();
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public void GivenPendingEnd_WhenNotExpired_ThenWaitsForConfirmation()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        _clockMock.Setup(x => x.Now).Returns(_clockMock.Object.Now.AddMinutes(1));

        // When
        _tracker.RecognizeActivity();

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Never);
        
        _tracker.GetPendingEndMeeting().Should().NotBeNull();
    }

    [Fact]
    public void GivenPendingEnd_WhenDifferentMeetingRecognized_ThenBlocksNewMeeting()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        var differentMeeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Different Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(It.IsAny<StartedMeeting>(), It.IsAny<Guid?>()))
            .Returns((differentMeeting, Guid.NewGuid()));

        // When
        _tracker.RecognizeActivity();

        // Then
        _tracker.GetPendingEndMeeting().Should().NotBeNull();
        _tracker.GetPendingEndMeeting()!.Meeting.Title.Should().Be("Test Meeting");
        _tracker.GetOngoingMeeting().Should().BeNull();
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Different Meeting") && v.ToString()!.Contains("Test Meeting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenPendingEnd_WhenConfirmedWithCustomDescription_ThenPublishesMeetingEndedEventWithDescription()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        var customDescription = "Discussed project timeline";

        // When
        await _tracker.ConfirmMeetingEndAsync(meeting.Guid, customDescription);

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndedEvent>(e => 
                    e.EndedMeeting.GetDescription() == customDescription &&
                    e.EndedMeeting.HasCustomDescription),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _tracker.GetPendingEndMeeting().Should().BeNull();
    }

    [Fact]
    public void GivenPendingEnd_WhenCancelledViaCancelPendingEnd_ThenReturnsToActiveState()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        _tracker.RecognizeActivity();

        // When
        _tracker.CancelPendingEnd(meeting.Guid);

        // Then
        _tracker.GetPendingEndMeeting().Should().BeNull();
        _tracker.GetOngoingMeeting().Should().NotBeNull();
        _tracker.GetOngoingMeeting()!.Guid.Should().Be(meeting.Guid);
    }
}
