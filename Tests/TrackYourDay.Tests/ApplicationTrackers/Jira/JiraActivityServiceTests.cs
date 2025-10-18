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

        public JiraActivityServiceTests()
        {
            this.jiraRestApiClientMock = new Mock<IJiraRestApiClient>();
            this.loggerMock = new Mock<ILogger<JiraActivityService>>();
            this.jiraActivityService = new JiraActivityService(jiraRestApiClientMock.Object, loggerMock.Object);
        }

        [Fact]
        public void GivenIssueWithStatusChange_WhenGettingActivities_ThenReturnStatusChangeActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("status", "jira", "10001", "In Progress", "10002", "Done")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-1", "1", new JiraIssueFieldsResponse("Test Issue", new DateTime(2000, 01, 01, 10, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Changed status of PROJ-1");
            activities[0].Description.Should().Contain("In Progress");
            activities[0].Description.Should().Contain("Done");
        }

        [Fact]
        public void GivenIssueWithAssigneeChange_WhenGettingActivities_ThenReturnAssignmentActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("assignee", "jira", null, null, "account456", "Jane Smith")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-2", "2", new JiraIssueFieldsResponse("Assign Test", new DateTime(2000, 01, 01, 10, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Assigned PROJ-2");
            activities[0].Description.Should().Contain("Jane Smith");
        }

        [Fact]
        public void GivenIssueWithResolution_WhenGettingActivities_ThenReturnResolvedActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("resolution", "jira", null, null, "10000", "Fixed")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-3", "3", new JiraIssueFieldsResponse("Resolution Test", new DateTime(2000, 01, 01, 10, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Resolved PROJ-3");
            activities[0].Description.Should().Contain("Fixed");
        }

        [Fact]
        public void GivenIssueWithMultipleChanges_WhenGettingActivities_ThenReturnMultipleActivities()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("status", "jira", "10001", "To Do", "10002", "In Progress"),
                        new JiraChangeItemResponse("assignee", "jira", null, null, "account456", "Jane Smith")
                    }),
                new JiraHistoryResponse(
                    "2",
                    new JiraAuthorResponse("Jane Smith", "account456"),
                    new DateTime(2000, 01, 01, 14, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("status", "jira", "10002", "In Progress", "10003", "Done")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-4", "4", new JiraIssueFieldsResponse("Multi Change Test", new DateTime(2000, 01, 01, 14, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(3);
            activities[0].Description.Should().Contain("In Progress");
            activities[1].Description.Should().Contain("Assigned");
            activities[2].Description.Should().Contain("Done");
        }

        [Fact]
        public void GivenIssueWithComment_WhenGettingActivities_ThenReturnCommentActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("comment", "jira", null, null, "12345", "This is a comment")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-5", "5", new JiraIssueFieldsResponse("Comment Test", new DateTime(2000, 01, 01, 10, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Commented on PROJ-5");
        }

        [Fact]
        public void GivenIssueWithPriorityChange_WhenGettingActivities_ThenReturnPriorityChangeActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("priority", "jira", "3", "Medium", "2", "High")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-6", "6", new JiraIssueFieldsResponse("Priority Test", new DateTime(2000, 01, 01, 10, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Changed priority of PROJ-6");
            activities[0].Description.Should().Contain("Medium");
            activities[0].Description.Should().Contain("High");
        }

        [Fact]
        public void GivenIssueWithoutChangelog_WhenGettingActivities_ThenReturnSimpleUpdateActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-7", "7", new JiraIssueFieldsResponse("No Changelog Test", new DateTime(2000, 01, 01, 10, 00, 00), null, null), null)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Jira Issue Updated - PROJ-7");
        }

        [Fact]
        public void GivenIssueWithSprintChange_WhenGettingActivities_ThenReturnSprintActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("sprint", "custom", null, null, "123", "Sprint 5")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-8", "8", new JiraIssueFieldsResponse("Sprint Test", new DateTime(2000, 01, 01, 10, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Added PROJ-8");
            activities[0].Description.Should().Contain("sprint");
        }

        [Fact]
        public void GivenIssueWithTimeLogging_WhenGettingActivities_ThenReturnWorkLogActivity()
        {
            // Given
            var currentUser = new JiraUser("test-user", "test@example.com");
            var updateDate = new DateTime(2000, 01, 01);

            var changelog = new JiraChangelogResponse(new List<JiraHistoryResponse>
            {
                new JiraHistoryResponse(
                    "1",
                    new JiraAuthorResponse("John Doe", "account123"),
                    new DateTime(2000, 01, 01, 10, 00, 00),
                    new List<JiraChangeItemResponse>
                    {
                        new JiraChangeItemResponse("timespent", "jira", "0", "0m", "7200", "2h")
                    })
            });

            var issues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("PROJ-9", "9", new JiraIssueFieldsResponse("Time Log Test", new DateTime(2000, 01, 01, 10, 00, 00), null, null), changelog)
            };

            this.jiraRestApiClientMock.Setup(client => client.GetCurrentUser()).Returns(currentUser);
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(currentUser, updateDate)).Returns(issues);

            // When
            var activities = this.jiraActivityService.GetActivitiesUpdatedAfter(updateDate);

            // Then
            activities.Should().NotBeNull();
            activities.Should().HaveCount(1);
            activities[0].Description.Should().Contain("Logged work on PROJ-9");
        }
    }
}
