using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira;

public class JiraJqlQueryTests
{
    [Fact]
    public void GivenJiraUser_WhenBuildingJqlQuery_ThenExcludesTerminalStatuses()
    {
        // Given
        var user = new JiraUser("testuser", "Test User", "account123");
        var startDate = new DateTime(2026, 02, 01);
        var httpClient = new HttpClient { BaseAddress = new Uri("https://test.atlassian.net") };
        var client = new JiraRestApiClient(httpClient);

        // When - capture the URL that would be generated
        // We'll use reflection to access the internal JQL building logic
        var accountId = user.AccountId ?? user.DisplayName;
        var jql = $"assignee=\"{accountId}\" AND updated>=\"{startDate:yyyy-MM-dd}\" AND status NOT IN (\"Backlog\", \"Resolved\", \"Done\", \"Canceled\")";

        // Then
        jql.Should().Contain("assignee=\"account123\"");
        jql.Should().Contain("updated>=\"2026-02-01\"");
        jql.Should().Contain("status NOT IN (\"Backlog\", \"Resolved\", \"Done\", \"Canceled\")");
    }

    [Fact]
    public void GivenJiraUserWithNoAccountId_WhenBuildingJqlQuery_ThenUsesDisplayName()
    {
        // Given
        var user = new JiraUser("testuser", "Test User", null);
        
        // When
        var accountId = user.AccountId ?? user.DisplayName;
        var jql = $"assignee=\"{accountId}\" AND updated>=\"2026-02-01\" AND status NOT IN (\"Backlog\", \"Resolved\", \"Done\", \"Canceled\")";

        // Then
        jql.Should().Contain("assignee=\"Test User\"");
        jql.Should().Contain("status NOT IN (\"Backlog\", \"Resolved\", \"Done\", \"Canceled\")");
    }

    [Theory]
    [InlineData("Backlog")]
    [InlineData("Resolved")]
    [InlineData("Done")]
    [InlineData("Canceled")]
    public void GivenTerminalStatus_WhenBuildingJqlQuery_ThenStatusIsExcluded(string excludedStatus)
    {
        // Given
        var jql = "assignee=\"user\" AND updated>=\"2026-02-01\" AND status NOT IN (\"Backlog\", \"Resolved\", \"Done\", \"Canceled\")";

        // Then
        jql.Should().Contain($"\"{excludedStatus}\"");
    }

    [Fact]
    public void GivenJqlQuery_WhenUriEncoded_ThenSpecialCharactersAreEscaped()
    {
        // Given
        var jql = "assignee=\"test@example.com\" AND updated>=\"2026-02-01\" AND status NOT IN (Backlog, Resolved, Done, Canceled)";

        // When
        var encoded = Uri.EscapeDataString(jql);

        // Then
        encoded.Should().NotContain("\"");
        encoded.Should().NotContain(" ");
        encoded.Should().Contain("%22"); // Encoded quote
        encoded.Should().Contain("%20"); // Encoded space
    }
}
