using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings.Persistence;

[Trait("Category", "Unit")]
public class MeetingRuleRepositoryTests
{
    private readonly Mock<IGenericSettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<MeetingRuleRepository>> _loggerMock;
    private readonly MeetingRuleRepository _repository;

    public MeetingRuleRepositoryTests()
    {
        _settingsServiceMock = new Mock<IGenericSettingsService>();
        _loggerMock = new Mock<ILogger<MeetingRuleRepository>>();
        _repository = new MeetingRuleRepository(_settingsServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void WhenNoRulesExist_ThenReturnsDefaultRule()
    {
        // Given
        _settingsServiceMock.Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns((List<MeetingRecognitionRule>?)null);

        // When
        var rules = _repository.GetAllRules();

        // Then
        rules.Should().HaveCount(1);
        rules[0].Priority.Should().Be(1);
        rules[0].Criteria.Should().Be(MatchingCriteria.Both);
    }

    [Fact]
    public void WhenEmptyRulesList_ThenReturnsDefaultRule()
    {
        // Given
        _settingsServiceMock.Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns(new List<MeetingRecognitionRule>());

        // When
        var rules = _repository.GetAllRules();

        // Then
        rules.Should().HaveCount(1);
    }

    [Fact]
    public void WhenRulesExist_ThenReturnsSortedByPriority()
    {
        // Given
        var rule1 = CreateTestRule(priority: 3);
        var rule2 = CreateTestRule(priority: 1);
        var rule3 = CreateTestRule(priority: 2);
        _settingsServiceMock.Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns(new List<MeetingRecognitionRule> { rule1, rule2, rule3 });

        // When
        var rules = _repository.GetAllRules();

        // Then
        rules.Should().HaveCount(3);
        rules[0].Priority.Should().Be(1);
        rules[1].Priority.Should().Be(2);
        rules[2].Priority.Should().Be(3);
    }

    [Fact]
    public void GivenValidRules_WhenSaving_ThenPersistsToSettings()
    {
        // Given
        var rules = new List<MeetingRecognitionRule> { CreateTestRule(priority: 1) };

        // When
        _repository.SaveRules(rules);

        // Then
        _settingsServiceMock.Verify(x => x.SetSetting("MeetingRecognitionRules.v1", It.IsAny<List<MeetingRecognitionRule>>()), Times.Once);
        _settingsServiceMock.Verify(x => x.PersistSettings(), Times.Once);
    }

    [Fact]
    public void GivenNullRules_WhenSaving_ThenThrowsArgumentNullException()
    {
        // When
        var act = () => _repository.SaveRules(null!);

        // Then
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenDuplicatePriorities_WhenSaving_ThenThrowsArgumentException()
    {
        // Given
        var rule1 = CreateTestRule(priority: 1);
        var rule2 = CreateTestRule(priority: 1);

        // When
        var act = () => _repository.SaveRules([rule1, rule2]);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*priorities must be unique*");
    }

    [Fact]
    public void GivenInvalidRule_WhenSaving_ThenThrowsArgumentException()
    {
        // Given
        var invalidRule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 0, // Invalid
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = 0
        };

        // When
        var act = () => _repository.SaveRules([invalidRule]);

        // Then
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenIncrementingMatchCount_ThenUpdatesRuleAndSaves()
    {
        // Given
        var ruleId = Guid.NewGuid();
        var rule = new MeetingRecognitionRule
        {
            Id = ruleId,
            Priority = 1,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = 5,
            LastMatchedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _settingsServiceMock.Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns(new List<MeetingRecognitionRule> { rule });
        var matchedAt = DateTime.UtcNow;
        
        List<MeetingRecognitionRule>? capturedRules = null;
        _settingsServiceMock.Setup(x => x.SetSetting(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Callback<string, List<MeetingRecognitionRule>>((_, rules) => capturedRules = rules);

        // When
        _repository.IncrementMatchCount(ruleId, matchedAt);
        _repository.Dispose();

        // Then
        _settingsServiceMock.Verify(x => x.SetSetting("MeetingRecognitionRules.v1", It.IsAny<List<MeetingRecognitionRule>>()), Times.Once);
        capturedRules.Should().NotBeNull();
        capturedRules![0].MatchCount.Should().Be(6);
        capturedRules[0].LastMatchedAt.Should().Be(matchedAt);
    }

    [Fact]
    public void GivenNonExistentRuleId_WhenIncrementingMatchCount_ThenLogsWarning()
    {
        // Given
        var rules = new List<MeetingRecognitionRule> { CreateTestRule(priority: 1) };
        _settingsServiceMock.Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns(rules);
        var nonExistentId = Guid.NewGuid();

        // When
        _repository.IncrementMatchCount(nonExistentId, DateTime.UtcNow);

        // Then
        _settingsServiceMock.Verify(x => x.SetSetting(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void WhenCreatingDefaultRule_ThenReturnsRuleWithCorrectConfiguration()
    {
        // When
        var rule = _repository.CreateDefaultRule();

        // Then
        rule.Priority.Should().Be(1);
        rule.Criteria.Should().Be(MatchingCriteria.Both);
        rule.ProcessNamePattern.Should().NotBeNull();
        rule.ProcessNamePattern!.Pattern.Should().Be("ms-teams");
        rule.ProcessNamePattern.MatchMode.Should().Be(PatternMatchMode.Contains);
        rule.ProcessNamePattern.CaseSensitive.Should().BeFalse();
        
        rule.WindowTitlePattern.Should().NotBeNull();
        rule.WindowTitlePattern!.Pattern.Should().Be("Microsoft Teams");
        rule.WindowTitlePattern.MatchMode.Should().Be(PatternMatchMode.Contains);
        rule.WindowTitlePattern.CaseSensitive.Should().BeFalse();
        
        rule.Exclusions.Should().HaveCount(3);
        rule.Exclusions[0].Pattern.Should().Be("Czat |");
        rule.Exclusions[0].MatchMode.Should().Be(PatternMatchMode.StartsWith);
        rule.Exclusions[1].Pattern.Should().Be("Aktywność |");
        rule.Exclusions[1].MatchMode.Should().Be(PatternMatchMode.StartsWith);
        rule.Exclusions[2].Pattern.Should().Be("Microsoft Teams");
        rule.Exclusions[2].MatchMode.Should().Be(PatternMatchMode.Exact);
        
        rule.MatchCount.Should().Be(0);
        rule.LastMatchedAt.Should().BeNull();
    }

    [Fact]
    public void GivenCorruptedSettings_WhenGettingRules_ThenReturnsDefaultRule()
    {
        // Given
        _settingsServiceMock.Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Throws(new InvalidOperationException("Corrupted JSON"));

        // When
        var rules = _repository.GetAllRules();

        // Then
        rules.Should().HaveCount(1);
        rules[0].Priority.Should().Be(1);
    }

    [Fact]
    public void GivenRegexPattern_WhenLoading_ThenRecompilesRegex()
    {
        // Given
        var rule = new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.WindowTitleOnly,
            WindowTitlePattern = new PatternDefinition
            {
                Pattern = @"Meeting \d+",
                MatchMode = PatternMatchMode.Regex,
                CaseSensitive = false,
                CompiledRegex = null // Simulates deserialization
            },
            MatchCount = 0
        };
        _settingsServiceMock.Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns(new List<MeetingRecognitionRule> { rule });

        // When
        var rules = _repository.GetAllRules();

        // Then
        rules[0].WindowTitlePattern.Should().NotBeNull();
        rules[0].WindowTitlePattern!.CompiledRegex.Should().NotBeNull();
    }

    private MeetingRecognitionRule CreateTestRule(int priority)
    {
        return new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = priority,
            Criteria = MatchingCriteria.ProcessNameOnly,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("test", PatternMatchMode.Contains, false),
            MatchCount = 0
        };
    }
}
