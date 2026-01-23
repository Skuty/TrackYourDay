using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public class MsTeamsMeetingTrackerManualEndTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _meetingDiscoveryStrategyMock;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly MsTeamsMeetingTracker _tracker;

    public MsTeamsMeetingTrackerManualEndTests()
    {
        _clockMock = new Mock<IClock>();
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
    public async Task GivenOngoingMeeting_WhenEndedManually_ThenMeetingEndedEventIsPublished()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, startTime, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();
        _publisherMock.Invocations.Clear();

        // When
        await _tracker.EndMeetingManuallyAsync(meetingGuid);

        // Then
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<MeetingEndedEvent>(e => e.EndedMeeting.GetDescription() == "Test Meeting"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenEndedManuallyWithCustomDescription_ThenDescriptionIsApplied()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, startTime, "Original Title");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();

        // When
        await _tracker.EndMeetingManuallyAsync(meetingGuid, "Custom Description");

        // Then
        var endedMeetings = _tracker.GetEndedMeetings();
        endedMeetings.Should().HaveCount(1);
        endedMeetings.First().GetDescription().Should().Be("Custom Description");
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenEndedManuallyWithCustomEndTime_ThenEndTimeIsApplied()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, startTime, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        var customEndTime = startTime.AddMinutes(30);
        _clockMock.Setup(x => x.Now).Returns(startTime.AddMinutes(40));

        // When
        await _tracker.EndMeetingManuallyAsync(meetingGuid, customEndTime: customEndTime);

        // Then
        var endedMeetings = _tracker.GetEndedMeetings();
        endedMeetings.Should().HaveCount(1);
        endedMeetings.First().EndDate.Should().Be(customEndTime);
        endedMeetings.First().GetDuration().Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task GivenNoOngoingMeeting_WhenEndedManually_ThenThrowsInvalidOperationException()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        _clockMock.Setup(x => x.Now).Returns(DateTime.Now);

        // When
        var act = async () => await _tracker.EndMeetingManuallyAsync(meetingGuid);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"No meeting found with ID {meetingGuid}");
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenEndedManuallyWithWrongGuid_ThenThrowsInvalidOperationException()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var correctGuid = Guid.NewGuid();
        var wrongGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(correctGuid, startTime, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();

        // When
        var act = async () => await _tracker.EndMeetingManuallyAsync(wrongGuid);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"No meeting found with ID {wrongGuid}");
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenEndedManuallyWithEndTimeBeforeStart_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, startTime, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        var invalidEndTime = startTime.AddMinutes(-10);
        _clockMock.Setup(x => x.Now).Returns(startTime.AddMinutes(30));

        // When
        var act = async () => await _tracker.EndMeetingManuallyAsync(meetingGuid, customEndTime: invalidEndTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time*cannot be before meeting start time*");
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenEndedManuallyWithFutureEndTime_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var currentTime = startTime.AddMinutes(30);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, startTime, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        _clockMock.Setup(x => x.Now).Returns(currentTime);
        var futureEndTime = currentTime.AddMinutes(10);

        // When
        var act = async () => await _tracker.EndMeetingManuallyAsync(meetingGuid, customEndTime: futureEndTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*End time*cannot be in the future*");
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenEndedManuallyWithTooLongDescription_ThenThrowsArgumentException()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, startTime, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();
        
        var tooLongDescription = new string('A', 501);

        // When
        var act = async () => await _tracker.EndMeetingManuallyAsync(meetingGuid, tooLongDescription);

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Description cannot exceed 500 characters*");
    }

    [Fact]
    public async Task GivenOngoingMeeting_WhenEndedManually_ThenOngoingMeetingIsCleared()
    {
        // Given
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _clockMock.Setup(x => x.Now).Returns(startTime);
        
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, startTime, "Test Meeting");
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, (Guid?)null));
        
        _tracker.RecognizeActivity();

        // When
        await _tracker.EndMeetingManuallyAsync(meetingGuid);

        // Then
        _tracker.GetOngoingMeeting().Should().BeNull();
    }
}
