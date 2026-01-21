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
            var httpClient = new HttpClient { BaseAddress = new Uri("https://sampleUrl") };
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sampleKey");
            this.jiraRestApiClient = new JiraRestApiClient(httpClient);
        }

        [Fact]
        public async Task GetCurrentUser_ShouldReturnUserDetails()
        {
            // Arrange

            // Act
            var user = await this.jiraRestApiClient.GetCurrentUser();

            // Assert
            user.Should().NotBeNull();
            user.Name.Should().NotBeEmpty();
            user.DisplayName.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetUserIssues_ShouldReturnIssues()
        {
            // Arrange
            var user = await this.jiraRestApiClient.GetCurrentUser();

            // Act
            var issues = await this.jiraRestApiClient.GetUserIssues(user, DateTime.Today.AddDays(-30));

            // Assert
            issues.Should().NotBeNull();
            issues.Should().NotBeEmpty();
            issues.First().Key.Should().NotBeEmpty();
            issues.First().Fields.Summary.Should().NotBeEmpty();
            issues.First().Fields.Updated.Should().BeBefore(DateTime.Now);
        }
    }
}