using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using Xunit;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    public class JiraTrackerTests
    {
        private readonly Mock<JiraActivityService> jiraActivityServiceMock;
        private readonly JiraTracker jiraTracker;

        public JiraTrackerTests()
        {
            this.jiraActivityServiceMock = new Mock<JiraActivityService>(null, null);
            this.jiraTracker = new JiraTracker(this.jiraActivityServiceMock.Object);
        }

        [Fact]
        public void GetJiraActivities_ShouldReturnActivities()
        {
            // Arrange
            var activities = new List<JiraActivity>
            {
                new JiraActivity(DateTime.Today, "Issue ISSUE-1: Test Issue 1"),
                new JiraActivity(DateTime.Today, "Issue ISSUE-2: Test Issue 2")
            };
            this.jiraActivityServiceMock.Setup(service => service.GetTodayActivities())
                .Returns(activities);

            // Act
            var result = this.jiraTracker.GetJiraActivities();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(activities);
        }
    }
}