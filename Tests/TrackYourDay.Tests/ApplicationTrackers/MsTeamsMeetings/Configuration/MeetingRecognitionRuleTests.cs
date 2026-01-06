using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings.Configuration;

[Trait("Category", "Unit")]
public class MeetingRecognitionRuleTests
{
    [Fact]
    public void GivenValidRule_WhenValidating_ThenNoExceptionThrown()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            Exclusions = [],
            MatchCount = 0,
            LastMatchedAt = null
        };

        // When
        var act = () => rule.Validate();

        // Then
        act.Should().NotThrow();
    }

    [Fact]
    public void GivenPriorityZero_WhenValidating_ThenThrowsArgumentException()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 0,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = 0
        };

        // When
        var act = () => rule.Validate();

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Priority must be >= 1*");
    }

    [Fact]
    public void GivenProcessNameOnlyWithoutPattern_WhenValidating_ThenThrowsArgumentException()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = null,
            MatchCount = 0
        };

        // When
        var act = () => rule.Validate();

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ProcessNamePattern required for ProcessNameOnly criteria*");
    }

    [Fact]
    public void GivenWindowTitleOnlyWithoutPattern_WhenValidating_ThenThrowsArgumentException()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = null,
            MatchCount = 0
        };

        // When
        var act = () => rule.Validate();

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*WindowTitlePattern required for WindowTitleOnly criteria*");
    }

    [Fact]
    public void GivenBothCriteriaWithMissingProcessName_WhenValidating_ThenThrowsArgumentException()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = null,
            WindowTitlePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = 0
        };

        // When
        var act = () => rule.Validate();

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Both patterns required for Both criteria*");
    }

    [Fact]
    public void GivenNegativeMatchCount_WhenValidating_ThenThrowsArgumentException()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = -1
        };

        // When
        var act = () => rule.Validate();

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MatchCount cannot be negative*");
    }

    [Fact]
    public void WhenIncrementingMatchCount_ThenReturnsNewRuleWithUpdatedValues()
    {
        // Given
        var originalRule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = 5,
            LastMatchedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var matchedAt = DateTime.UtcNow;

        // When
        var newRule = originalRule.IncrementMatchCount(matchedAt);

        // Then
        newRule.MatchCount.Should().Be(6);
        newRule.LastMatchedAt.Should().Be(matchedAt);
        newRule.Id.Should().Be(originalRule.Id);
        originalRule.MatchCount.Should().Be(5);
    }

    [Fact]
    public void GivenRuleWithExclusions_WhenValidating_ThenNoExceptionThrown()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Teams", PatternMatchMode.Contains, false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Chat", PatternMatchMode.StartsWith, false),
                PatternDefinition.CreateStringPattern("Activity", PatternMatchMode.StartsWith, false)
            ],
            MatchCount = 0
        };

        // When
        var act = () => rule.Validate();

        // Then
        act.Should().NotThrow();
    }
}
