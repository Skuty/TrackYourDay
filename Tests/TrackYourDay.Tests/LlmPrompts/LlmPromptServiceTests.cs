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
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;
using Newtonsoft.Json;

namespace TrackYourDay.Tests.LlmPrompts;

public class LlmPromptServiceTests
{
    [Fact]
    public void GivenValidTemplate_WhenGeneratingPrompt_ThenReturnsPromptWithActivityData()
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockActivityRepo = new Mock<IHistoricalDataRepository<EndedActivity>>();
        var mockMeetingRepo = new Mock<IHistoricalDataRepository<EndedMeeting>>();
        var userTaskService = new UserTaskService();
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test", Name = "Test Template",
            SystemPrompt = "Analyze: {ACTIVITY_DATA_PLACEHOLDER}", IsActive = true, DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var sut = new LlmPromptService(mockSettings.Object, mockActivityRepo.Object, mockMeetingRepo.Object, userTaskService, strategy, Mock.Of<ILogger<LlmPromptService>>());

        var endedActivity = new EndedActivity(DateTime.Today.AddHours(9), DateTime.Today.AddHours(10), new FocusOnApplicationState("Test Activity"));
        mockActivityRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedActivity>>())).Returns(new[] { endedActivity });
        mockMeetingRepo.Setup(r => r.Find(It.IsAny<ISpecification<EndedMeeting>>())).Returns(Array.Empty<EndedMeeting>());

        var result = sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        result.Should().NotBeNullOrEmpty();
        result.Should().NotContain("{ACTIVITY_DATA_PLACEHOLDER}");
        result.Should().Contain("Test Activity");
    }

    [Fact]
    public void GivenNonExistentTemplate_WhenGeneratingPrompt_ThenThrowsInvalidOperationException()
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        mockSettings.Setup(r => r.GetSetting(It.IsAny<string>())).Returns((string?)null);
        var strategy = new ActivityNameSummaryStrategy(Mock.Of<ILogger<ActivityNameSummaryStrategy>>());
        var sut = new LlmPromptService(mockSettings.Object, Mock.Of<IHistoricalDataRepository<EndedActivity>>(), Mock.Of<IHistoricalDataRepository<EndedMeeting>>(),
            new UserTaskService(), strategy, Mock.Of<ILogger<LlmPromptService>>());

        var act = () => sut.GeneratePrompt("nonexistent", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }
}
