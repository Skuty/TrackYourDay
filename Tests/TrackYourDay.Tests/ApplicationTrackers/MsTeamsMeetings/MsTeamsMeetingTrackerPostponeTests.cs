using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using Xunit;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

public sealed class MsTeamsMeetingTrackerPostponeTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _strategyMock;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly MsTeamsMeetingTracker _sut;
    private DateTime _now;

    public MsTeamsMeetingTrackerPostponeTests()
    {
        _now = new DateTime(2026, 2, 6, 10, 0, 0);
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(c => c.Now).Returns(() => _now);

        _publisherMock = new Mock<IPublisher>();
        _strategyMock = new Mock<IMeetingDiscoveryStrategy>();
        _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();

        _sut = new MsTeamsMeetingTracker(_clockMock.Object, _publisherMock.Object, _strategyMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenPostponeWithValidTime_ThenPublishesEvent()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));

        await _sut.RecognizeActivityAsync();
        
        var postponeUntil = _now.AddMinutes(10);

        // When
        await _sut.PostponeCheckAsync(meetingGuid, postponeUntil);

        // Then
        _publisherMock.Verify(p => p.Publish(
            It.Is<MeetingCheckPostponedEvent>(e => 
                e.MeetingGuid == meetingGuid && 
                e.PostponedUntil == postponeUntil),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenPendingMeeting_WhenPostpone_ThenRestoresToOngoing()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        
        // Start meeting
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        await _sut.RecognizeActivityAsync();
        
        // Transition to pending (meeting window closed)
        _strategyMock.Setup(s => s.RecognizeMeeting(meeting, null)).Returns(((StartedMeeting?)null, (Guid?)null));
        await _sut.RecognizeActivityAsync();

        // When
        await _sut.PostponeCheckAsync(meetingGuid, _now.AddMinutes(5));

        // Then - meeting should be restored to ongoing, next detection should work
        _strategyMock.Setup(s => s.RecognizeMeeting(meeting, null)).Returns((meeting, (Guid?)null));
        await _sut.RecognizeActivityAsync(); // Should not throw or block
    }

    [Fact]
    public async Task GivenPostponeActive_WhenRecognizeActivity_ThenSkipsDetection()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        
        await _sut.RecognizeActivityAsync();
        await _sut.PostponeCheckAsync(meetingGuid, _now.AddMinutes(10));

        _strategyMock.Invocations.Clear();

        // When
        _now = _now.AddMinutes(5); // Still within postpone window
        await _sut.RecognizeActivityAsync();

        // Then
        _strategyMock.Verify(s => s.RecognizeMeeting(It.IsAny<StartedMeeting?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task GivenPostponeExpired_WhenRecognizeActivity_ThenResumesDetection()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        
        await _sut.RecognizeActivityAsync();
        await _sut.PostponeCheckAsync(meetingGuid, _now.AddMinutes(10));

        _strategyMock.Invocations.Clear();
        _strategyMock.Setup(s => s.RecognizeMeeting(meeting, null)).Returns((meeting, (Guid?)null));

        // When
        _now = _now.AddMinutes(11); // Past postpone window
        await _sut.RecognizeActivityAsync();

        // Then
        _strategyMock.Verify(s => s.RecognizeMeeting(It.IsAny<StartedMeeting?>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Theory]
    [InlineData(-5)] // Past time
    [InlineData(0)]  // Current time
    public async Task GivenInvalidPostponeTime_WhenPostpone_ThenThrowsArgumentException(int minuteOffset)
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        await _sut.RecognizeActivityAsync();

        var invalidTime = _now.AddMinutes(minuteOffset);

        // When
        var act = () => _sut.PostponeCheckAsync(meetingGuid, invalidTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be in the future*");
    }

    [Fact]
    public async Task GivenPostponeBeyond24Hours_WhenPostpone_ThenThrowsArgumentException()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        await _sut.RecognizeActivityAsync();

        var tooFarFuture = _now.AddHours(25);

        // When
        var act = () => _sut.PostponeCheckAsync(meetingGuid, tooFarFuture);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot exceed 24 hours*");
    }

    [Fact]
    public async Task GivenWrongMeetingGuid_WhenPostpone_ThenThrowsArgumentException()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        await _sut.RecognizeActivityAsync();

        var wrongGuid = Guid.NewGuid();

        // When
        var act = () => _sut.PostponeCheckAsync(wrongGuid, _now.AddMinutes(10));

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*does not match*");
    }

    [Fact]
    public async Task GivenPostponeActive_WhenManualEnd_ThenClearsPostpone()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        
        await _sut.RecognizeActivityAsync();
        await _sut.PostponeCheckAsync(meetingGuid, _now.AddMinutes(10));

        // When
        await _sut.EndMeetingManuallyAsync(meetingGuid);

        // Then - should resume detection after manual end
        _strategyMock.Invocations.Clear();
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _sut.RecognizeActivityAsync();
        
        _strategyMock.Verify(s => s.RecognizeMeeting(It.IsAny<StartedMeeting?>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task GivenPostponeActive_WhenConfirmEndFromOngoing_ThenClearsPostpone()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _now, "Daily Standup");
        
        // Start and transition to pending
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns((meeting, (Guid?)null));
        await _sut.RecognizeActivityAsync();
        
        _strategyMock.Setup(s => s.RecognizeMeeting(meeting, null)).Returns(((StartedMeeting?)null, (Guid?)null));
        await _sut.RecognizeActivityAsync();
        
        // Postpone restores meeting to ongoing state
        await _sut.PostponeCheckAsync(meetingGuid, _now.AddMinutes(10));

        // When - end meeting manually (since it's now ongoing, not pending)
        await _sut.EndMeetingManuallyAsync(meetingGuid);

        // Then - should resume detection after manual end
        _strategyMock.Invocations.Clear();
        _strategyMock.Setup(s => s.RecognizeMeeting(null, null)).Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _sut.RecognizeActivityAsync();
        
        _strategyMock.Verify(s => s.RecognizeMeeting(It.IsAny<StartedMeeting?>(), It.IsAny<Guid?>()), Times.Once);
    }
}
