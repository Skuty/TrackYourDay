using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings.Persistence;

[Trait("Category", "Unit")]
public class MeetingRuleRepositoryConcurrencyTests
{
    private readonly Mock<IGenericSettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<MeetingRuleRepository>> _loggerMock;
    private readonly MeetingRuleRepository _repository;

    public MeetingRuleRepositoryConcurrencyTests()
    {
        _settingsServiceMock = new Mock<IGenericSettingsService>();
        _loggerMock = new Mock<ILogger<MeetingRuleRepository>>();
        _repository = new MeetingRuleRepository(_settingsServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GivenConcurrentIncrementCalls_WhenFlushOccurs_ThenAllUpdatesArePersisted()
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

        _settingsServiceMock
            .Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns([rule]);

        var matchTime = DateTime.UtcNow;

        // When - simulate 5 concurrent increments
        Parallel.For(0, 5, _ =>
        {
            _repository.IncrementMatchCount(rule.Id, matchTime);
        });

        // Force flush by disposing
        _repository.Dispose();

        // Then
        _settingsServiceMock.Verify(
            x => x.SetSetting(It.IsAny<string>(), It.Is<List<MeetingRecognitionRule>>(rules =>
                rules.Count == 1 && rules[0].MatchCount == 5)),
            Times.Once());
    }

    [Fact]
    public void GivenConcurrentSaveAndIncrement_WhenExecuted_ThenNoExceptionIsThrown()
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

        _settingsServiceMock
            .Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns([rule]);

        // When - concurrent save and increment
        var saveTask = Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var updatedRule = rule with { Priority = i + 1 };
                _repository.SaveRules([updatedRule]);
                Thread.Sleep(10);
            }
        });

        var incrementTask = Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                _repository.IncrementMatchCount(rule.Id, DateTime.UtcNow);
                Thread.Sleep(10);
            }
        });

        // Then
        var act = async () => await Task.WhenAll(saveTask, incrementTask);
        act.Should().NotThrowAsync();
    }

    [Fact]
    public void GivenRuleCachingEnabled_WhenGetAllRulesCalledTwiceWithin5Seconds_ThenSettingsServiceCalledOnce()
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

        _settingsServiceMock
            .Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns([rule]);

        // When
        var result1 = _repository.GetAllRules();
        var result2 = _repository.GetAllRules();

        // Then
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
        _settingsServiceMock.Verify(
            x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()),
            Times.Once());
    }

    [Fact]
    public void GivenSaveRulesWithDuplicateIds_WhenCalled_ThenThrowsArgumentException()
    {
        // Given
        var sharedId = Guid.NewGuid();
        var rules = new List<MeetingRecognitionRule>
        {
            new()
            {
                Id = sharedId,
                Priority = 1,
                Criteria = MatchingCriteria.ProcessNameOnly,
                ProcessNamePattern = PatternDefinition.CreateStringPattern("test1", PatternMatchMode.Contains, false),
                MatchCount = 0
            },
            new()
            {
                Id = sharedId, // Duplicate ID
                Priority = 2,
                Criteria = MatchingCriteria.ProcessNameOnly,
                ProcessNamePattern = PatternDefinition.CreateStringPattern("test2", PatternMatchMode.Contains, false),
                MatchCount = 0
            }
        };

        // When
        var act = () => _repository.SaveRules(rules);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Rule IDs must be unique*");
    }

    [Fact]
    public void GivenRepositoryDisposed_WhenFlushPending_ThenAllUpdatesArePersisted()
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

        _settingsServiceMock
            .Setup(x => x.GetSetting<List<MeetingRecognitionRule>>(It.IsAny<string>(), It.IsAny<List<MeetingRecognitionRule>>()))
            .Returns([rule]);

        _repository.IncrementMatchCount(rule.Id, DateTime.UtcNow);

        // When
        _repository.Dispose();

        // Then
        _settingsServiceMock.Verify(
            x => x.SetSetting(It.IsAny<string>(), It.Is<List<MeetingRecognitionRule>>(rules =>
                rules[0].MatchCount == 1)),
            Times.Once());
    }

    [Fact]
    public void GivenDisposedRepository_WhenDisposeCalledAgain_ThenNoExceptionThrown()
    {
        // Given
        _repository.Dispose();

        // When
        var act = () => _repository.Dispose();

        // Then
        act.Should().NotThrow();
    }
}
