using FluentAssertions;
using TrackYourDay.Core.Services.PromptGeneration;
using Xunit;

namespace TrackYourDay.Tests.Services.PromptGeneration;

public sealed class PromptTemplateProviderTests
{
    private readonly PromptTemplateProvider _sut = new();

    [Theory]
    [InlineData(PromptTemplate.DetailedSummaryWithTimeAllocation)]
    [InlineData(PromptTemplate.ConciseBulletPointSummary)]
    [InlineData(PromptTemplate.JiraFocusedWorklogTemplate)]
    public void GivenValidTemplate_WhenGettingTemplate_ThenReturnsNonEmptyString(PromptTemplate template)
    {
        // When
        var result = _sut.GetTemplate(template);

        // Then
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("{DATE}");
        result.Should().Contain("{ACTIVITY_LIST}");
        result.Should().Contain("{JIRA_KEYS}");
    }

    [Fact]
    public void GivenInvalidTemplate_WhenGettingTemplate_ThenThrowsArgumentException()
    {
        // Given
        var invalidTemplate = (PromptTemplate)999;

        // When
        var act = () => _sut.GetTemplate(invalidTemplate);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("Unknown template: 999*");
    }

    [Fact]
    public void WhenGettingAvailableTemplates_ThenReturnsAllTemplates()
    {
        // When
        var result = _sut.GetAvailableTemplates();

        // Then
        result.Should().HaveCount(3);
        result.Should().ContainKey(PromptTemplate.DetailedSummaryWithTimeAllocation);
        result.Should().ContainKey(PromptTemplate.ConciseBulletPointSummary);
        result.Should().ContainKey(PromptTemplate.JiraFocusedWorklogTemplate);
    }
}
