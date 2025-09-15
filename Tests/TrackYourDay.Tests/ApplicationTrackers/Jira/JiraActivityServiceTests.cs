using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    public class JiraActivityServiceTests
    {
        private readonly Mock<IJiraRestApiClient> mockJiraClient;
        private readonly Mock<ILogger<JiraActivityService>> mockLogger;
        private readonly JiraActivityService jiraActivityService;

        public JiraActivityServiceTests()
        {
            mockJiraClient = new Mock<IJiraRestApiClient>();
            mockLogger = new Mock<ILogger<JiraActivityService>>();
            jiraActivityService = new JiraActivityService(mockJiraClient.Object, mockLogger.Object);
        }

        [Fact]
        public void GetActivitiesUpdatedAfter_ShouldReturnBasicActivityDescription()
        {
            // Arrange
            var testDate = DateTime.Now.AddDays(-1);
            var testUser = new JiraUser("testuser", "Test User");
            
            var testIssue = new JiraIssueResponse(
                Key: "TEST-123",
                Id: "12345",
                Fields: new JiraIssueFieldsResponse(
                    Summary: "Fix critical bug",
                    Updated: DateTimeOffset.Now,
                    Status: null,
                    Priority: null,
                    Assignee: null,
                    Reporter: null,
                    IssueType: null,
                    Project: null,
                    Created: null,
                    Description: null,
                    Labels: null,
                    Components: null
                )
            );

            mockJiraClient.Setup(x => x.GetCurrentUser()).Returns(testUser);
            mockJiraClient.Setup(x => x.GetUserIssues(testUser, testDate))
                         .Returns(new List<JiraIssueResponse> { testIssue });

            // Act
            var activities = jiraActivityService.GetActivitiesUpdatedAfter(testDate);

            // Assert
            Assert.Single(activities);
            var activity = activities[0];
            
            // Verify basic structure
            Assert.NotNull(activity.Description);
            Assert.NotEmpty(activity.Description);
            
            // Test that basic information is included
            Assert.Contains("TEST-123", activity.Description);
            Assert.Contains("Fix critical bug", activity.Description);
            Assert.Contains("Issue ID: 12345", activity.Description);
            Assert.Contains("Updated:", activity.Description);
            Assert.Contains("Assignee: Unassigned", activity.Description);
        }

        [Fact]
        public void GetActivitiesUpdatedAfter_ShouldHandleMinimalIssueData()
        {
            // Arrange
            var testDate = DateTime.Now.AddDays(-1);
            var testUser = new JiraUser("testuser", "Test User");
            
            var minimalIssue = new JiraIssueResponse(
                Key: "MIN-001",
                Id: "67890",
                Fields: new JiraIssueFieldsResponse(
                    Summary: null,
                    Updated: DateTimeOffset.Now,
                    Status: null,
                    Priority: null,
                    Assignee: null,
                    Reporter: null,
                    IssueType: null,
                    Project: null,
                    Created: null,
                    Description: null,
                    Labels: null,
                    Components: null
                )
            );

            mockJiraClient.Setup(x => x.GetCurrentUser()).Returns(testUser);
            mockJiraClient.Setup(x => x.GetUserIssues(testUser, testDate))
                         .Returns(new List<JiraIssueResponse> { minimalIssue });

            // Act
            var activities = jiraActivityService.GetActivitiesUpdatedAfter(testDate);

            // Assert
            Assert.Single(activities);
            var activity = activities[0];
            
            // Verify the description handles null/missing data gracefully
            Assert.Contains("MIN-001: No Summary", activity.Description);
            Assert.Contains("Assignee: Unassigned", activity.Description);
            Assert.Contains("Issue ID: 67890", activity.Description);
            Assert.Contains("Updated:", activity.Description);
            
            // Should not contain optional fields that are null
            Assert.DoesNotContain("Type:", activity.Description);
            Assert.DoesNotContain("Project:", activity.Description);
            Assert.DoesNotContain("Status:", activity.Description);
            Assert.DoesNotContain("Priority:", activity.Description);
            Assert.DoesNotContain("Reporter:", activity.Description);
            Assert.DoesNotContain("Components:", activity.Description);
            Assert.DoesNotContain("Labels:", activity.Description);
            Assert.DoesNotContain("Description:", activity.Description);
            Assert.DoesNotContain("Created:", activity.Description);
        }
    }
}
