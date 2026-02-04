using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

/// <summary>
/// Tests for meeting end confirmation time validation in MsTeamsMeetingTracker.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MsTeamsMeetingTrackerValidationTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _meetingDiscoveryStrategyMock;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly MsTeamsMeetingTracker _tracker;

    public MsTeamsMeetingTrackerValidationTests()
    {
        _clockMock = new Mock<IClock>();
        _publisherMock = new Mock<IPublisher>();
        _meetingDiscoveryStrategyMock = new Mock<IMeetingDiscoveryStrategy>();
        _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
        
        _tracker = new MsTeamsMeetingTracker(
            _clockMock.Object,
            _publisherMock.Object,
            _meetingDiscoveryStrategyMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeInFuture_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var currentTime = new DateTime(2026, 1, 23, 11, 0, 0);
        var futureEndTime = new DateTime(2026, 1, 23, 12, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, futureEndTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time*cannot be in the future*");
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeBeforeStart_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var invalidEndTime = new DateTime(2026, 1, 23, 9, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, invalidEndTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time*cannot be before meeting start time*");
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeAtCurrentTime_ThenSucceeds()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var currentTime = new DateTime(2026, 1, 23, 11, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, currentTime);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeAtStartTime_ThenSucceeds()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, startTime);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeBetweenStartAndNow_ThenSucceeds()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var currentTime = new DateTime(2026, 1, 23, 12, 0, 0);
        var validEndTime = new DateTime(2026, 1, 23, 11, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, validEndTime);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenNoPendingMeeting_WhenConfirmingEnd_ThenThrowsInvalidOperationException()
    {
        // Given
        var randomGuid = Guid.NewGuid();
        var currentTime = new DateTime(2026, 1, 23, 11, 0, 0);
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(randomGuid, null, currentTime);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"No pending meeting found with ID {randomGuid}");
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeOneSecondBeforeNow_ThenSucceeds()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var currentTime = new DateTime(2026, 1, 23, 11, 0, 0);
        var validEndTime = currentTime.AddSeconds(-1);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, validEndTime);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeOneSecondAfterNow_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var currentTime = new DateTime(2026, 1, 23, 11, 0, 0);
        var futureEndTime = currentTime.AddSeconds(1);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, futureEndTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be in the future*");
    }

    [Fact]
    public async Task GivenPendingEnd_WhenEndTimeOneSecondBeforeStart_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var invalidEndTime = startTime.AddSeconds(-1);
        
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meeting = await StartMeetingAndTransitionToPending(startTime);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, invalidEndTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be before meeting start time*");
    }

    private async Task<StartedMeeting> StartMeetingAndTransitionToPending(DateTime startTime)
    {
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
        
        return meeting;
    }
}
