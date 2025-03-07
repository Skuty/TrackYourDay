using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings
{
    [Trait("Category", "Unit")]
    public class ProcessBasedMeetingRecognizingStrategyTests
    {
        private readonly ProcessBasedMeetingRecognizingStrategy processBasedMeetingRecognizingStrategy;
        private readonly Mock<ILogger<ProcessBasedMeetingRecognizingStrategy>> mockLogger;
        private readonly Mock<IProcessService> mockProcessService;

        public ProcessBasedMeetingRecognizingStrategyTests()
        {
            this.mockLogger = new Mock<ILogger<ProcessBasedMeetingRecognizingStrategy>>();
            this.mockProcessService = new Mock<IProcessService>();
            this.processBasedMeetingRecognizingStrategy = new ProcessBasedMeetingRecognizingStrategy(this.mockLogger.Object, this.mockProcessService.Object);
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
            this.mockProcessService.Setup(x => x.GetProcesses()).Returns(new List<ProcessInfo>
            {
                new ProcessInfo
                {
                    ProcessName = processName,
                    MainWindowTitle = windowTitle
                }
            });

            // Act
            var result = this.processBasedMeetingRecognizingStrategy.RecognizeMeeting();

            // Assert
            Assert.NotNull(result);

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
            this.mockProcessService.Setup(x => x.GetProcesses()).Returns(new List<ProcessInfo>
            {
                new ProcessInfo
                {
                    ProcessName = processName,
                    MainWindowTitle = windowTitle
                }
            });

            // Act
            var result = this.processBasedMeetingRecognizingStrategy.RecognizeMeeting();

            // Assert
            Assert.Null(result);
        }
    }
}
