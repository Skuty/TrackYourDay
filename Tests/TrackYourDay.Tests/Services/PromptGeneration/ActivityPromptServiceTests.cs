using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.Services.PromptGeneration;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;
using Xunit;

namespace TrackYourDay.Tests.Services.PromptGeneration;

public sealed class ActivityPromptServiceTests
{
    private readonly Mock<IHistoricalDataRepository<EndedActivity>> _activityRepositoryMock = new();
    private readonly Mock<IJiraActivityProvider> _jiraActivityProviderMock = new();
    private readonly Mock<ILogger<ActivityPromptService>> _loggerMock = new();
    private readonly ActivityPromptService _sut;

    public ActivityPromptServiceTests()
    {
        _sut = new ActivityPromptService(
            _activityRepositoryMock.Object,
            _jiraActivityProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GivenSystemAndJiraActivities_WhenGettingActivities_ThenReturnsCombinedList()
    {
        // Given
        var date = new DateOnly(2026, 1, 3);
        var startDate = date.ToDateTime(TimeOnly.MinValue);

        var systemState = new FocusOnApplicationState("VS Code - Development");
        var systemActivities = new List<EndedActivity>
        {
            new EndedActivity(startDate.AddHours(9), startDate.AddHours(9).AddMinutes(30), systemState)
        };

        var jiraActivities = new List<JiraActivity>
        {
            new JiraActivity(
                startDate.AddHours(10),
                "PROJ-123: Implement feature")
        };

        _activityRepositoryMock.Setup(r => r.Find(It.IsAny<ActivityByDateSpecification>()))
            .Returns(systemActivities);

        _jiraActivityProviderMock.Setup(j => j.GetJiraActivities())
            .Returns(jiraActivities);

        // When
        var result = _sut.GetActivitiesForDate(date);

        // Then
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.ApplicationName == "VS Code");
        result.Should().Contain(a => a.ApplicationName == "Jira" && a.Title.Contains("PROJ-123"));
    }

    [Fact]
    public void GivenActivitiesShorterThan5Minutes_WhenGettingActivities_ThenExcludesThem()
    {
        // Given
        var date = new DateOnly(2026, 1, 3);
        var startDate = date.ToDateTime(TimeOnly.MinValue);

        var systemState1 = new FocusOnApplicationState("Chrome - Quick check");
        var systemState2 = new FocusOnApplicationState("VS Code - Main work");
        var systemActivities = new List<EndedActivity>
        {
            new EndedActivity(startDate.AddHours(9), startDate.AddHours(9).AddMinutes(2), systemState1),
            new EndedActivity(startDate.AddHours(10), startDate.AddHours(10).AddMinutes(30), systemState2)
        };

        _activityRepositoryMock.Setup(r => r.Find(It.IsAny<ActivityByDateSpecification>()))
            .Returns(systemActivities);

        _jiraActivityProviderMock.Setup(j => j.GetJiraActivities())
            .Returns(new List<JiraActivity>());

        // When
        var result = _sut.GetActivitiesForDate(date);

        // Then
        result.Should().ContainSingle();
        result.First().Duration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void GivenNoActivities_WhenGettingActivities_ThenReturnsEmptyList()
    {
        // Given
        var date = new DateOnly(2026, 1, 3);

        _activityRepositoryMock.Setup(r => r.Find(It.IsAny<ActivityByDateSpecification>()))
            .Returns(new List<EndedActivity>());

        _jiraActivityProviderMock.Setup(j => j.GetJiraActivities())
            .Returns(new List<JiraActivity>());

        // When
        var result = _sut.GetActivitiesForDate(date);

        // Then
        result.Should().BeEmpty();
    }
}
