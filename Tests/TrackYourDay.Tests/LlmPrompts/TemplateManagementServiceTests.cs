// Tests/TrackYourDay.Tests/LlmPrompts/TemplateManagementServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.LlmPrompts;

namespace TrackYourDay.Tests.LlmPrompts;

public class TemplateManagementServiceTests
{
    [Fact]
    public void GivenValidTemplate_WhenSaving_ThenSavesToRepository()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        mockRepository.Setup(r => r.GetByKey("test")).Returns((LlmPromptTemplate?)null);

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        sut.SaveTemplate("test", "Test Template", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1);

        // Then
        mockRepository.Verify(r => r.Save(It.Is<LlmPromptTemplate>(
            t => t.TemplateKey == "test" && t.Name == "Test Template")), Times.Once);
    }

    [Theory]
    [InlineData("", "Name", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1)]
    [InlineData("  ", "Name", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1)]
    public void GivenInvalidTemplateKey_WhenSaving_ThenThrowsArgumentException(
        string key, string name, string prompt, int order)
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var act = () => sut.SaveTemplate(key, name, prompt, order);

        // Then
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("test", "test", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1)]
    [InlineData("test", "abcd", "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation", 1)]
    public void GivenInvalidName_WhenSaving_ThenThrowsArgumentException(
        string key, string name, string prompt, int order)
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var act = () => sut.SaveTemplate(key, name, prompt, order);

        // Then
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenPromptWithoutPlaceholder_WhenSaving_ThenThrowsArgumentException()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var act = () => sut.SaveTemplate(
            "test", 
            "Test Template", 
            "Content without placeholder but long enough to pass length validation of at least 100 characters required by the validation rules",
            1);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must contain {ACTIVITY_DATA_PLACEHOLDER}*");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("a")]
    public void GivenPromptTooShort_WhenSaving_ThenThrowsArgumentException(string prompt)
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var act = () => sut.SaveTemplate("test", "Test Template", prompt, 1);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Prompt must be 100-10,000 characters*");
    }

    [Fact]
    public void GivenNegativeDisplayOrder_WhenSaving_ThenThrowsArgumentException()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var act = () => sut.SaveTemplate(
            "test", 
            "Test Template", 
            "Content {ACTIVITY_DATA_PLACEHOLDER} more content that makes it 100+ characters long enough to pass validation",
            0);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Display order must be positive*");
    }

    [Fact]
    public void GivenLastActiveTemplate_WhenDeleting_ThenThrowsInvalidOperationException()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        mockRepository.Setup(r => r.GetActiveTemplateCount()).Returns(1);

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var act = () => sut.DeleteTemplate("test");

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot delete the last active template*");
    }

    [Fact]
    public void GivenMultipleActiveTemplates_WhenDeleting_ThenCallsRepositoryDelete()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        mockRepository.Setup(r => r.GetActiveTemplateCount()).Returns(2);

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        sut.DeleteTemplate("test");

        // Then
        mockRepository.Verify(r => r.Delete("test"), Times.Once);
    }

    [Fact]
    public void WhenRestoringTemplate_ThenCallsRepositoryRestore()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        sut.RestoreTemplate("test");

        // Then
        mockRepository.Verify(r => r.Restore("test"), Times.Once);
    }

    [Fact]
    public void WhenReorderingTemplates_ThenCallsRepositoryBulkUpdate()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        var reorder = new Dictionary<string, int>
        {
            ["template1"] = 1,
            ["template2"] = 2
        };

        // When
        sut.ReorderTemplates(reorder);

        // Then
        mockRepository.Verify(r => r.BulkUpdateDisplayOrder(reorder), Times.Once);
    }

    [Fact]
    public void GivenValidPrompt_WhenGeneratingPreview_ThenReturnsPreviewWithSampleData()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var result = sut.GeneratePreview("Analyze: {ACTIVITY_DATA_PLACEHOLDER}");

        // Then
        result.Should().NotContain("{ACTIVITY_DATA_PLACEHOLDER}");
        result.Should().Contain("| Date");
        result.Should().Contain("Visual Studio Code");
    }

    [Fact]
    public void WhenGettingAllTemplates_ThenDelegatesToRepository()
    {
        // Given
        var mockRepository = new Mock<ILlmPromptTemplateRepository>();
        var mockLogger = new Mock<ILogger<TemplateManagementService>>();

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

        mockRepository.Setup(r => r.GetAllTemplates()).Returns(templates);

        var sut = new TemplateManagementService(mockRepository.Object, mockLogger.Object);

        // When
        var result = sut.GetAllTemplates();

        // Then
        result.Should().HaveCount(1);
        result[0].TemplateKey.Should().Be("test");
    }
}
