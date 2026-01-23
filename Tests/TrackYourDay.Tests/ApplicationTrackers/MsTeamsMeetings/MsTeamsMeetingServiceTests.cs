using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public sealed class MsTeamsMeetingServiceTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _discoveryStrategyMock;
    private readonly MsTeamsMeetingTracker _tracker;

    public MsTeamsMeetingServiceTests()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(x => x.Now).Returns(new DateTime(2026, 1, 7, 10, 30, 0));
        
        _publisherMock = new Mock<IPublisher>();
        _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
        _discoveryStrategyMock = new Mock<IMeetingDiscoveryStrategy>();
        
        _tracker = new MsTeamsMeetingTracker(
            _clockMock.Object,
            _publisherMock.Object,
            _discoveryStrategyMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GivenPendingMeeting_WhenConfirmed_ThenPublishesMeetingEndedEvent()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, new DateTime(2026, 1, 7, 10, 0, 0), "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _discoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _discoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        _tracker.RecognizeActivity();

        // When
        await _tracker.ConfirmMeetingEndAsync(meetingGuid);

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndedEvent>(e => e.EndedMeeting.Guid == meetingGuid),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _tracker.GetOngoingMeeting().Should().BeNull();
    }

    [Fact]
    public async Task GivenNoPendingMeeting_WhenConfirmed_ThenThrowsInvalidOperationException()
    {
        // Given
        var meetingGuid = Guid.NewGuid();

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meetingGuid);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"No pending meeting found with ID {meetingGuid}");
        
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenDifferentPendingMeeting_WhenConfirmed_ThenThrowsInvalidOperationException()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _discoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _discoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        _tracker.RecognizeActivity();

        var differentGuid = Guid.NewGuid();

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(differentGuid);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"No pending meeting found with ID {differentGuid}");
        
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void GivenOngoingMeeting_WhenGetOngoingMeeting_ThenReturnsIt()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _discoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();

        // When
        var result = _tracker.GetOngoingMeeting();

        // Then
        result.Should().NotBeNull();
        result!.Guid.Should().Be(meeting.Guid);
    }

    [Fact]
    public void GivenPendingEndMeeting_WhenPublished_ThenEventContainsMeetingInfo()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _discoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        _tracker.RecognizeActivity();
        
        _discoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        // When
        _tracker.RecognizeActivity();

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndConfirmationRequestedEvent>(e => 
                    e.MeetingGuid == meeting.Guid && 
                    e.MeetingTitle == "Test Meeting"),
                CancellationToken.None),
            Times.Once);
    }
}
