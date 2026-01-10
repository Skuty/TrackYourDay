using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings.RuleEngine;

[Trait("Category", "Unit")]
public class MeetingRuleEngineExclusionTests
{
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<ILogger<MeetingRuleEngine>> _loggerMock;
    private readonly MeetingRuleEngine _engine;
    private readonly DateTime _now;

    public MeetingRuleEngineExclusionTests()
    {
        _clockMock = new Mock<IClock>();
        _loggerMock = new Mock<ILogger<MeetingRuleEngine>>();
        _engine = new MeetingRuleEngine(_clockMock.Object, _loggerMock.Object);
        _now = DateTime.UtcNow;
        _clockMock.Setup(x => x.Now).Returns(_now);
    }

    [Fact]
    public void GivenExclusionMatchesProcessName_WhenEvaluating_ThenRuleDoesNotMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("ms-teams-old", PatternMatchMode.Contains, false)
            ],
            MatchCount = 0
        };

        var processes = new[]
        {
            new ProcessInfo("ms-teams-old", "Daily Meeting")
        };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenExclusionMatchesWindowTitle_WhenEvaluating_ThenRuleDoesNotMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Czat |", PatternMatchMode.StartsWith, false)
            ],
            MatchCount = 0
        };

        var processes = new[]
        {
            new ProcessInfo("ms-teams", "Czat | John Doe | Microsoft Teams")
        };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenExclusionMatchesEitherField_WhenBothFieldsMatch_ThenRuleDoesNotMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Chat", PatternMatchMode.Contains, false)
            ],
            MatchCount = 0
        };

        // Test exclusion matching process name
        var processes1 = new[]
        {
            new ProcessInfo("ms-teams-chat", "Daily Meeting | Microsoft Teams")
        };

        // Test exclusion matching window title
        var processes2 = new[]
        {
            new ProcessInfo("ms-teams", "Chat Window | Microsoft Teams")
        };

        // When
        var result1 = _engine.EvaluateRules([rule], processes1, null);
        var result2 = _engine.EvaluateRules([rule], processes2, null);

        // Then
        result1.Should().BeNull();
        result2.Should().BeNull();
    }

    [Fact]
    public void GivenMultipleExclusions_WhenAnyMatches_ThenRuleDoesNotMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Czat |", PatternMatchMode.StartsWith, false),
                PatternDefinition.CreateStringPattern("Aktywność |", PatternMatchMode.StartsWith, false),
                PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Exact, false)
            ],
            MatchCount = 0
        };

        var processes = new[]
        {
            new ProcessInfo("ms-teams", "Aktywność | Some Activity | Microsoft Teams")
        };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public void GivenExclusionDoesNotMatch_WhenInclusionMatches_ThenRuleMatches()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Chat", PatternMatchMode.Contains, false)
            ],
            MatchCount = 0
        };

        var processes = new[]
        {
            new ProcessInfo("ms-teams", "Daily Meeting")
        };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().NotBeNull();
        result!.ProcessName.Should().Be("ms-teams");
        result.WindowTitle.Should().Be("Daily Meeting");
    }

    [Fact]
    public void GivenExclusionWithRegex_WhenMatchesBothFields_ThenRuleDoesNotMatch()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Meeting", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateRegexPattern("^(Chat|Czat) \\|", caseSensitive: false)
            ],
            MatchCount = 0
        };

        var processes = new[]
        {
            new ProcessInfo("ms-teams", "Chat | User Name | Meeting")
        };

        // When
        var result = _engine.EvaluateRules([rule], processes, null);

        // Then
        result.Should().BeNull();
    }
}
