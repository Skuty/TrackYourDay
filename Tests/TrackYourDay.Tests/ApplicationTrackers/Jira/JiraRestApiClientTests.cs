using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using Xunit;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    public class JiraRestApiClientTests
    {
        private readonly Mock<HttpMessageHandler> httpMessageHandlerMock;
        private readonly JiraRestApiClient jiraRestApiClient;

        public JiraRestApiClientTests()
        {
            this.jiraRestApiClient = new JiraRestApiClient("https://sampleinstance", "samplekey");
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnUserDetails()
        {
            // Arrange

            // Act
            var user = this.jiraRestApiClient.GetCurrentUser();

            // Assert
            user.Should().NotBeNull();
            user.Name.Should().NotBeEmpty();
            user.DisplayName.Should().NotBeEmpty();
        }

        [Fact]
        public void GetUserIssues_ShouldReturnIssues()
        {
            // Arrange
            var user = this.jiraRestApiClient.GetCurrentUser();

            // Act
            var issues = this.jiraRestApiClient.GetUserIssues(user, DateTime.Today);

            // Assert
            issues.Should().NotBeNull();
            issues.Should().HaveCount(2);
        }
    }
}