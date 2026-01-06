using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public class ConfigurableMeetingDiscoveryStrategyTests
{
    private readonly Mock<IMeetingRuleEngine> _ruleEngineMock;
    private readonly Mock<IMeetingRuleRepository> _ruleRepositoryMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly Mock<IMeetingStateCache> _stateCacheMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>> _loggerMock;
    private readonly ConfigurableMeetingDiscoveryStrategy _strategy;
    private readonly DateTime _now;

    public ConfigurableMeetingDiscoveryStrategyTests()
    {
        _ruleEngineMock = new Mock<IMeetingRuleEngine>();
        _ruleRepositoryMock = new Mock<IMeetingRuleRepository>();
        _processServiceMock = new Mock<IProcessService>();
        _stateCacheMock = new Mock<IMeetingStateCache>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>>();
        _now = DateTime.UtcNow;
        _clockMock.Setup(x => x.Now).Returns(_now);

        _strategy = new ConfigurableMeetingDiscoveryStrategy(
            _ruleEngineMock.Object,
            _ruleRepositoryMock.Object,
            _processServiceMock.Object,
            _stateCacheMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GivenNoRules_WhenRecognizingMeeting_ThenReturnsNull()
    {
        // Given
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([]);

        // When
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().BeNull();
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
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenMatchingProcess_WhenNoOngoingMeeting_ThenReturnsNewMeeting()
    {
        // Given
        var rule = CreateTestRule();
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([new ProcessSnapshot { ProcessName = "ms-teams", MainWindowTitle = "Test Meeting" }]);
        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns((Guid?)null);
        _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns((StartedMeeting?)null);

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
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Meeting");
        result.StartDate.Should().Be(_now);
        _ruleRepositoryMock.Verify(x => x.IncrementMatchCount(rule.Id, _now), Times.Once);
        _stateCacheMock.Verify(x => x.SetMatchedRuleId(rule.Id), Times.Once);
    }

    [Fact]
    public void GivenMatchingProcess_WhenSameRuleOngoingMeeting_ThenReturnsExistingMeeting()
    {
        // Given
        var rule = CreateTestRule();
        var existingMeeting = new StartedMeeting(Guid.NewGuid(), _now.AddMinutes(-5), "Existing Meeting");
        
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([new ProcessSnapshot { ProcessName = "ms-teams", MainWindowTitle = "Test Meeting" }]);
        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns(rule.Id);
        _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(existingMeeting);

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
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().Be(existingMeeting);
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
        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns(rule1.Id);
        _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(existingMeeting);

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
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().NotBe(existingMeeting);
        result!.Title.Should().Be("New Meeting");
        _ruleRepositoryMock.Verify(x => x.IncrementMatchCount(rule2.Id, _now), Times.Once);
        _stateCacheMock.Verify(x => x.SetMatchedRuleId(rule2.Id), Times.Once);
    }

    [Fact]
    public void GivenNoMatch_WhenOngoingMeetingExists_ThenClearsMatchedRuleId()
    {
        // Given
        var rule = CreateTestRule();
        _ruleRepositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([]);
        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns(rule.Id);
        _ruleEngineMock.Setup(x => x.EvaluateRules(It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), It.IsAny<IEnumerable<ProcessInfo>>(), rule.Id))
            .Returns((MeetingMatch?)null);

        // When
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().BeNull();
        _stateCacheMock.Verify(x => x.SetMatchedRuleId(null), Times.Once);
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
        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns((Guid?)null);

        ProcessInfo[]? capturedProcesses = null;
        _ruleEngineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(), 
                It.IsAny<IEnumerable<ProcessInfo>>(), 
                It.IsAny<Guid?>()))
            .Callback<IReadOnlyList<MeetingRecognitionRule>, IEnumerable<ProcessInfo>, Guid?>(
                (r, p, id) => capturedProcesses = p.ToArray())
            .Returns((MeetingMatch?)null);

        // When
        _strategy.RecognizeMeeting();

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
