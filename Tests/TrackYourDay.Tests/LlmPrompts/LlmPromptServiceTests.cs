// Tests/TrackYourDay.Tests/LlmPrompts/LlmPromptServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.LlmPrompts;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.LlmPrompts;

public class LlmPromptServiceTests
{
    [Fact]
    public void GivenValidTemplate_WhenGeneratingPrompt_ThenReturnsPromptWithActivityData()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var userTaskService = new UserTaskService();
        var mockStrategyLogger = new Mock<ILogger<ActivityNameSummaryStrategy>>();
        var strategy = new ActivityNameSummaryStrategy(mockStrategyLogger.Object);
        var mockLogger = new Mock<ILogger<LlmPromptService>>();

        var template = new LlmPromptTemplate
        {
            Id = 1,
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockRepository.Setup(r => r.GetByKey("test")).Returns(template);

        var endedActivity = new EndedActivity(
            DateTime.Today.AddHours(9),
            DateTime.Today.AddHours(10),
            new FocusOnApplicationState("Test Activity"));

        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>()))
            .Returns(new[] { endedActivity });
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>()))
            .Returns(Array.Empty<EndedMeeting>());

        var sut = new LlmPromptService(
            mockRepository.Object,
            mockActivityRepo.Object,
            mockMeetingRepo.Object,
            userTaskService,
            strategy,
            mockLogger.Object);

        // When
        var result = sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().NotBeNullOrEmpty();
        result.Should().NotContain("{ACTIVITY_DATA_PLACEHOLDER}");
        result.Should().Contain("Test Activity");
        result.Should().Contain("| Date");
    }

    [Fact]
    public void GivenNonExistentTemplate_WhenGeneratingPrompt_ThenThrowsInvalidOperationException()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var userTaskService = new UserTaskService();
        var mockStrategyLogger = new Mock<ILogger<ActivityNameSummaryStrategy>>();
        var strategy = new ActivityNameSummaryStrategy(mockStrategyLogger.Object);
        var mockLogger = new Mock<ILogger<LlmPromptService>>();

        mockRepository.Setup(r => r.GetByKey("nonexistent")).Returns((LlmPromptTemplate?)null);

        var sut = new LlmPromptService(
            mockRepository.Object,
            mockActivityRepo.Object,
            mockMeetingRepo.Object,
            userTaskService,
            strategy,
            mockLogger.Object);

        // When
        var act = () => sut.GeneratePrompt("nonexistent", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void GivenNoActivities_WhenGeneratingPrompt_ThenThrowsInvalidOperationException()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var userTaskService = new UserTaskService();
        var mockStrategyLogger = new Mock<ILogger<ActivityNameSummaryStrategy>>();
        var strategy = new ActivityNameSummaryStrategy(mockStrategyLogger.Object);
        var mockLogger = new Mock<ILogger<LlmPromptService>>();

        var template = new LlmPromptTemplate
        {
            Id = 1,
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockRepository.Setup(r => r.GetByKey("test")).Returns(template);
        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>()))
            .Returns(Array.Empty<EndedActivity>());
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>()))
            .Returns(Array.Empty<EndedMeeting>());

        var sut = new LlmPromptService(
            mockRepository.Object,
            mockActivityRepo.Object,
            mockMeetingRepo.Object,
            userTaskService,
            strategy,
            mockLogger.Object);

        // When
        var act = () => sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No activities found*");
    }

    [Fact]
    public void GivenStartDateAfterEndDate_WhenGeneratingPrompt_ThenThrowsArgumentException()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var userTaskService = new UserTaskService();
        var mockStrategyLogger = new Mock<ILogger<ActivityNameSummaryStrategy>>();
        var strategy = new ActivityNameSummaryStrategy(mockStrategyLogger.Object);
        var mockLogger = new Mock<ILogger<LlmPromptService>>();

        var sut = new LlmPromptService(
            mockRepository.Object,
            mockActivityRepo.Object,
            mockMeetingRepo.Object,
            userTaskService,
            strategy,
            mockLogger.Object);

        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(-1);

        // When
        var act = () => sut.GeneratePrompt("test", start, end);

        // Then
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenTemplateWithoutPlaceholder_WhenGeneratingPrompt_ThenThrowsInvalidOperationException()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var userTaskService = new UserTaskService();
        var mockStrategyLogger = new Mock<ILogger<ActivityNameSummaryStrategy>>();
        var strategy = new ActivityNameSummaryStrategy(mockStrategyLogger.Object);
        var mockLogger = new Mock<ILogger<LlmPromptService>>();

        var template = new LlmPromptTemplate
        {
            Id = 1,
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "No placeholder here",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockRepository.Setup(r => r.GetByKey("test")).Returns(template);

        var sut = new LlmPromptService(
            mockRepository.Object,
            mockActivityRepo.Object,
            mockMeetingRepo.Object,
            userTaskService,
            strategy,
            mockLogger.Object);

        // When
        var act = () => sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing placeholder*");
    }

    [Fact]
    public void WhenGettingActiveTemplates_ThenDelegatesToRepository()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var userTaskService = new UserTaskService();
        var mockStrategyLogger = new Mock<ILogger<ActivityNameSummaryStrategy>>();
        var strategy = new ActivityNameSummaryStrategy(mockStrategyLogger.Object);
        var mockLogger = new Mock<ILogger<LlmPromptService>>();

        var templates = new List<LlmPromptTemplate>
        {
            new LlmPromptTemplate
            {
                Id = 1,
                TemplateKey = "test",
                Name = "Test",
                SystemPrompt = "Test",
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        mockRepository.Setup(r => r.GetActiveTemplates()).Returns(templates);

        var sut = new LlmPromptService(
            mockRepository.Object,
            mockActivityRepo.Object,
            mockMeetingRepo.Object,
            userTaskService,
            strategy,
            mockLogger.Object);

        // When
        var result = sut.GetActiveTemplates();

        // Then
        result.Should().HaveCount(1);
        result[0].TemplateKey.Should().Be("test");
    }
}
