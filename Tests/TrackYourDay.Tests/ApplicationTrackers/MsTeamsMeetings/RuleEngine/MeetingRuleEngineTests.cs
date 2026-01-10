using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings.RuleEngine;

[Trait("Category", "Unit")]
public class MeetingRuleEngineTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<MeetingRuleEngine>> _loggerMock;
    private readonly MeetingRuleEngine _engine;
    private readonly DateTime _now;

    public MeetingRuleEngineTests()
    {
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<MeetingRuleEngine>>();
        _engine = new MeetingRuleEngine(_clockMock.Object, _loggerMock.Object);
        _now = DateTime.UtcNow;
        _clockMock.Setup(x => x.Now).Returns(_now);
    }

    [Fact]
    public void GivenNullRules_WhenEvaluating_ThenThrowsArgumentNullException()
    {
        // When
        var act = () => _engine.EvaluateRules(null!, [], null);

        // Then
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenNullProcesses_WhenEvaluating_ThenThrowsArgumentNullException()
    {
        // When
        var act = () => _engine.EvaluateRules([], null!, null);

        // Then
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenNoProcesses_WhenEvaluating_ThenReturnsNull()
    {
        // Given
        var rules = CreateSampleRules();

        // When
        var result = _engine.EvaluateRules(rules, [], null);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenMatchingProcess_WhenEvaluating_ThenReturnsMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
        var processes = new[] { new ProcessInfo("ms-teams", "Test Meeting") };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().NotBeNull();
        result!.MatchedRuleId.Should().Be(rule.Id);
        result.ProcessName.Should().Be("ms-teams");
        result.WindowTitle.Should().Be("Test Meeting");
        result.MatchedAt.Should().Be(_now);
    }

    [Fact]
    public void GivenMultipleRules_WhenFirstMatches_ThenReturnsFirstMatch()
    {
        // Given
        var rule1 = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
        var rule2 = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 2,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("zoom", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
        var processes = new[]
        {
            new ProcessInfo("ms-teams", "Meeting"),
            new ProcessInfo("zoom", "Meeting")
        };

        // When
        var result = _engine.EvaluateRules([rule1, rule2], processes, null);

        // Then
        result.Should().NotBeNull();
        result!.MatchedRuleId.Should().Be(rule1.Id);
    }

    [Fact]
    public void GivenBothCriteria_WhenBothMatch_ThenReturnsMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
        var processes = new[] { new ProcessInfo("ms-teams", "Test Meeting") };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().NotBeNull();
    }

    [Fact]
    public void GivenBothCriteria_WhenOnlyProcessMatches_ThenReturnsNull()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
        var processes = new[] { new ProcessInfo("ms-teams", "Chat Window") };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenWindowTitleOnly_WhenWindowTitleMatches_ThenReturnsMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
        var processes = new[] { new ProcessInfo("any-process", "Team Meeting") };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().NotBeNull();
        result!.WindowTitle.Should().Be("Team Meeting");
    }

    [Fact]
    public void GivenExclusionPattern_WhenExclusionMatches_ThenReturnsNull()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Chat |", PatternMatchMode.StartsWith, false)
            ],
            MatchCount = 0
        };
        var processes = new[] { new ProcessInfo("ms-teams", "Chat | John Doe") };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenExclusionPattern_WhenExclusionDoesNotMatch_ThenReturnsMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Chat |", PatternMatchMode.StartsWith, false)
            ],
            MatchCount = 0
        };
        var processes = new[] { new ProcessInfo("ms-teams", "Meeting | John Doe - Microsoft Teams") };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().NotBeNull();
    }

    [Fact]
    public void GivenMultipleExclusions_WhenAnyMatches_ThenReturnsNull()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Chat |", PatternMatchMode.StartsWith, false),
                PatternDefinition.CreateStringPattern("Activity |", PatternMatchMode.StartsWith, false),
                PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Exact, false)
            ],
            MatchCount = 0
        };

        // When - Test each exclusion
        var result1 = _engine.EvaluateRules([rule], [new ProcessInfo("ms-teams", "Chat | John")], null);
        var result2 = _engine.EvaluateRules([rule], [new ProcessInfo("ms-teams", "Activity | Updates")], null);
        var result3 = _engine.EvaluateRules([rule], [new ProcessInfo("ms-teams", "Microsoft Teams")], null);

        // Then
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    [Fact]
    public void GivenFirstProcessMatchesInList_WhenMultipleProcesses_ThenReturnsFirstMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
        var processes = new[]
        {
            new ProcessInfo("other", "Window 1"),
            new ProcessInfo("test-app", "Window 2"),
            new ProcessInfo("test-app", "Window 3")
        };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().NotBeNull();
        result!.WindowTitle.Should().Be("Window 2");
    }

    [Fact]
    public void GivenRegexPattern_WhenMatching_ThenEvaluatesRegex()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = PatternDefinition.CreateRegexPattern(@"Meeting \d+", caseSensitive: false),
            MatchCount = 0
        };
        var processes = new[] { new ProcessInfo("test", "Meeting 123") };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().NotBeNull();
    }

    private List<MeetingRecognitionRule> CreateSampleRules()
    {
        return
        [
            new MeetingRecognitionRule
            {
                Id = Guid.NewGuid(),
                Priority = 1,
                Criteria = MatchingCriteria.ProcessNameOnly,
                ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
                MatchCount = 0
            }
        ];
    }
}
