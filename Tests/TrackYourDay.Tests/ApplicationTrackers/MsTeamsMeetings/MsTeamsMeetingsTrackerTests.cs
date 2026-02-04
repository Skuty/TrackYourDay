using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public class MsTeamsMeetingsTrackerTests
{
    private readonly IClock _clock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IMeetingDiscoveryStrategy> _meetingDiscoveryStrategyMock;
    private readonly Mock<ILogger<MsTeamsMeetingTracker>> _loggerMock;
    private readonly MsTeamsMeetingTracker _msTeamsMeetingsTracker;

    public MsTeamsMeetingsTrackerTests()
    {
        _clock = new Clock();
        _loggerMock = new Mock<ILogger<MsTeamsMeetingTracker>>();
        _publisherMock = new Mock<IPublisher>();
        _meetingDiscoveryStrategyMock = new Mock<IMeetingDiscoveryStrategy>();
        _msTeamsMeetingsTracker = new MsTeamsMeetingTracker(
            _clock, 
            _publisherMock.Object, 
            _meetingDiscoveryStrategyMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GivenMeetingIsNotStarted_WhenMeetingIsStarted_ThenMeetingStartedEventIsPublished()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
        var matchedRuleId = Guid.NewGuid();
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));

        // When
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();

        // Then
        _publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GivenMeetingIsOngoing_WhenSameMeetingRecognized_ThenMeetingStartedEventIsNotPublished()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns((meeting, matchedRuleId));

        // When
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();

        // Then
        _publisherMock.Verify(x => x.Publish(It.IsAny<MeetingStartedEvent>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GivenMeetingIsStarted_WhenMeetingEnds_ThenMeetingEndConfirmationRequestedEventIsPublished()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));

        // When
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();

        // Then
        _publisherMock.Verify(x => x.Publish(It.IsAny<MeetingEndConfirmationRequestedEvent>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GivenEndedMeeting_WhenSettingDescription_ThenDescriptionIsSet()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _clock.Now, "Test meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();
        
        await _msTeamsMeetingsTracker.ConfirmMeetingEndAsync(meetingGuid, "Discussed project requirements");

        // When
        var endedMeeting = _msTeamsMeetingsTracker.GetEndedMeetings().First(m => m.Guid == meetingGuid);

        // Then
        Assert.Equal("Discussed project requirements", endedMeeting.CustomDescription);
        Assert.Equal("Discussed project requirements", endedMeeting.GetDescription());
    }

    [Fact]
    public async Task GivenEndedMeetingWithoutDescription_WhenGettingDescription_ThenReturnsMeetingTitle()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var meeting = new StartedMeeting(meetingGuid, _clock.Now, "Test meeting");
        var matchedRuleId = Guid.NewGuid();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(null, null))
            .Returns((meeting, matchedRuleId));
        
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();
        
        _meetingDiscoveryStrategyMock
            .Setup(x => x.RecognizeMeeting(meeting, matchedRuleId))
            .Returns(((StartedMeeting?)null, (Guid?)null));
        
        await _msTeamsMeetingsTracker.RecognizeActivityAsync();
        
        await _msTeamsMeetingsTracker.ConfirmMeetingEndAsync(meetingGuid);

        // When
        var endedMeeting = _msTeamsMeetingsTracker.GetEndedMeetings().First(m => m.Guid == meetingGuid);

        // Then
        Assert.Null(endedMeeting.CustomDescription);
        Assert.Equal("Test meeting", endedMeeting.GetDescription());
    }
}
