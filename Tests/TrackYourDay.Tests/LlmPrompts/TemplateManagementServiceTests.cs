// Tests/TrackYourDay.Tests/LlmPrompts/TemplateManagementServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.LlmPrompts;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Tests.LlmPrompts;

public class TemplateManagementServiceTests
{
    [Fact]
    public void GivenValidTemplate_WhenSaving_ThenSavesToStore()
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();
        var sut = new TemplateManagementService(mockSettings.Object, mockLogger.Object);

        mockSettings.Setup(r => r.GetSetting(It.IsAny<string>())).Returns((string?)null);

        sut.SaveTemplate("test", "Test Template", "Content {JIRA_ACTIVITIES} more content that makes it 100+ characters long enough to pass validation!", 1);

        mockSettings.Verify(r => r.SetSetting(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockSettings.Verify(r => r.Save(), Times.Once);
    }

    [Theory]
    [InlineData("", "Name", "Content {JIRA_ACTIVITIES} more content that makes it 100+ characters long enough to pass validation", 1)]
    [InlineData("  ", "Name", "Content {GITLAB_ACTIVITIES} more content that makes it 100+ characters long enough to pass validation", 1)]
    public void GivenInvalidTemplateKey_WhenSaving_ThenThrowsArgumentException(string key, string name, string prompt, int order)
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var sut = new TemplateManagementService(mockSettings.Object, Mock.Of<ILogger<TemplateManagementService>>());

        var act = () => sut.SaveTemplate(key, name, prompt, order);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenPromptWithoutPlaceholder_WhenSaving_ThenSucceeds()
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();
        var sut = new TemplateManagementService(mockSettings.Object, mockLogger.Object);

        mockSettings.Setup(r => r.GetSetting(It.IsAny<string>())).Returns((string?)null);

        sut.SaveTemplate("test", "Test Template", "Content without placeholder but long enough to pass length validation of at least 100 characters required", 1);

        mockSettings.Verify(r => r.SetSetting(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockSettings.Verify(r => r.Save(), Times.Once);
    }

    [Fact]
    public void GivenValidPromptWithJiraPlaceholder_WhenGeneratingPreview_ThenReturnsPreviewWithSampleData()
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var sut = new TemplateManagementService(mockSettings.Object, Mock.Of<ILogger<TemplateManagementService>>());

        var result = sut.GeneratePreview("Analyze: {JIRA_ACTIVITIES}");

        result.Should().NotContain("{JIRA_ACTIVITIES}");
        result.Should().Contain("| Date");
        result.Should().Contain("Visual Studio Code");
    }
}
