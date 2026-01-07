using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Commands;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public sealed class ConfirmMeetingEndCommandHandlerTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly IMeetingStateCache _stateCache;
    private readonly Mock<ILogger<ConfirmMeetingEndCommandHandler>> _loggerMock;
    private readonly ConfirmMeetingEndCommandHandler _handler;

    public ConfirmMeetingEndCommandHandlerTests()
    {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(x => x.Now).Returns(new DateTime(2026, 1, 7, 10, 30, 0));
        
        _publisherMock = new Mock<IPublisher>();
        _stateCache = new MeetingStateCache();
        _loggerMock = new Mock<ILogger<ConfirmMeetingEndCommandHandler>>();
        
        _handler = new ConfirmMeetingEndCommandHandler(
            _stateCache,
            _publisherMock.Object,
            _clockMock.Object,
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

        var command = new ConfirmMeetingEndCommand(meetingGuid);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndedEvent>(e => e.EndedMeeting.Guid == meetingGuid),
                CancellationToken.None),
            Times.Once);
        
        _stateCache.GetPendingEndMeeting().Should().BeNull();
        _stateCache.GetOngoingMeeting().Should().BeNull();
    }

    [Fact]
    public async Task GivenNoPendingMeeting_WhenConfirmed_ThenLogsWarningAndDoesNothing()
    {
        // Given
        var command = new ConfirmMeetingEndCommand(Guid.NewGuid());

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
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
        var command = new ConfirmMeetingEndCommand(differentGuid);

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<MeetingEndedEvent>(), CancellationToken.None),
            Times.Never);
        
        _stateCache.GetPendingEndMeeting().Should().NotBeNull();
    }
}
