using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    public class JiraActivityServiceTests
    {
        private readonly Mock<IJiraRestApiClient> jiraRestApiClientMock;
        private readonly Mock<ILogger<JiraActivityService>> loggerMock;
        private readonly JiraActivityService jiraActivityService;
        private readonly JiraUser currentUser;

        public JiraActivityServiceTests()
        {
            this.jiraRestApiClientMock = new Mock<IJiraRestApiClient>();
            this.loggerMock = new Mock<ILogger<JiraActivityService>>();
            this.jiraActivityService = new JiraActivityService(jiraRestApiClientMock.Object, loggerMock.Object);
            this.currentUser = new JiraUser("test-user-name", "test-user");  // Name, DisplayName

            // Setup default current user
            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).ReturnsAsync(currentUser);
        }

        private JiraIssueFieldsResponse CreateIssueFields(
            string summary,
            DateTime updated,
            DateTime? created = null,
            string? projectKey = "PROJ",
            string? projectName = "Project",
            string? issueType = "Task",
            bool isSubtask = false,
            string? creatorName = null,
            JiraParentIssueResponse? parent = null)
        {
            return new JiraIssueFieldsResponse(
                summary,
                new DateTimeOffset(updated),
                created.HasValue ? new DateTimeOffset(created.Value) : null,
                new JiraStatusResponse("Open"),
                new JiraUserResponse("test-user"),
                creatorName != null ? new JiraUserResponse(creatorName) : null,
                new JiraIssueTypeResponse(issueType, isSubtask),
                new JiraProjectResponse(projectKey, projectName),
                parent
            );
        }

        [Fact]
        public async Task GivenIssueWithStatusChangeByCurrentUser_WhenGettingActivities_ThenReturnStatusChangeActivity()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("test-user", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("status", "jira", "10001", "In Progress", "10002", "Done")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-1", "1", CreateIssueFields("Test Issue", new DateTime(2000, 01, 01, 10, 00, 00)), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Changed status");
            activities[0].Description.Should().Contain("PROJ-1");
            activities[0].Description.Should().Contain("In Progress");
            activities[0].Description.Should().Contain("Done");
        }

        [Fact]
        public async Task GivenIssueWithStatusChangeByOtherUser_WhenGettingActivities_ThenReturnNoActivities()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("other-user", "account456"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("status", "jira", "10001", "In Progress", "10002", "Done")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-1", "1", CreateIssueFields("Test Issue", new DateTime(2000, 01, 01, 10, 00, 00)), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().BeEmpty();
        }

        [Fact]
        public async Task GivenIssueCreatedByCurrentUser_WhenGettingActivities_ThenReturnIssueCreationActivity()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);
            var createdDate = new DateTime(2000, 01, 01, 9, 00, 00);

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-1", "1",
                    CreateIssueFields("New Issue", new DateTime(2000, 01, 01, 10, 00, 00), createdDate, creatorName: "test-user"),
                    null)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Created");
            activities[0].Description.Should().Contain("PROJ-1");
            activities[0].Description.Should().Contain("New Issue");
        }

        [Fact]
        public async Task GivenSubtaskCreatedByCurrentUser_WhenGettingActivities_ThenReturnSubtaskCreationWithParent()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);
            var createdDate = new DateTime(2000, 01, 01, 9, 00, 00);

            var parent = new JiraParentIssueResponse(
                "PROJ-1",
                new JiraParentFieldsResponse("Parent Story", new JiraIssueTypeResponse("Story", false))
            );

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-2", "2",
                    CreateIssueFields("Subtask Issue", new DateTime(2000, 01, 01, 10, 00, 00), createdDate,
                        issueType: "Sub-task", isSubtask: true, creatorName: "test-user", parent: parent),
                    null)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Created Sub-task");
            activities[0].Description.Should().Contain("PROJ-2");
            activities[0].Description.Should().Contain("sub-task of Story PROJ-1");
        }

        [Fact]
        public async Task GivenWorklogByCurrentUser_WhenGettingActivities_ThenReturnWorklogActivity()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-1", "1", CreateIssueFields("Test Issue", new DateTime(2000, 01, 01, 10, 00, 00)), null)
            };

            var worklogs = new List<JiraWorklogResponse>
            {
                new JiraWorklogResponse(
                    "1",
                    new JiraUserResponse("test-user"),
                    "Fixed the bug",
                    new DateTime(2000, 01, 01, 11, 00, 00),
                    "2h",
                    7200
                )
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);
            this.jiraRestApiClientMock.Setup(client => client.GetIssueWorklogs("PROJ-1", updateDate)).ReturnsAsync(worklogs);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Logged 2h");
            activities[0].Description.Should().Contain("PROJ-1");
            activities[0].Description.Should().Contain("Fixed the bug");
        }

        [Fact]
        public async Task GivenWorklogByOtherUser_WhenGettingActivities_ThenReturnNoWorklogActivity()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-1", "1", CreateIssueFields("Test Issue", new DateTime(2000, 01, 01, 10, 00, 00)), null)
            };

            var worklogs = new List<JiraWorklogResponse>
            {
                new JiraWorklogResponse(
                    "1",
                    new JiraUserResponse("other-user"),
                    "Fixed the bug",
                    new DateTime(2000, 01, 01, 11, 00, 00),
                    "2h",
                    7200
                )
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);
            this.jiraRestApiClientMock.Setup(client => client.GetIssueWorklogs("PROJ-1", updateDate)).ReturnsAsync(worklogs);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().BeEmpty();
        }

        [Fact]
        public async Task GivenMultipleActivitiesByCurrentUser_WhenGettingActivities_ThenReturnAllActivitiesSortedByDate()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);
            var createdDate = new DateTime(2000, 01, 01, 8, 00, 00);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("test-user", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("status", "jira", "10001", "To Do", "10002", "In Progress")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-1", "1",
                    CreateIssueFields("Test Issue", new DateTime(2000, 01, 01, 14, 00, 00), createdDate, creatorName: "test-user"),
                    changelog)
            };

            var worklogs = new List<JiraWorklogResponse>
            {
                new JiraWorklogResponse("1", new JiraUserResponse("test-user"), "Work done", new DateTime(2000, 01, 01, 12, 00, 00), "1h", 3600)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);
            this.jiraRestApiClientMock.Setup(client => client.GetIssueWorklogs("PROJ-1", updateDate)).ReturnsAsync(worklogs);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(3);
            activities[0].OccurrenceDate.Should().Be(new DateTime(2000, 01, 01, 8, 00, 00)); // Created
            activities[1].OccurrenceDate.Should().Be(new DateTime(2000, 01, 01, 10, 00, 00)); // Status change
            activities[2].OccurrenceDate.Should().Be(new DateTime(2000, 01, 01, 12, 00, 00)); // Worklog
        }

        [Fact]
        public async Task GivenIssueWithProjectAndIssueTypeInfo_WhenGettingActivities_ThenIncludeContextInDescription()
        {
            // Given
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("test-user", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("priority", "jira", "3", "Medium", "2", "High")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("MYPROJ-123", "1",
                    CreateIssueFields("Priority Test", new DateTime(2000, 01, 01, 10, 00, 00),
                        projectKey: "MYPROJ", issueType: "Bug"),
                    changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).ReturnsAsync(issues);

            // When
            var activities = await this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Bug MYPROJ-123 in MYPROJ");
            activities[0].Description.Should().Contain("Medium");
            activities[0].Description.Should().Contain("High");
        }
    }
}
