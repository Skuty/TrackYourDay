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
public class MeetingContinuityTests
{
    private readonly Mock<IMeetingRuleEngine> _engineMock;
    private readonly Mock<IMeetingRuleRepository> _repositoryMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly Mock<IMeetingStateCache> _stateCacheMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>> _loggerMock;
    private readonly ConfigurableMeetingDiscoveryStrategy _strategy;
    private readonly DateTime _now;

    public MeetingContinuityTests()
    {
        _engineMock = new Mock<IMeetingRuleEngine>();
        _repositoryMock = new Mock<IMeetingRuleRepository>();
        _processServiceMock = new Mock<IProcessService>();
        _stateCacheMock = new Mock<IMeetingStateCache>();
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>>();
        
        _now = DateTime.UtcNow;
        _clockMock.Setup(x => x.Now).Returns(_now);
        
        _strategy = new ConfigurableMeetingDiscoveryStrategy(
            _engineMock.Object,
            _repositoryMock.Object,
            _processServiceMock.Object,
            _stateCacheMock.Object,
            _clockMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GivenOngoingMeeting_WhenSameRuleMatchesWithDifferentTitle_ThenMeetingContinuesWithOriginalTitle()
    {
        // Given
        var ruleId = Guid.NewGuid();
        var originalMeeting = new StartedMeeting(Guid.NewGuid(), _now.AddMinutes(-10), "Daily Standup");
        
        var rule = new MeetingRecognitionRule
        {
            Id = ruleId,
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            MatchCount = 0
        };

        _repositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([
            new TestProcessInfo("ms-teams", "Different Meeting Title")
        ]);

        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns(ruleId);
        _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(originalMeeting);

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                It.IsAny<Guid?>()))
            .Returns(new MeetingMatch
            {
                MatchedRuleId = ruleId,
                ProcessName = "ms-teams",
                WindowTitle = "Different Meeting Title",
                MatchedAt = _now
            });

        // When
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().NotBeNull();
        result!.Guid.Should().Be(originalMeeting.Guid);
        result.Title.Should().Be("Daily Standup"); // Original title preserved
        result.StartDate.Should().Be(originalMeeting.StartDate);
        
        _stateCacheMock.Verify(x => x.SetOngoingMeeting(It.IsAny<StartedMeeting>()), Times.Never);
    }

    [Fact]
    public void GivenOngoingMeeting_WhenDifferentRuleMatches_ThenNewMeetingStarted()
    {
        // Given
        var originalRuleId = Guid.NewGuid();
        var newRuleId = Guid.NewGuid();
        var originalMeeting = new StartedMeeting(Guid.NewGuid(), _now.AddMinutes(-10), "Daily Standup");
        
        var rules = new List<MeetingRecognitionRule>
        {
            new()
            {
                Id = originalRuleId,
                Priority = 1,
                Criteria = MatchingCriteria.ProcessNameOnly,
                ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
                MatchCount = 0
            },
            new()
            {
                Id = newRuleId,
                Priority = 2,
                Criteria = MatchingCriteria.WindowTitleOnly,
                WindowTitlePattern = PatternDefinition.CreateStringPattern("Zoom", PatternMatchMode.Contains, false),
                MatchCount = 0
            }
        };

        _repositoryMock.Setup(x => x.GetAllRules()).Returns(rules);
        
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([
            new TestProcessInfo("zoom", "Zoom Meeting")
        ]);

        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns(originalRuleId);
        _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(originalMeeting);

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                It.IsAny<Guid?>()))
            .Returns(new MeetingMatch
            {
                MatchedRuleId = newRuleId,
                ProcessName = "zoom",
                WindowTitle = "Zoom Meeting",
                MatchedAt = _now
            });

        // When
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().NotBeNull();
        result!.Guid.Should().NotBe(originalMeeting.Guid);
        result.Title.Should().Be("Zoom Meeting");
        result.StartDate.Should().Be(_now);
        
        _stateCacheMock.Verify(x => x.SetMatchedRuleId(newRuleId), Times.Once);
        _stateCacheMock.Verify(x => x.SetOngoingMeeting(It.Is<StartedMeeting>(m => m.Title == "Zoom Meeting")), Times.Once);
        _repositoryMock.Verify(x => x.IncrementMatchCount(newRuleId, _now), Times.Once);
    }

    [Fact]
    public void GivenNoOngoingMeeting_WhenNewMatch_ThenNewMeetingStarted()
    {
        // Given
        var ruleId = Guid.NewGuid();
        var rule = new MeetingRecognitionRule
        {
            Id = ruleId,
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            MatchCount = 0
        };

        _repositoryMock.Setup(x => x.GetAllRules()).Returns([rule]);
        
        _processServiceMock.Setup(x => x.GetProcesses()).Returns([
            new TestProcessInfo("ms-teams", "Daily Meeting")
        ]);

        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns((Guid?)null);
        _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns((StartedMeeting?)null);

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                It.IsAny<Guid?>()))
            .Returns(new MeetingMatch
            {
                MatchedRuleId = ruleId,
                ProcessName = "ms-teams",
                WindowTitle = "Daily Meeting",
                MatchedAt = _now
            });

        // When
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().NotBeNull();
        result!.Title.Should().Be("Daily Meeting");
        result.StartDate.Should().Be(_now);
        
        _stateCacheMock.Verify(x => x.SetMatchedRuleId(ruleId), Times.Once);
        _stateCacheMock.Verify(x => x.SetOngoingMeeting(It.Is<StartedMeeting>(m => m.Title == "Daily Meeting")), Times.Once);
        _repositoryMock.Verify(x => x.IncrementMatchCount(ruleId, _now), Times.Once);
    }

    [Fact]
    public void GivenOngoingMeeting_WhenNoMatchFound_ThenStateClearedAndReturnsNull()
    {
        // Given
        var ruleId = Guid.NewGuid();
        var originalMeeting = new StartedMeeting(Guid.NewGuid(), _now.AddMinutes(-10), "Daily Standup");
        
        _repositoryMock.Setup(x => x.GetAllRules()).Returns([
            new MeetingRecognitionRule
            {
                Id = ruleId,
                Priority = 1,
                Criteria = MatchingCriteria.ProcessNameOnly,
                ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
                MatchCount = 0
            }
        ]);
        
        _processServiceMock.Setup(x => x.GetProcesses()).Returns(Array.Empty<TestProcessInfo>());

        _stateCacheMock.Setup(x => x.GetMatchedRuleId()).Returns(ruleId);
        _stateCacheMock.Setup(x => x.GetOngoingMeeting()).Returns(originalMeeting);

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                It.IsAny<Guid?>()))
            .Returns((MeetingMatch?)null);

        // When
        var result = _strategy.RecognizeMeeting();

        // Then
        result.Should().BeNull();
        _stateCacheMock.Verify(x => x.SetMatchedRuleId(null), Times.Once);
        _stateCacheMock.Verify(x => x.SetOngoingMeeting(null), Times.Once);
    }

    private class TestProcessInfo : IProcess
    {
        public TestProcessInfo(string processName, string mainWindowTitle)
        {
            ProcessName = processName;
            MainWindowTitle = mainWindowTitle;
        }

        public string ProcessName { get; }
        public string MainWindowTitle { get; }
    }
}
