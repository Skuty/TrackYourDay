using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

/// <summary>
/// Tests for custom meeting start time validation in MsTeamsMeetingTracker.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MsTeamsMeetingTrackerStartTimeValidationTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _meetingDiscoveryStrategyMock;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly MsTeamsMeetingTracker _tracker;

    public MsTeamsMeetingTrackerStartTimeValidationTests()
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
    public async Task GivenPendingEnd_WhenCustomStartTimeInFuture_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var now = new DateTime(2026, 1, 23, 11, 0, 0);
        var futureStartTime = new DateTime(2026, 1, 23, 12, 0, 0);

        _clockMock.Setup(x => x.Now).Returns(startTime);
        var meeting = await StartMeetingAndTransitionToPending(startTime);
        _clockMock.Setup(x => x.Now).Returns(now);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(
            meeting.Guid, null, customEndTime: now, customStartTime: futureStartTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Start time*cannot be in the future*");
    }

    [Fact]
    public async Task GivenPendingEnd_WhenCustomStartTimeAfterEndTime_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var now = new DateTime(2026, 1, 23, 12, 0, 0);
        var customStart = new DateTime(2026, 1, 23, 11, 30, 0);
        var customEnd = new DateTime(2026, 1, 23, 11, 0, 0); // before customStart

        _clockMock.Setup(x => x.Now).Returns(startTime);
        var meeting = await StartMeetingAndTransitionToPending(startTime);
        _clockMock.Setup(x => x.Now).Returns(now);

        // When
        var act = async () => await _tracker.ConfirmMeetingEndAsync(
            meeting.Guid, null, customEndTime: customEnd, customStartTime: customStart);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time*cannot be before meeting start time*");
    }

    [Fact]
    public async Task GivenPendingEnd_WhenCustomStartTimeValid_ThenEndedMeetingHasCustomStartTime()
    {
        // Given
        var originalStart = new DateTime(2026, 1, 23, 10, 0, 0);
        var now = new DateTime(2026, 1, 23, 12, 0, 0);
        var customStart = new DateTime(2026, 1, 23, 9, 30, 0);
        var customEnd = new DateTime(2026, 1, 23, 11, 0, 0);

        _clockMock.Setup(x => x.Now).Returns(originalStart);
        var meeting = await StartMeetingAndTransitionToPending(originalStart);
        _clockMock.Setup(x => x.Now).Returns(now);

        // When
        await _tracker.ConfirmMeetingEndAsync(
            meeting.Guid, null, customEndTime: customEnd, customStartTime: customStart);

        // Then
        var endedMeeting = _tracker.GetEndedMeetings().Single();
        endedMeeting.StartDate.Should().Be(customStart);
        endedMeeting.EndDate.Should().Be(customEnd);
    }

    [Fact]
    public async Task GivenPendingEnd_WhenNoCustomStartTime_ThenEndedMeetingUsesOriginalStartTime()
    {
        // Given
        var originalStart = new DateTime(2026, 1, 23, 10, 0, 0);
        var now = new DateTime(2026, 1, 23, 12, 0, 0);

        _clockMock.Setup(x => x.Now).Returns(originalStart);
        var meeting = await StartMeetingAndTransitionToPending(originalStart);
        _clockMock.Setup(x => x.Now).Returns(now);

        // When
        await _tracker.ConfirmMeetingEndAsync(meeting.Guid, null, customEndTime: now);

        // Then
        var endedMeeting = _tracker.GetEndedMeetings().Single();
        endedMeeting.StartDate.Should().Be(originalStart);
    }

    [Fact]
    public async Task GivenEndedMeetingExists_WhenNewMeetingOverlapsWithIt_ThenThrowsArgumentException()
    {
        // Given
        var firstStart = new DateTime(2026, 1, 23, 9, 0, 0);
        var now = new DateTime(2026, 1, 23, 12, 0, 0);

        // End first meeting: 09:00 - 10:00
        _clockMock.Setup(x => x.Now).Returns(firstStart);
        var firstMeeting = await StartMeetingAndTransitionToPending(firstStart);
        _clockMock.Setup(x => x.Now).Returns(now);
        await _tracker.ConfirmMeetingEndAsync(
            firstMeeting.Guid, null,
            customEndTime: new DateTime(2026, 1, 23, 10, 0, 0));

        // Start second meeting: 10:30 - 11:00
        var secondStart = new DateTime(2026, 1, 23, 10, 30, 0);
        _clockMock.Setup(x => x.Now).Returns(secondStart);
        var secondMeeting = await StartMeetingAndTransitionToPending(secondStart);
        _clockMock.Setup(x => x.Now).Returns(now);

        // Attempt to set second meeting start to 09:30, overlapping first (09:00-10:00)
        var overlappingStart = new DateTime(2026, 1, 23, 9, 30, 0);
        var act = async () => await _tracker.ConfirmMeetingEndAsync(
            secondMeeting.Guid, null,
            customEndTime: new DateTime(2026, 1, 23, 11, 0, 0),
            customStartTime: overlappingStart);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*overlaps with already recognized meeting*");
    }

    [Fact]
    public async Task GivenEndedMeetingExists_WhenNewMeetingStartsExactlyAtPreviousEnd_ThenSucceeds()
    {
        // Given
        var firstStart = new DateTime(2026, 1, 23, 9, 0, 0);
        var firstEnd = new DateTime(2026, 1, 23, 10, 0, 0);
        var now = new DateTime(2026, 1, 23, 12, 0, 0);

        _clockMock.Setup(x => x.Now).Returns(firstStart);
        var firstMeeting = await StartMeetingAndTransitionToPending(firstStart);
        _clockMock.Setup(x => x.Now).Returns(now);
        await _tracker.ConfirmMeetingEndAsync(firstMeeting.Guid, null, customEndTime: firstEnd);

        var secondStart = new DateTime(2026, 1, 23, 10, 30, 0);
        _clockMock.Setup(x => x.Now).Returns(secondStart);
        var secondMeeting = await StartMeetingAndTransitionToPending(secondStart);
        _clockMock.Setup(x => x.Now).Returns(now);

        // When — second meeting starts exactly when first ended (no overlap)
        var act = async () => await _tracker.ConfirmMeetingEndAsync(
            secondMeeting.Guid, null,
            customEndTime: new DateTime(2026, 1, 23, 11, 0, 0),
            customStartTime: firstEnd);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenCustomStartTimeInFuture_ThenEndMeetingManuallyThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2026, 1, 23, 10, 0, 0);
        var now = new DateTime(2026, 1, 23, 11, 0, 0);
        var futureStart = new DateTime(2026, 1, 23, 12, 0, 0);

        _clockMock.Setup(x => x.Now).Returns(startTime);
        var meeting = await StartOngoingMeeting(startTime);
        _clockMock.Setup(x => x.Now).Returns(now);

        // When
        var act = async () => await _tracker.EndMeetingManuallyAsync(
            meeting.Guid, null, customEndTime: now, customStartTime: futureStart);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Start time*cannot be in the future*");
    }

    [Fact]
    public async Task GivenEndedMeetingExists_WhenEndMeetingManuallyOverlaps_ThenThrowsArgumentException()
    {
        // Given
        var firstStart = new DateTime(2026, 1, 23, 9, 0, 0);
        var now = new DateTime(2026, 1, 23, 12, 0, 0);

        _clockMock.Setup(x => x.Now).Returns(firstStart);
        var firstMeeting = await StartMeetingAndTransitionToPending(firstStart);
        _clockMock.Setup(x => x.Now).Returns(now);
        await _tracker.ConfirmMeetingEndAsync(
            firstMeeting.Guid, null,
            customEndTime: new DateTime(2026, 1, 23, 10, 0, 0));

        var secondStart = new DateTime(2026, 1, 23, 10, 30, 0);
        _clockMock.Setup(x => x.Now).Returns(secondStart);
        var secondMeeting = await StartOngoingMeeting(secondStart);
        _clockMock.Setup(x => x.Now).Returns(now);

        // When — overlap with first meeting
        var act = async () => await _tracker.EndMeetingManuallyAsync(
            secondMeeting.Guid, null,
            customEndTime: new DateTime(2026, 1, 23, 11, 0, 0),
            customStartTime: new DateTime(2026, 1, 23, 9, 30, 0));

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*overlaps with already recognized meeting*");
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

    private async Task<StartedMeeting> StartOngoingMeeting(DateTime startTime)
    {
        var meeting = new StartedMeeting(Guid.NewGuid(), startTime, "Test Meeting 2");
        var matchedRuleId = Guid.NewGuid();

        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));

        await _tracker.RecognizeActivityAsync();

        return meeting;
    }
}
