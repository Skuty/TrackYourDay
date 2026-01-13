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
public class MeetingContinuityTests
{
    private readonly Mock<IMeetingRuleEngine> _engineMock;
    private readonly Mock<IMeetingRuleRepository> _repositoryMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>> _loggerMock;
    private readonly ConfigurableMeetingDiscoveryStrategy _strategy;
    private readonly DateTime _now;

    public MeetingContinuityTests()
    {
        _engineMock = new Mock<IMeetingRuleEngine>();
        _repositoryMock = new Mock<IMeetingRuleRepository>();
        _processServiceMock = new Mock<IProcessService>();
        _loggerMock = new Mock<ILogger<ConfigurableMeetingDiscoveryStrategy>>();
        
        _now = DateTime.UtcNow;
        
        _strategy = new ConfigurableMeetingDiscoveryStrategy(
            _engineMock.Object,
            _repositoryMock.Object,
            _processServiceMock.Object,
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

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                ruleId))
            .Returns(new MeetingMatch
            {
                MatchedRuleId = ruleId,
                ProcessName = "ms-teams",
                WindowTitle = "Different Meeting Title",
                MatchedAt = _now
            });

        // When
        var (meeting, matchedRuleId) = _strategy.RecognizeMeeting(originalMeeting, ruleId);

        // Then
        meeting.Should().NotBeNull();
        meeting!.Guid.Should().Be(originalMeeting.Guid);
        meeting.Title.Should().Be("Daily Standup");
        meeting.StartDate.Should().Be(originalMeeting.StartDate);
        matchedRuleId.Should().Be(ruleId);
        
        _repositoryMock.Verify(x => x.IncrementMatchCount(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
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

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                originalRuleId))
            .Returns(new MeetingMatch
            {
                MatchedRuleId = newRuleId,
                ProcessName = "zoom",
                WindowTitle = "Zoom Meeting",
                MatchedAt = _now
            });

        // When
        var (meeting, matchedRuleId) = _strategy.RecognizeMeeting(originalMeeting, originalRuleId);

        // Then
        meeting.Should().NotBeNull();
        meeting!.Guid.Should().NotBe(originalMeeting.Guid);
        meeting.Title.Should().Be("Zoom Meeting");
        meeting.StartDate.Should().Be(_now);
        matchedRuleId.Should().Be(newRuleId);
        
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

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                null))
            .Returns(new MeetingMatch
            {
                MatchedRuleId = ruleId,
                ProcessName = "ms-teams",
                WindowTitle = "Daily Meeting",
                MatchedAt = _now
            });

        // When
        var (meeting, matchedRuleId) = _strategy.RecognizeMeeting(null, null);

        // Then
        meeting.Should().NotBeNull();
        meeting!.Title.Should().Be("Daily Meeting");
        meeting.StartDate.Should().Be(_now);
        matchedRuleId.Should().Be(ruleId);
        
        _repositoryMock.Verify(x => x.IncrementMatchCount(ruleId, _now), Times.Once);
    }

    [Fact]
    public void GivenOngoingMeeting_WhenNoMatchFound_ThenReturnsNull()
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

        _engineMock.Setup(x => x.EvaluateRules(
                It.IsAny<IReadOnlyList<MeetingRecognitionRule>>(),
                It.IsAny<IEnumerable<ProcessInfo>>(),
                ruleId))
            .Returns((MeetingMatch?)null);

        // When
        var (meeting, matchedRuleId) = _strategy.RecognizeMeeting(originalMeeting, ruleId);

        // Then
        meeting.Should().BeNull();
        matchedRuleId.Should().BeNull();
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
