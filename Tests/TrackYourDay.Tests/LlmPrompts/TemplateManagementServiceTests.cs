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
        var mockLogger = new Mock<ILogger<LlmPromptTemplateStore>>();
        var store = new LlmPromptTemplateStore(mockSettings.Object, mockLogger.Object);
        var sut = new TemplateManagementService(store, Mock.Of<ILogger<TemplateManagementService>>());

        mockSettings.Setup(r => r.GetSetting(It.IsAny<string>())).Returns((string?)null);

        sut.SaveTemplate("test", "Test Template", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1);

        mockSettings.Verify(r => r.SetSetting(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockSettings.Verify(r => r.Save(), Times.Once);
    }

    [Theory]
    [InlineData("", "Name", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1)]
    [InlineData("  ", "Name", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1)]
    public void GivenInvalidTemplateKey_WhenSaving_ThenThrowsArgumentException(string key, string name, string prompt, int order)
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var store = new LlmPromptTemplateStore(mockSettings.Object, Mock.Of<ILogger<LlmPromptTemplateStore>>());
        var sut = new TemplateManagementService(store, Mock.Of<ILogger<TemplateManagementService>>());

        var act = () => sut.SaveTemplate(key, name, prompt, order);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenPromptWithoutPlaceholder_WhenSaving_ThenThrowsArgumentException()
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var store = new LlmPromptTemplateStore(mockSettings.Object, Mock.Of<ILogger<LlmPromptTemplateStore>>());
        var sut = new TemplateManagementService(store, Mock.Of<ILogger<TemplateManagementService>>());

        var act = () => sut.SaveTemplate("test", "Test Template", "Content without placeholder but long enough to pass length validation of at least 100 characters required", 1);

        act.Should().Throw<ArgumentException>().WithMessage("*must contain {ACTIVITY_DATA_PLACEHOLDER}*");
    }

    [Fact]
    public void GivenValidPrompt_WhenGeneratingPreview_ThenReturnsPreviewWithSampleData()
    {
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var store = new LlmPromptTemplateStore(mockSettings.Object, Mock.Of<ILogger<LlmPromptTemplateStore>>());
        var sut = new TemplateManagementService(store, Mock.Of<ILogger<TemplateManagementService>>());

        var result = sut.GeneratePreview("Analyze: {ACTIVITY_DATA_PLACEHOLDER}");

        result.Should().NotContain("{ACTIVITY_DATA_PLACEHOLDER}");
        result.Should().Contain("| Date");
        result.Should().Contain("Visual Studio Code");
    }
}
