// Tests/TrackYourDay.Tests/LlmPrompts/LlmPromptTemplateTests.cs
using FluentAssertions;
using TrackYourDay.Core.LlmPrompts;

namespace TrackYourDay.Tests.LlmPrompts;

public class LlmPromptTemplateTests
{
    [Theory]
    [InlineData("detailed")]
    [InlineData("concise")]
    [InlineData("task-oriented")]
    [InlineData("my-custom-template")]
    [InlineData("abc")]
    [InlineData("test-123")]
    public void GivenValidTemplateKey_WhenValidating_ThenReturnsTrue(string key)
    {
        // When
        var isValid = LlmPromptTemplate.IsValidTemplateKey(key);

        // Then
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    [InlineData("AB")]
    [InlineData("Test")]
    [InlineData("my_template")]
    [InlineData("template with spaces")]
    [InlineData("template!")]
    public void GivenInvalidTemplateKey_WhenValidating_ThenReturnsFalse(string key)
    {
        // When
        var isValid = LlmPromptTemplate.IsValidTemplateKey(key);

        // Then
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GivenTemplateLongerThan50Chars_WhenValidating_ThenReturnsFalse()
    {
        // Given
        var key = new string('a', 51);

        // When
        var isValid = LlmPromptTemplate.IsValidTemplateKey(key);

        // Then
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GivenTemplateWithJiraActivitiesPlaceholder_WhenCheckingPlaceholder_ThenReturnsTrue()
    {
        // Given
        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test",
            SystemPrompt = "Before {JIRA_ACTIVITIES} after",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // When
        var hasPlaceholder = template.HasValidPlaceholder();

        // Then
        hasPlaceholder.Should().BeTrue();
    }

    [Fact]
    public void GivenTemplateWithGitLabActivitiesPlaceholder_WhenCheckingPlaceholder_ThenReturnsTrue()
    {
        // Given
        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test",
            SystemPrompt = "Before {GITLAB_ACTIVITIES} after",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // When
        var hasPlaceholder = template.HasValidPlaceholder();

        // Then
        hasPlaceholder.Should().BeTrue();
    }

    [Fact]
    public void GivenTemplateWithCurrentlyAssignedPlaceholder_WhenCheckingPlaceholder_ThenReturnsTrue()
    {
        // Given
        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test",
            SystemPrompt = "Before {CURRENTLY_ASSIGNED_ISSUES} after",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // When
        var hasPlaceholder = template.HasValidPlaceholder();

        // Then
        hasPlaceholder.Should().BeTrue();
    }

    [Fact]
    public void GivenTemplateWithMultiplePlaceholders_WhenCheckingPlaceholder_ThenReturnsTrue()
    {
        // Given
        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test",
            SystemPrompt = "{CURRENTLY_ASSIGNED_ISSUES}\n\n{JIRA_ACTIVITIES}\n\n{GITLAB_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // When
        var hasPlaceholder = template.HasValidPlaceholder();

        // Then
        hasPlaceholder.Should().BeTrue();
    }

    [Fact]
    public void GivenTemplateWithoutPlaceholder_WhenCheckingPlaceholder_ThenReturnsFalse()
    {
        // Given
        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test",
            SystemPrompt = "No placeholder here",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // When
        var hasPlaceholder = template.HasValidPlaceholder();

        // Then
        hasPlaceholder.Should().BeFalse();
    }
}
