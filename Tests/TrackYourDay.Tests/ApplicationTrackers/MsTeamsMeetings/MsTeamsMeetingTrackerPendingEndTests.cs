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
    public async Task GivenOngoingMeeting_WhenNoLongerRecognized_ThenPublishesPendingEndEvent()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));

        // When
        await _tracker.RecognizeActivityAsync();

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndConfirmationRequestedEvent>(e => 
                    e.MeetingGuid == meeting.Guid &&
                    e.StartTime == meeting.StartDate),
                CancellationToken.None),
            Times.Once);
        
        _tracker.GetOngoingMeeting().Should().BeNull();
        _tracker.GetEndedMeetings().Should().BeEmpty();
    }

    [Fact]
    public async Task GivenPendingEnd_WhenMeetingRecognizedAgain_ThenCancelsPendingEnd()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();
        
        var sameMeeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(It.IsAny<StartedMeeting>(), It.IsAny<Guid?>()))
            .Returns((sameMeeting, matchedRuleId));

        // When
        await _tracker.RecognizeActivityAsync();

        // Then
        _tracker.GetOngoingMeeting().Should().NotBeNull();
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task GivenPendingEnd_WhenNotExpired_ThenWaitsForConfirmation()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();
        
        _clockMock.Setup(x => x.Now).Returns(_clockMock.Object.Now.AddMinutes(1));

        // When
        await _tracker.RecognizeActivityAsync();

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task GivenPendingEnd_WhenDifferentMeetingRecognized_ThenBlocksNewMeeting()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();
        
        var differentMeeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Different Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(It.IsAny<StartedMeeting>(), It.IsAny<Guid?>()))
            .Returns((differentMeeting, Guid.NewGuid()));

        // When
        await _tracker.RecognizeActivityAsync();

        // Then
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
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();
        
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
        
        _tracker.GetOngoingMeeting().Should().BeNull();
    }

    [Fact]
    public async Task GivenPendingEnd_WhenConfirmedWithCustomEndTime_ThenUsesProvidedEndTime()
    {
        // Given
        var startTime = new DateTime(2026, 1, 7, 10, 0, 0);
        var customEndTime = new DateTime(2026, 1, 7, 11, 30, 0);
        var currentTime = new DateTime(2026, 1, 7, 12, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = new StartedMeeting(Guid.NewGuid(), startTime, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();

        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, customEndTime);

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndedEvent>(e => 
                    e.EndedMeeting.EndDate == customEndTime),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenPendingEnd_WhenConfirmedWithEndTimeBeforeStartTime_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 7, 10, 0, 0);
        var invalidEndTime = new DateTime(2026, 1, 7, 9, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = new StartedMeeting(Guid.NewGuid(), startTime, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, invalidEndTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time*cannot be before meeting start time*");
    }

    [Fact]
    public async Task GivenPendingEnd_WhenConfirmedWithoutCustomEndTime_ThenUsesClockNow()
    {
        // Given
        var startTime = new DateTime(2026, 1, 7, 10, 0, 0);
        var currentTime = new DateTime(2026, 1, 7, 11, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = new StartedMeeting(Guid.NewGuid(), startTime, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        await _tracker.ConfirmMeetingEndAsync(meeting.Guid);

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndedEvent>(e => 
                    e.EndedMeeting.EndDate == currentTime),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenPendingEnd_WhenCancelledViaCancelPendingEnd_ThenReturnsToActiveState()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _tracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _tracker.RecognizeActivityAsync();

        // When
        _tracker.CancelPendingEnd(meeting.Guid);

        // Then
        _tracker.GetOngoingMeeting().Should().NotBeNull();
        _tracker.GetOngoingMeeting()!.Guid.Should().Be(meeting.Guid);
    }
}
