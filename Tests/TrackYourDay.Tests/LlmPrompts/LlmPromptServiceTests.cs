// Tests/TrackYourDay.Tests/LlmPrompts/LlmPromptServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.LlmPrompts;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;
using Newtonsoft.Json;

namespace TrackYourDay.Tests.LlmPrompts;

public class LlmPromptServiceTests
{
    [Fact]
    public async Task GivenValidTemplate_WhenGeneratingPrompt_ThenReturnsPromptWithActivityData()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var mockJiraService = new Mock<IJiraActivityService>();
        var userTaskService = new UserTaskService();
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test", Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}", IsActive = true, DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));
        mockJiraService.Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>())).ReturnsAsync([]);

        var sut = new LlmPromptService(mockSettings.Object, mockActivityRepo.Object, mockMeetingRepo.Object, 
            userTaskService, strategy, mockJiraService.Object, Mock.Of<ILogger<LlmPromptService>>());

        var endedActivity = new EndedActivity(DateTime.Today.AddHours(9), DateTime.Today.AddHours(10), 
            new FocusOnApplicationState("Test Activity"));
        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>())).Returns(new[] { endedActivity });
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>())).Returns(Array.Empty<EndedMeeting>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().NotBeNullOrEmpty();
        result.Should().NotContain("{ACTIVITY_DATA_PLACEHOLDER}");
        result.Should().Contain("Test Activity");
    }

    [Fact]
    public async Task GivenNonExistentTemplate_WhenGeneratingPrompt_ThenThrowsInvalidOperationException()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        mockSettings.Setup(r => r.GetSetting(It.IsAny<string>())).Returns((string?)null);
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());
        var mockJiraService = new Mock<IJiraActivityService>();
        
        var sut = new LlmPromptService(mockSettings.Object, Mock.Of<IHistoricalDataRepository<EndedActivity>>(), 
            Mock.Of<IHistoricalDataRepository<EndedMeeting>>(), new UserTaskService(), strategy, 
            mockJiraService.Object, Mock.Of<ILogger<LlmPromptService>>());

        // When
        var act = async () => await sut.GeneratePrompt("nonexistent", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task GivenJiraIssuesExist_WhenGeneratingPrompt_ThenIncludesJiraIssues()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var mockJiraService = new Mock<IJiraActivityService>();
        var userTaskService = new UserTaskService();
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test", Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}", IsActive = true, DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var jiraActivities = new List<JiraActivity>
        {
            new(DateTime.Today.AddHours(9), "Created Issue PROJ-123 in Project: Implement feature"),
            new(DateTime.Today.AddHours(14), "Logged 2h on Issue PROJ-456 in Project: Bug fix")
        };
        mockJiraService.Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>())).ReturnsAsync(jiraActivities);

        var sut = new LlmPromptService(mockSettings.Object, mockActivityRepo.Object, mockMeetingRepo.Object,
            userTaskService, strategy, mockJiraService.Object, Mock.Of<ILogger<LlmPromptService>>());

        var endedActivity = new EndedActivity(DateTime.Today.AddHours(9), DateTime.Today.AddHours(10),
            new FocusOnApplicationState("Test Activity"));
        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>())).Returns(new[] { endedActivity });
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>())).Returns(Array.Empty<EndedMeeting>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Contain("Related Jira Issues");
        result.Should().Contain("PROJ-123");
        result.Should().Contain("PROJ-456");
        result.Should().Contain("Implement feature");
        result.Should().Contain("Bug fix");
    }

    [Fact]
    public async Task GivenNoJiraIssuesExist_WhenGeneratingPrompt_ThenExcludesJiraSection()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var mockJiraService = new Mock<IJiraActivityService>();
        var userTaskService = new UserTaskService();
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test", Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}", IsActive = true, DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));
        mockJiraService.Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>())).ReturnsAsync([]);

        var sut = new LlmPromptService(mockSettings.Object, mockActivityRepo.Object, mockMeetingRepo.Object,
            userTaskService, strategy, mockJiraService.Object, Mock.Of<ILogger<LlmPromptService>>());

        var endedActivity = new EndedActivity(DateTime.Today.AddHours(9), DateTime.Today.AddHours(10),
            new FocusOnApplicationState("Test Activity"));
        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>())).Returns(new[] { endedActivity });
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>())).Returns(Array.Empty<EndedMeeting>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().NotContain("Related Jira Issues");
    }

    [Fact]
    public async Task GivenJiraServiceThrowsException_WhenGeneratingPrompt_ThenContinuesWithoutJiraIssues()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var mockJiraService = new Mock<IJiraActivityService>();
        var userTaskService = new UserTaskService();
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test", Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}", IsActive = true, DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));
        mockJiraService.Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>()))
            .Throws(new Exception("Jira connection failed"));

        var sut = new LlmPromptService(mockSettings.Object, mockActivityRepo.Object, mockMeetingRepo.Object,
            userTaskService, strategy, mockJiraService.Object, Mock.Of<ILogger<LlmPromptService>>());

        var endedActivity = new EndedActivity(DateTime.Today.AddHours(9), DateTime.Today.AddHours(10),
            new FocusOnApplicationState("Test Activity"));
        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>())).Returns(new[] { endedActivity });
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>())).Returns(Array.Empty<EndedMeeting>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Test Activity");
        result.Should().NotContain("Related Jira Issues");
    }

    [Fact]
    public async Task GivenJiraIssuesOutsideDateRange_WhenGeneratingPrompt_ThenFiltersOutThoseIssues()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var mockJiraService = new Mock<IJiraActivityService>();
        var userTaskService = new UserTaskService();
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test", Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}", IsActive = true, DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var jiraActivities = new List<JiraActivity>
        {
            new(DateTime.Today.AddDays(-7), "Old issue PROJ-999 from last week"),
            new(DateTime.Today.AddHours(9), "Created Issue PROJ-123 in Project: Implement feature"),
            new(DateTime.Today.AddDays(7), "Future issue PROJ-888 next week")
        };
        mockJiraService.Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>())).ReturnsAsync(jiraActivities);

        var sut = new LlmPromptService(mockSettings.Object, mockActivityRepo.Object, mockMeetingRepo.Object,
            userTaskService, strategy, mockJiraService.Object, Mock.Of<ILogger<LlmPromptService>>());

        var endedActivity = new EndedActivity(DateTime.Today.AddHours(9), DateTime.Today.AddHours(10),
            new FocusOnApplicationState("Test Activity"));
        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>())).Returns(new[] { endedActivity });
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>())).Returns(Array.Empty<EndedMeeting>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Contain("PROJ-123");
        result.Should().NotContain("PROJ-999");
        result.Should().NotContain("PROJ-888");
    }
}
