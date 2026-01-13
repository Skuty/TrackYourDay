using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public class ProcessBasedMeetingRecognizingStrategyTests
{
    private readonly ProcessBasedMeetingRecognizingStrategy _strategy;
    private readonly Mock<ILogger<ProcessBasedMeetingRecognizingStrategy>> _mockLogger;
    private readonly Mock<IProcessService> _mockProcessService;

    public ProcessBasedMeetingRecognizingStrategyTests()
    {
        _mockLogger = new Mock<ILogger<ProcessBasedMeetingRecognizingStrategy>>();
        _mockProcessService = new Mock<IProcessService>();
        _strategy = new ProcessBasedMeetingRecognizingStrategy(_mockLogger.Object, _mockProcessService.Object);
    }

    [Theory]
    [InlineData("ms-teams", "Imie Nazwisko, Imie2 Nazwisko2 | Microsoft Teams")]
    [InlineData("ms-teams", "Imie Nazwisko | Microsoft Teams")]
    [InlineData("ms-teams", "Testowe spotkanko | Microsoft Teams")]
    [InlineData("ms-teams", "Spotkanie usprawniające / retrospektywa | Microsoft Teams")]
    [InlineData("ms-teams", "Pasek sterowania Udostępnianie | Microsoft Teams")]
    [InlineData("ms-teams", "Widok kompaktowy spotkania | Imie Nazwisko | Microsoft Teams")]
    public void GivenProcessNameAndWindowTitleMatchesTeamsMeeting_WhenAttemptingToRecognizeMeeting_ThenMeetingIsRecognized(string processName, string windowTitle)
    {
        // Arrange
        _mockProcessService.Setup(x => x.GetProcesses()).Returns(new List<IProcess>
        {
            new ProcessSnapshot
            {
                ProcessName = processName,
                MainWindowTitle = windowTitle
            }
        });

        // Act
        var (meeting, ruleId) = _strategy.RecognizeMeeting(null, null);

        // Assert
        Assert.NotNull(meeting);
        Assert.Equal(windowTitle, meeting.Title);
    }

    [Theory]
    [InlineData("ms-teams", "")]
    [InlineData("ms-teams", "Microsoft Teams")]
    [InlineData("ms-teams", "Czat | Microsoft Teams")]
    [InlineData("ms-teams", "Czat | Imie Nazwisko | Microsoft Teams")]
    [InlineData("ms-teams", "Czat | Sztosy | Microsoft Teams")]
    [InlineData("ms-teams", "Aktywność | Sztosy | Microsoft Teams")]
    [InlineData("ms-teams", "Aktywność | Kick off Squadu | Microsoft Teams")]
    public void GivenProcessNameAndWindowTitleDoesNotMatchesTeamsMeeting_WhenAttemptingToRecognizeMeeting_ThenMeetingIsNotRecognized(string processName, string windowTitle)
    {
        // Arrange
        _mockProcessService.Setup(x => x.GetProcesses()).Returns(new List<IProcess>
        {
            new ProcessSnapshot
            {
                ProcessName = processName,
                MainWindowTitle = windowTitle
            }
        });

        // Act
        var (meeting, ruleId) = _strategy.RecognizeMeeting(null, null);

        // Assert
        Assert.Null(meeting);
    }
}
