using FluentAssertions;
using System.Net;
using System.Text;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira;

public sealed class JiraRestApiClientIssueQueryTests
{
    [Fact]
    public async Task GivenBlankFilterName_WhenGetIssuesByFilterName_ThenThrowsArgumentException()
    {
        // Given
        var sut = CreateSut(_ => Task.FromResult(CreateSearchResponse(HttpStatusCode.OK, EmptyIssuesJson())));

        // When
        var act = () => sut.GetIssuesByFilterName(" ");

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("issueFilterName");
    }

    [Fact]
    public async Task GivenBlankRawJql_WhenGetIssuesByRawJql_ThenThrowsArgumentException()
    {
        // Given
        var sut = CreateSut(_ => Task.FromResult(CreateSearchResponse(HttpStatusCode.OK, EmptyIssuesJson())));

        // When
        var act = () => sut.GetIssuesByRawJql(" ");

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("rawJql");
    }

    [Fact]
    public async Task GivenFilterName_WhenGetIssuesByFilterName_ThenUsesFilterQueryInSearchRequest()
    {
        // Given
        string? requestUri = null;
        var sut = CreateSut(request =>
        {
            requestUri = request.RequestUri?.ToString();
            return Task.FromResult(CreateSearchResponse(HttpStatusCode.OK, EmptyIssuesJson()));
        });

        // When
        _ = await sut.GetIssuesByFilterName("My Shared Filter");

        // Then
        requestUri.Should().NotBeNullOrEmpty();
        requestUri.Should().Contain("/rest/api/2/search?jql=");
        ExtractDecodedJql(requestUri!).Should().Be("filter = \"My Shared Filter\"");
    }

    [Fact]
    public async Task GivenRawJql_WhenGetIssuesByRawJql_ThenUsesProvidedJqlInSearchRequest()
    {
        // Given
        string? requestUri = null;
        var sut = CreateSut(request =>
        {
            requestUri = request.RequestUri?.ToString();
            return Task.FromResult(CreateSearchResponse(HttpStatusCode.OK, EmptyIssuesJson()));
        });

        // When
        _ = await sut.GetIssuesByRawJql("project = PROJ AND assignee = currentUser()");

        // Then
        requestUri.Should().NotBeNullOrEmpty();
        requestUri.Should().Contain("/rest/api/2/search?jql=");
        ExtractDecodedJql(requestUri!).Should().Be("project = PROJ AND assignee = currentUser()");
    }

    private static JiraRestApiClient CreateSut(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new TestHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://jira.example.com")
        };

        return new JiraRestApiClient(httpClient);
    }

    private static HttpResponseMessage CreateSearchResponse(HttpStatusCode statusCode, string jsonContent)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
    }

    private static string EmptyIssuesJson() => """{"issues":[],"total":0,"startAt":0,"maxResults":100}""";

    private static string ExtractDecodedJql(string requestUri)
    {
        var uri = new Uri(requestUri);
        var query = uri.Query.TrimStart('?');
        var jqlEntry = query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .First(part => part.StartsWith("jql=", StringComparison.Ordinal));
        var encodedJql = jqlEntry.Substring("jql=".Length);

        return Uri.UnescapeDataString(encodedJql);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request);
    }
}
