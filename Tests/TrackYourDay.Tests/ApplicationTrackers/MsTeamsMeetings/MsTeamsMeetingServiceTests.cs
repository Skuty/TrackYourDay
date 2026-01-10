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
public sealed class MsTeamsMeetingServiceTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly IMeetingStateCache _stateCache;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _discoveryStrategyMock;
    private readonly IMsTeamsMeetingService _service;

    public MsTeamsMeetingServiceTests()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(x => x.Now).Returns(new DateTime(2026, 1, 7, 10, 30, 0));
        
        _publisherMock = new Mock<IPublisher>();
        _stateCache = new MeetingStateCache();
        _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
        _discoveryStrategyMock = new Mock<IMeetingDiscoveryStrategy>();
        
        _service = new MsTeamsMeetingTracker(
            _clockMock.Object,
            _publisherMock.Object,
            _discoveryStrategyMock.Object,
            _stateCache,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GivenPendingMeeting_WhenConfirmed_ThenPublishesMeetingEndedEvent()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, new DateTime(2026, 1, 7, 10, 0, 0), "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = new DateTime(2026, 1, 7, 10, 25, 0)
        };
        _stateCache.SetPendingEndMeeting(pending);

        // When
        await _service.ConfirmMeetingEndAsync(meetingGuid);

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndedEvent>(e => e.EndedMeeting.Guid == meetingGuid),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _stateCache.GetPendingEndMeeting().Should().BeNull();
        _stateCache.GetOngoingMeeting().Should().BeNull();
    }

    [Fact]
    public async Task GivenNoPendingMeeting_WhenConfirmed_ThenLogsWarningAndDoesNothing()
    {
        // Given
        var meetingGuid = Guid.NewGuid();

        // When
        await _service.ConfirmMeetingEndAsync(meetingGuid);

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenDifferentPendingMeeting_WhenConfirmed_ThenLogsWarningAndDoesNothing()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = _clockMock.Object.Now
        };
        _stateCache.SetPendingEndMeeting(pending);

        var differentGuid = Guid.NewGuid();

        // When
        await _service.ConfirmMeetingEndAsync(differentGuid);

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        
        _stateCache.GetPendingEndMeeting().Should().NotBeNull();
    }

    [Fact]
    public void GivenOngoingMeeting_WhenGetOngoingMeeting_ThenReturnsIt()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        _stateCache.SetOngoingMeeting(meeting);

        // When
        var result = _service.GetOngoingMeeting();

        // Then
        result.Should().Be(meeting);
    }

    [Fact]
    public void GivenPendingEndMeeting_WhenGetPendingEndMeeting_ThenReturnsIt()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clockMock.Object.Now, "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = _clockMock.Object.Now
        };
        _stateCache.SetPendingEndMeeting(pending);

        // When
        var result = _service.GetPendingEndMeeting();

        // Then
        result.Should().Be(pending);
    }
}
