// Tests/TrackYourDay.Tests/LlmPrompts/LlmPromptServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.LlmPrompts;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.Settings;
using Newtonsoft.Json;

namespace TrackYourDay.Tests.LlmPrompts;

public class LlmPromptServiceTests
{
    [Fact]
    public async Task GivenValidTemplate_WhenGeneratingPrompt_ThenReturnsPromptWithActivityData()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "Analyze: {JIRA_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));
        
        var jiraActivity = new JiraActivity
        {
            UpstreamId = "jira-1",
            OccurrenceDate = DateTime.Today.AddHours(9),
            Description = "Created Issue PROJ-123"
        };
        
        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { jiraActivity });
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GitLabActivity>());
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraIssueState>());
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitLabStateSnapshot?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().NotBeNullOrEmpty();
        result.Should().NotContain("{JIRA_ACTIVITIES}");
        result.Should().Contain("PROJ-123");
    }

    [Fact]
    public async Task GivenNonExistentTemplate_WhenGeneratingPrompt_ThenThrowsInvalidOperationException()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        mockSettings.Setup(r => r.GetSetting(It.IsAny<string>())).Returns((string?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            Mock.Of<IHistoricalDataRepository<JiraActivity>>(),
            Mock.Of<IHistoricalDataRepository<GitLabActivity>>(),
            Mock.Of<IJiraIssueRepository>(),
            Mock.Of<IGitLabStateRepository>(),
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var act = async () => await sut.GeneratePrompt("nonexistent", DateOnly.FromDateTime(DateTime.Today));

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task GivenJiraActivitiesExist_WhenGeneratingPrompt_ThenIncludesJiraSection()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "{JIRA_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var jiraActivities = new List<JiraActivity>
        {
            new() { UpstreamId = "jira-1", OccurrenceDate = DateTime.Today.AddHours(9), Description = "Created Issue PROJ-123" },
            new() { UpstreamId = "jira-2", OccurrenceDate = DateTime.Today.AddHours(14), Description = "Logged 2h on PROJ-456" }
        };

        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraActivities);
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GitLabActivity>());
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraIssueState>());
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitLabStateSnapshot?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Contain("| Time | Activity Description |");
        result.Should().Contain("PROJ-123");
        result.Should().Contain("PROJ-456");
    }

    [Fact]
    public async Task GivenGitLabActivitiesExist_WhenGeneratingPrompt_ThenIncludesGitLabSection()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "{GITLAB_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var gitLabActivities = new List<GitLabActivity>
        {
            new() { UpstreamId = "gitlab-1", OccuranceDate = DateTime.Today.AddHours(10), Description = "Opened MR !123" },
            new() { UpstreamId = "gitlab-2", OccuranceDate = DateTime.Today.AddHours(15), Description = "Merged MR !456" }
        };

        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraActivity>());
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitLabActivities);
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraIssueState>());
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitLabStateSnapshot?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Contain("| Time | Activity Description |");
        result.Should().Contain("!123");
        result.Should().Contain("!456");
    }

    [Fact]
    public async Task GivenBothJiraAndGitLabActivitiesExist_WhenGeneratingPrompt_ThenIncludesBothSections()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "{JIRA_ACTIVITIES}\n\n{GITLAB_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var jiraActivities = new List<JiraActivity>
        {
            new() { UpstreamId = "jira-1", OccurrenceDate = DateTime.Today.AddHours(9), Description = "Created Issue PROJ-123" }
        };

        var gitLabActivities = new List<GitLabActivity>
        {
            new() { UpstreamId = "gitlab-1", OccuranceDate = DateTime.Today.AddHours(10), Description = "Opened MR !123" }
        };

        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraActivities);
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitLabActivities);
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraIssueState>());
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitLabStateSnapshot?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Contain("| Time | Activity Description |");
        result.Should().Contain("PROJ-123");
        result.Should().Contain("!123");
    }

    [Fact]
    public async Task GivenCurrentlyAssignedIssuesExist_WhenGeneratingPrompt_ThenIncludesCurrentlyAssignedSection()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "{CURRENTLY_ASSIGNED_ISSUES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var jiraIssues = new List<JiraIssueState>
        {
            new()
            {
                Key = "PROJ-123",
                Id = "1",
                Summary = "Fix critical bug",
                Status = "In Progress",
                IssueType = "Bug",
                ProjectKey = "PROJ",
                Updated = DateTimeOffset.Now,
                BrowseUrl = "https://jira.example.com/browse/PROJ-123"
            }
        };

        var gitLabSnapshot = new GitLabStateSnapshot
        {
            Guid = Guid.NewGuid(),
            CapturedAt = DateTime.Now,
            Artifacts = new List<GitLabArtifact>
            {
                new()
                {
                    Id = 1,
                    Iid = 123,
                    ProjectId = 10,
                    Title = "Feature implementation",
                    Description = "Implement new feature",
                    Type = GitLabArtifactType.MergeRequest,
                    State = "opened",
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now,
                    WebUrl = "https://gitlab.example.com/project/merge_requests/123",
                    AuthorUsername = "user1",
                    AssigneeUsernames = new List<string> { "user1" }
                }
            }
        };

        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraActivity>());
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GitLabActivity>());
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraIssues);
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitLabSnapshot);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Contain("| Key | Summary | Status | Project | Updated |");
        result.Should().Contain("PROJ-123");
        result.Should().Contain("Fix critical bug");
        result.Should().Contain("| Type | Title | State | Updated |");
        result.Should().Contain("Feature implementation");
    }

    [Fact]
    public async Task GivenNoActivitiesOnDate_WhenGeneratingPrompt_ThenReplacesPlaceholderWithEmptyString()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "Analyze: {JIRA_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));
        
        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraActivity>());
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GitLabActivity>());
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraIssueState>());
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitLabStateSnapshot?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Be("Analyze: No data available");
    }

    [Fact]
    public async Task GivenJiraRepositoryThrowsException_WhenGeneratingPrompt_ThenContinuesWithNoDataMessage()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "{JIRA_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));
        
        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GitLabActivity>());
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JiraIssueState>());
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitLabStateSnapshot?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Be("No data available");
    }

    [Fact]
    public async Task GivenThreePlaceholders_WhenGeneratingPrompt_ThenReplacesAllIndependently()
    {
        // Given
        var mockSettings = new Mock<IGenericSettingsRepository>();
        var mockJiraActivityRepo = new Mock<IHistoricalDataRepository<JiraActivity>>();
        var mockGitLabActivityRepo = new Mock<IHistoricalDataRepository<GitLabActivity>>();
        var mockJiraIssueRepo = new Mock<IJiraIssueRepository>();
        var mockGitLabStateRepo = new Mock<IGitLabStateRepository>();

        var template = new LlmPromptTemplate
        {
            TemplateKey = "test",
            Name = "Test Template",
            SystemPrompt = "{CURRENTLY_ASSIGNED_ISSUES}\n\n{JIRA_ACTIVITIES}\n\n{GITLAB_ACTIVITIES}",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mockSettings.Setup(r => r.GetSetting("llm_template:test")).Returns(JsonConvert.SerializeObject(template));

        var jiraActivities = new List<JiraActivity>
        {
            new() { UpstreamId = "jira-1", OccurrenceDate = DateTime.Today.AddHours(9), Description = "Activity 1" }
        };

        var gitLabActivities = new List<GitLabActivity>
        {
            new() { UpstreamId = "gitlab-1", OccuranceDate = DateTime.Today.AddHours(10), Description = "Activity 2" }
        };

        var jiraIssues = new List<JiraIssueState>
        {
            new()
            {
                Key = "PROJ-123",
                Id = "1",
                Summary = "Issue 1",
                Status = "Open",
                IssueType = "Bug",
                ProjectKey = "PROJ",
                Updated = DateTimeOffset.Now,
                BrowseUrl = "#"
            }
        };

        mockJiraActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<JiraActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraActivities);
        
        mockGitLabActivityRepo.Setup(r => r.FindAsync(It.IsAny<DateRangeSpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitLabActivities);
        
        mockJiraIssueRepo.Setup(r => r.GetCurrentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jiraIssues);
        
        mockGitLabStateRepo.Setup(r => r.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitLabStateSnapshot?)null);

        var sut = new LlmPromptService(
            mockSettings.Object,
            mockJiraActivityRepo.Object,
            mockGitLabActivityRepo.Object,
            mockJiraIssueRepo.Object,
            mockGitLabStateRepo.Object,
            Mock.Of<ILogger<LlmPromptService>>());

        // When
        var result = await sut.GeneratePrompt("test", DateOnly.FromDateTime(DateTime.Today));

        // Then
        result.Should().Contain("| Key | Summary | Status | Project | Updated |");
        result.Should().Contain("| Time | Activity Description |");
        result.Should().NotContain("{CURRENTLY_ASSIGNED_ISSUES}");
        result.Should().NotContain("{JIRA_ACTIVITIES}");
        result.Should().NotContain("{GITLAB_ACTIVITIES}");
    }
}
