using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using Xunit;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    public class JiraActivityServiceTests
    {
        private readonly Mock<IJiraRestApiClient> jiraRestApiClientMock;
        private readonly JiraActivityService jiraActivityService;

        public JiraActivityServiceTests()
        {
            this.jiraRestApiClientMock = new Mock<IJiraRestApiClient>();
            var loggerMock = new Mock<ILogger<JiraActivityService>>();
            this.jiraActivityService = new JiraActivityService(this.jiraRestApiClientMock.Object, loggerMock.Object);
        }

        [Fact]
        public void GetTodayActivities_ShouldReturnMappedActivities()
        {
            // Arrange
            var issues = new List<JiraIssue>
            {
                new JiraIssue("ISSUE-1", "Test Issue 1", DateTime.Today),
                new JiraIssue("ISSUE-2", "Test Issue 2", DateTime.Today)
            };
            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .Returns(issues);

            // Act
            var activities = this.jiraActivityService.GetTodayActivities();

            // Assert
            activities.Should().NotBeNull();
            activities.Should().HaveCount(2);
            activities[0].Description.Should().Be("Issue ISSUE-1: Test Issue 1");
            activities[1].Description.Should().Be("Issue ISSUE-2: Test Issue 2");
        }
    }
}