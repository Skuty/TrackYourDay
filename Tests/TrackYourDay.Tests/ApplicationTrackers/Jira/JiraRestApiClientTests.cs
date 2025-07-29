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
            var httpClient = new HttpClient(this.httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://example.atlassian.net")
            };
            this.jiraRestApiClient = new JiraRestApiClient("https://example.atlassian.net", "fake-api-key");
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnUserDetails()
        {
            // Arrange

            // Act
            var user = this.jiraRestApiClient.GetCurrentUser();

            // Assert
            user.Should().NotBeNull();
            user.AccountId.Should().Be("12345");
            user.DisplayName.Should().Be("Test User");
        }

        [Fact]
        public void GetUserIssues_ShouldReturnIssues()
        {
            // Arrange
            var responseContent = JsonSerializer.Serialize(new List<JiraIssue>
            {
                new JiraIssue("ISSUE-1", "Test Issue 1", DateTime.Now),
                new JiraIssue("ISSUE-2", "Test Issue 2", DateTime.Now)
            });

            // Act
            var issues = this.jiraRestApiClient.GetUserIssues("12345", DateTime.Today);

            // Assert
            issues.Should().NotBeNull();
            issues.Should().HaveCount(2);
        }
    }
}