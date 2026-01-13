using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public class ConfigurableMeetingDiscoveryStrategyTests
{
    private readonly Mock<IMeetingRuleEngine> _ruleEngineMock;
    private readonly Mock<IMeetingRuleRepository> _ruleRepositoryMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>> _loggerMock;
    private readonly ConfigurableMeetingDiscoveryStrategy _strategy;
    private readonly DateTime _now;

    public ConfigurableMeetingDiscoveryStrategyTests()
    {
        _ruleEngineMock = new Mock<IMeetingRuleEngine>();
        _ruleRepositoryMock = new Mock<IMeetingRuleRepository>();
        _processServiceMock = new Mock<IProcessService>();
        _loggerMock = new Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>>();
        _now = DateTime.UtcNow;

        _strategy = new ConfigurableMeetingDiscoveryStrategy(
            _ruleEngineMock.Object,
            _ruleRepositoryMock.Object,
            _processServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GivenNoRules_WhenRecognizingMeeting_ThenReturnsNull()
    {
        // Given
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([]);

        // When
        var (meeting, ruleId) = _strategy.RecognizeMeeting(null, null);

        // Then
        meeting.Should().BeNull();
        ruleId.Should().BeNull();
    }

    [Fact]
    public void GivenNoMatchingProcess_WhenRecognizingMeeting_ThenReturnsNull()
    {
        // Given
        var rules = new List<MeetingRecognitionRule> { CreateTestRule() };
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns(rules);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([]);
        _ruleEngineMock.Setup(x => x.EvaluateRules(It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), It.IsAny<IEnumerable<ProcessInfo>>(), It.IsAny<Guid?>()))
            .Returns((MeetingMatch?)null);

        // When
        var (meeting, ruleId) = _strategy.RecognizeMeeting(null, null);

        // Then
        meeting.Should().BeNull();
        ruleId.Should().BeNull();
    }

    [Fact]
    public void GivenMatchingProcess_WhenNoOngoingMeeting_ThenReturnsNewMeeting()
    {
        // Given
        var rule = CreateTestRule();
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([new ProcessSnapshot { ProcessName = "ms-teams", MainWindowTitle = "Test Meeting" }]);

        var match = new MeetingMatch
        {
            MatchedRuleId = rule.Id,
            ProcessName = "ms-teams",
            WindowTitle = "Test Meeting",
            MatchedAt = _now
        };
        _ruleEngineMock.Setup(x => x.EvaluateRules(It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), It.IsAny<IEnumerable<ProcessInfo>>(), null))
            .Returns(match);

        // When
        var (meeting, ruleId) = _strategy.RecognizeMeeting(null, null);

        // Then
        meeting.Should().NotBeNull();
        meeting!.Title.Should().Be("Test Meeting");
        meeting.StartDate.Should().Be(_now);
        ruleId.Should().Be(rule.Id);
        _ruleRepositoryMock.Verify(x => x.IncrementMatchCount(rule.Id, _now), Times.Once);
    }

    [Fact]
    public void GivenMatchingProcess_WhenSameRuleOngoingMeeting_ThenReturnsExistingMeeting()
    {
        // Given
        var rule = CreateTestRule();
        var existingMeeting = new StartedMeeting(Guid.NewGuid(), _now.AddMinutes(-5), "Existing Meeting");
        
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([new ProcessSnapshot { ProcessName = "ms-teams", MainWindowTitle = "Test Meeting" }]);

        var match = new MeetingMatch
        {
            MatchedRuleId = rule.Id,
            ProcessName = "ms-teams",
            WindowTitle = "Test Meeting",
            MatchedAt = _now
        };
        _ruleEngineMock.Setup(x => x.EvaluateRules(It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), It.IsAny<IEnumerable<ProcessInfo>>(), rule.Id))
            .Returns(match);

        // When
        var (meeting, ruleId) = _strategy.RecognizeMeeting(existingMeeting, rule.Id);

        // Then
        meeting.Should().Be(existingMeeting);
        ruleId.Should().Be(rule.Id);
        _ruleRepositoryMock.Verify(x => x.IncrementMatchCount(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public void GivenMatchingProcess_WhenDifferentRuleOngoingMeeting_ThenReturnsNewMeeting()
    {
        // Given
        var rule1 = CreateTestRule();
        var rule2 = CreateTestRule();
        var existingMeeting = new StartedMeeting(Guid.NewGuid(), _now.AddMinutes(-5), "Old Meeting");
        
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule1, rule2]);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([new ProcessSnapshot { ProcessName = "ms-teams", MainWindowTitle = "New Meeting" }]);

        var match = new MeetingMatch
        {
            MatchedRuleId = rule2.Id,
            ProcessName = "ms-teams",
            WindowTitle = "New Meeting",
            MatchedAt = _now
        };
        _ruleEngineMock.Setup(x => x.EvaluateRules(It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), It.IsAny<IEnumerable<ProcessInfo>>(), rule1.Id))
            .Returns(match);

        // When
        var (meeting, ruleId) = _strategy.RecognizeMeeting(existingMeeting, rule1.Id);

        // Then
        meeting.Should().NotBe(existingMeeting);
        meeting!.Title.Should().Be("New Meeting");
        ruleId.Should().Be(rule2.Id);
        _ruleRepositoryMock.Verify(x => x.IncrementMatchCount(rule2.Id, _now), Times.Once);
    }

    [Fact]
    public void GivenNoMatch_WhenOngoingMeetingExists_ThenReturnsNull()
    {
        // Given
        var rule = CreateTestRule();
        var existingMeeting = new StartedMeeting(Guid.NewGuid(), _now.AddMinutes(-5), "Existing Meeting");
        
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([]);
        _ruleEngineMock.Setup(x => x.EvaluateRules(It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), It.IsAny<IEnumerable<ProcessInfo>>(), rule.Id))
            .Returns((MeetingMatch?)null);

        // When
        var (meeting, ruleId) = _strategy.RecognizeMeeting(existingMeeting, rule.Id);

        // Then
        meeting.Should().BeNull();
        ruleId.Should().BeNull();
    }

    [Fact]
    public void GivenMultipleProcesses_WhenRecognizing_ThenConvertsToProcessInfo()
    {
        // Given
        var rule = CreateTestRule();
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        
        var mockProcesses = new List<IProcess>
        {
            new ProcessSnapshot { ProcessName = "ms-teams", MainWindowTitle = "Meeting 1" },
            new ProcessSnapshot { ProcessName = "ms-teams", MainWindowTitle = "Meeting 2" }
        };
        _processServiceMock.Setup(x => x.GetProcesses()).Returns(mockProcesses);

        ProcessInfo[]? capturedProcesses = null;
        _ruleEngineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), 
                It.IsAny<IEnumerable<ProcessInfo>>(), 
                It.IsAny<Guid?>()))
            .Callback<IReadOnlyList<MeetingRecognitionRule>, IEnumerable<ProcessInfo>, Guid?>(
                (r, p, id) => capturedProcesses = p.ToArray())
            .Returns((MeetingMatch?)null);

        // When
        _strategy.RecognizeMeeting(null, null);

        // Then
        capturedProcesses.Should().HaveCount(2);
        capturedProcesses![0].ProcessName.Should().Be("ms-teams");
        capturedProcesses[0].MainWindowTitle.Should().Be("Meeting 1");
        capturedProcesses[1].MainWindowTitle.Should().Be("Meeting 2");
    }

    private MeetingRecognitionRule CreateTestRule()
    {
        return new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
    }
}
