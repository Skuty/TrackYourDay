using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public sealed class MsTeamsMeetingTrackerPendingEndTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _meetingDiscoveryStrategyMock;
    private readonly IMeetingStateCache _stateCache;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly MsTeamsMeetingTracker _tracker;

    public MsTeamsMeetingTrackerPendingEndTests()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(x => x.Now).Returns(new DateTime(2026, 1, 7, 10, 0, 0));
        
        _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
        _publisherMock = new Mock<IPublisher>();
        _meetingDiscoveryStrategyMock = new Mock<IMeetingDiscoveryStrategy>();
        _stateCache = new MeetingStateCache();
        
        _tracker = new MsTeamsMeetingTracker(
            _clockMock.Object,
            _publisherMock.Object,
            _meetingDiscoveryStrategyMock.Object,
            _stateCache,
            _loggerMock.Object);
    }

    [Fact]
    public void GivenOngoingMeeting_WhenNoLongerRecognized_ThenPublishesPendingEndEvent()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        _stateCache.SetOngoingMeeting(meeting);
        _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting?)null);

        // When
        _tracker.RecognizeActivity();

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndConfirmationRequestedEvent>(e => e.PendingMeeting.Meeting.Guid == meeting.Guid),
                CancellationToken.None),
            Times.Once);
        
        _stateCache.GetOngoingMeeting().Should().BeNull();
        _stateCache.GetPendingEndMeeting().Should().NotBeNull();
        _stateCache.GetPendingEndMeeting()!.Meeting.Guid.Should().Be(meeting.Guid);
    }

    [Fact]
    public void GivenPendingEnd_WhenMeetingRecognizedAgain_ThenCancelsPendingEnd()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = _clockMock.Object.Now
        };
        _stateCache.SetPendingEndMeeting(pending);
        _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting())
            .Returns(new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting"));

        // When
        _tracker.RecognizeActivity();

        // Then
        _stateCache.GetPendingEndMeeting().Should().BeNull();
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public void GivenPendingEnd_WhenExpired_ThenAutoConfirmsEnd()
    {
        // Given
        var meetingStart = new DateTime(2026, 1, 7, 9, 0, 0);
        var pendingDetected = new DateTime(2026, 1, 7, 10, 0, 0);
        var afterExpiry = pendingDetected.AddMinutes(3);

        var meeting = new StartedMeeting(Guid.NewGuid(), meetingStart, "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = pendingDetected
        };
        _stateCache.SetPendingEndMeeting(pending);
        
        _clockMock.Setup(x => x.Now).Returns(afterExpiry);
        _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting?)null);

        // When
        _tracker.RecognizeActivity();

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Once);
        
        _stateCache.GetPendingEndMeeting().Should().BeNull();
        _stateCache.GetOngoingMeeting().Should().BeNull();
    }

    [Fact]
    public void GivenPendingEnd_WhenNotExpired_ThenWaitsForConfirmation()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = _clockMock.Object.Now
        };
        _stateCache.SetPendingEndMeeting(pending);
        
        _clockMock.Setup(x => x.Now).Returns(pending.DetectedAt.AddMinutes(1)); // Still within 2-minute window
        _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting()).Returns((StartedMeeting?)null);

        // When
        _tracker.RecognizeActivity();

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Never);
        
        _stateCache.GetPendingEndMeeting().Should().NotBeNull();
    }

    [Fact]
    public void GivenPendingEnd_WhenDifferentMeetingRecognized_ThenStillPending()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = _clockMock.Object.Now
        };
        _stateCache.SetPendingEndMeeting(pending);
        _meetingDiscoveryStrategyMock.Setup(x => x.RecognizeMeeting())
            .Returns(new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Different Meeting"));

        // When
        _tracker.RecognizeActivity();

        // Then
        _stateCache.GetPendingEndMeeting().Should().NotBeNull();
    }
}
