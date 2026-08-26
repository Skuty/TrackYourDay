using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.GitLab;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab;

[Trait("Category", "Unit")]
public class GitLabRestApiClientUnitTests
{
    [Fact]
    public async Task GivenRefNameContainsSlash_WhenGettingCommits_ThenRequestUsesUrlEncodedRefName()
    {
        // Given
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://gitlab.com") };
        var sut = new GitLabRestApiClient(client);

        // When
        await sut.GetCommits(new GitLabProjectId(123), new GitLabRefName("feature/commit-tracking"), new DateOnly(2025, 03, 01));

        // Then
        handler.RequestUris.Should().HaveCount(1);
        handler.RequestUris[0].Query.Should().Contain("ref_name=feature%2Fcommit-tracking");
    }

    [Fact]
    public async Task GivenCommitsSpanMultiplePages_WhenGettingCommits_ThenAllPagesAreFetched()
    {
        // Given
        var firstPage = Enumerable.Range(1, 100).Select(CreateCommit).ToList();
        var secondPage = new List<GitLabCommit> { CreateCommit(101) };

        var responses = new Queue<string>([
            JsonSerializer.Serialize(firstPage),
            JsonSerializer.Serialize(secondPage)
        ]);

        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responses.Dequeue(), Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://gitlab.com") };
        var sut = new GitLabRestApiClient(client);

        // When
        var commits = await sut.GetCommits(new GitLabProjectId(123), new GitLabRefName("master"), new DateOnly(2025, 03, 01));

        // Then
        commits.Should().HaveCount(101);
        handler.RequestUris.Should().HaveCount(2);
        handler.RequestUris[0].Query.Should().Contain("page=1");
        handler.RequestUris[1].Query.Should().Contain("page=2");
    }

    private static GitLabCommit CreateCommit(int index)
    {
        var date = new DateTimeOffset(2025, 03, 01, 10, 0, 0, TimeSpan.Zero).AddMinutes(index);

        return new GitLabCommit(
            $"commit-{index:000}",
            $"{index:000}",
            $"Commit {index}",
            "Author Name",
            "author@example.com",
            date,
            "Committer Name",
            "committer@example.com",
            date,
            date,
            $"Commit message {index}",
            [],
            $"https://gitlab.com/project/-/commit/commit-{index:000}");
    }

    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory;
        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            return Task.FromResult(_responseFactory(request));
        }
    }
}
