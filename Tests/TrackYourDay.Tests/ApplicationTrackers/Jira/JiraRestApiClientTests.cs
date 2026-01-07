using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    [Trait("Category", "Integration")]
    public class JiraRestApiClientTests
    {
        private readonly Mock<HttpMessageHandler> httpMessageHandlerMock;
        private readonly JiraRestApiClient jiraRestApiClient;

        public JiraRestApiClientTests()
        {
            var mockLogger = new Mock<ILogger<JiraRestApiClient>>();
            this.jiraRestApiClient = new JiraRestApiClient("https://sampleUrl", "sampleKey", mockLogger.Object);
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
            var issues = this.jiraRestApiClient.GetUserIssues(user, DateTime.Today.AddDays(-30));

            // Assert
            issues.Should().NotBeNull();
            issues.Should().NotBeEmpty();
            issues.First().Key.Should().NotBeEmpty();
            issues.First().Fields.Summary.Should().NotBeEmpty();
            issues.First().Fields.Updated.Should().BeBefore(DateTime.Now);
        }
    }
}