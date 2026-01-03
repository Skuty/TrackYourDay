using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.Services.PromptGeneration;
using Xunit;

namespace TrackYourDay.Tests.Services.PromptGeneration;

public sealed class PromptGeneratorServiceTests
{
    private readonly Mock<IPromptTemplateProvider> _templateProviderMock = new();
    private readonly Mock<IActivityPromptService> _activityServiceMock = new();
    private readonly Mock<ILogger<PromptGeneratorService>> _loggerMock = new();
    private readonly PromptGeneratorService _sut;

    public PromptGeneratorServiceTests()
    {
        _sut = new PromptGeneratorService(
            _templateProviderMock.Object,
            _activityServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GivenActivitiesWithJiraKeys_WhenGeneratingPrompt_ThenSubstitutesPlaceholders()
    {
        // Given
        var date = new DateOnly(2026, 1, 3);
        var activities = new[]
        {
            new ActivitySummaryDto("JIRA-123: Implementation", "Jira", TimeSpan.FromHours(9), TimeSpan.FromHours(2)),
            new ActivitySummaryDto("Email review", "Outlook", TimeSpan.FromHours(11), TimeSpan.FromMinutes(30))
        };

        var template = "Date: {DATE}\nActivities:\n{ACTIVITY_LIST}\nJira: {JIRA_KEYS}";
        _templateProviderMock.Setup(p => p.GetTemplate(PromptTemplate.DetailedSummaryWithTimeAllocation))
            .Returns(template);

        _activityServiceMock.Setup(s => s.GetActivitiesForDate(date))
            .Returns(activities);

        // When
        var result = _sut.GeneratePrompt(date, PromptTemplate.DetailedSummaryWithTimeAllocation);

        // Then
        result.Should().Contain("Date: 2026-01-03");
        result.Should().Contain("JIRA-123");
        result.Should().Contain("09:00 | 2h 0m | Jira | JIRA-123: Implementation");
        result.Should().Contain("11:00 | 30m | Outlook | Email review");
    }

    [Fact]
    public void GivenNoActivities_WhenGeneratingPrompt_ThenReturnsNoActivitiesMessage()
    {
        // Given
        var date = new DateOnly(2026, 1, 3);
        var activities = Array.Empty<ActivitySummaryDto>();
        var template = "Activities:\n{ACTIVITY_LIST}";
        
        _templateProviderMock.Setup(p => p.GetTemplate(It.IsAny<PromptTemplate>()))
            .Returns(template);

        _activityServiceMock.Setup(s => s.GetActivitiesForDate(date))
            .Returns(activities);

        // When
        var result = _sut.GeneratePrompt(date, PromptTemplate.ConciseBulletPointSummary);

        // Then
        result.Should().Contain("(No activities recorded)");
    }

    [Fact]
    public void GivenNoJiraKeys_WhenGeneratingPrompt_ThenIncludesNoTicketsMessage()
    {
        // Given
        var date = new DateOnly(2026, 1, 3);
        var activities = new[]
        {
            new ActivitySummaryDto("General development", "VS Code", TimeSpan.FromHours(9), TimeSpan.FromHours(1))
        };

        var template = "Jira: {JIRA_KEYS}";
        _templateProviderMock.Setup(p => p.GetTemplate(It.IsAny<PromptTemplate>()))
            .Returns(template);

        _activityServiceMock.Setup(s => s.GetActivitiesForDate(date))
            .Returns(activities);

        // When
        var result = _sut.GeneratePrompt(date, PromptTemplate.ConciseBulletPointSummary);

        // Then
        result.Should().Contain("No Jira tickets detected");
    }
}
