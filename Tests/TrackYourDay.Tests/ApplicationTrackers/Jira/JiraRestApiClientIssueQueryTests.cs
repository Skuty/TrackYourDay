using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public async Task GivenValidWorklogData_WhenCreateIssueWorklog_ThenPostsExpectedPayload()
    {
        // Given
        var startedAt = new DateTime(2026, 1, 20, 8, 30, 0, DateTimeKind.Local);
        const int timeSpentSeconds = 1800;
        const string comment = "Meeting summary";

        string? method = null;
        string? requestUri = null;
        string? payload = null;

        var sut = CreateSut(async request =>
        {
            method = request.Method.Method;
            requestUri = request.RequestUri?.ToString();
            payload = request.Content is null ? null : await request.Content.ReadAsStringAsync();

            return new HttpResponseMessage(HttpStatusCode.Created);
        });

        // When
        await sut.CreateIssueWorklog("PROJ-123", startedAt, timeSpentSeconds, comment);

        // Then
        method.Should().Be("POST");
        requestUri.Should().Contain("/rest/api/2/issue/PROJ-123/worklog");
        payload.Should().NotBeNullOrWhiteSpace();

        using var document = JsonDocument.Parse(payload!);
        document.RootElement.GetProperty("comment").GetString().Should().Be(comment);
        document.RootElement.GetProperty("timeSpentSeconds").GetInt32().Should().Be(timeSpentSeconds);
        document.RootElement.GetProperty("started").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GivenZeroDuration_WhenCreateIssueWorklog_ThenThrowsArgumentException()
    {
        // Given
        var sut = CreateSut(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)));

        // When
        var act = () => sut.CreateIssueWorklog("PROJ-123", DateTime.Now, 0, "Comment");

        // Then
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("timeSpentSeconds");
    }

    [Fact]
    public async Task GivenJiraApiFailure_WhenCreateIssueWorklog_ThenThrowsJiraApiExceptionWithDetails()
    {
        // Given
        var sut = CreateSut(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            ReasonPhrase = "Unauthorized",
            Content = new StringContent("""{"errorMessages":["bad token"]}""", Encoding.UTF8, "application/json")
        }));

        // When
        var act = () => sut.CreateIssueWorklog("PROJ-123", DateTime.Now, 60, "Comment");

        // Then
        var exception = await act.Should().ThrowAsync<JiraApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Which.RequestPath.Should().Contain("/rest/api/2/issue/PROJ-123/worklog");
        exception.Which.ResponseBody.Should().Contain("bad token");
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
