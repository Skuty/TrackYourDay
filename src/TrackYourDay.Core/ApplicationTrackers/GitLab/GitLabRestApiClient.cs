using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public interface IGitLabRestApiClient
    {
        Task<GitLabUser> GetCurrentUser();
        Task<List<GitLabEvent>> GetUserEvents(GitLabUserId userId, DateOnly startingFromDate);
        Task<GitLabProject> GetProject(GitLabProjectId projectId);
        Task<List<GitLabCommit>> GetCommits(GitLabProjectId projectId, GitLabRefName refName, DateOnly startingFromDate);
        Task<List<GitLabCommit>> GetCommitsByShaRange(GitLabProjectId projectId, string commitFromSha, string commitToSha);
        Task<List<Models.GitLabArtifact>> GetAssignedIssues(GitLabUserId userId);
        Task<List<Models.GitLabArtifact>> GetAssignedMergeRequests(GitLabUserId userId);
        Task<List<Models.GitLabArtifact>> GetCreatedMergeRequests(GitLabUserId userId);
    }

    public class GitLabRestApiClient : IGitLabRestApiClient
    {
        private readonly HttpClient _httpClient;
        private const int PAGE_LIMIT = 100; // GitLab API supports up to 100 items per page

        public GitLabRestApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GitLabUser> GetCurrentUser()
        {
            var response = await _httpClient.GetAsync($"/api/v4/user");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitLabUser>(content);
        }

        //TODO: implement stream reading approach with last readed event, use Id property
        public async Task<List<GitLabEvent>> GetUserEvents(GitLabUserId userId, DateOnly startingFromDate)
        {
            var allEvents = new List<GitLabEvent>();
            int page = 1;
            bool hasMoreEvents;

            do
            {
                var response = await _httpClient.GetAsync($"/api/v4/users/{userId.Id}/events?per_page={PAGE_LIMIT}&page={page}&after={startingFromDate.AddDays(-1).ToString("yyyy-MM-dd")}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<List<GitLabEvent>>(content) ?? new List<GitLabEvent>();

                allEvents.AddRange(events);
                hasMoreEvents = events.Count == PAGE_LIMIT;
                page++;
            } while (hasMoreEvents);

            return allEvents;
        }

        public async Task<GitLabProject> GetProject(GitLabProjectId projectId)
        {
            var response = await _httpClient.GetAsync($"/api/v4/projects/{projectId.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<GitLabProject>(content);
        }

        public async Task<List<GitLabCommit>> GetCommits(GitLabProjectId projectId, GitLabRefName refName, DateOnly startingFromDate)
        {
            var response = await _httpClient.GetAsync($"/api/v4/projects/{projectId.Id}/repository/commits?ref_name={refName.Name}&since={startingFromDate.AddDays(-1).ToString("yyyy-MM-dd")}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<GitLabCommit>>(content) ?? new List<GitLabCommit>();
        }

        public async Task<List<GitLabCommit>> GetCommitsByShaRange(GitLabProjectId projectId, string commitFromSha, string commitToSha)
        {
            // Use GitLab's compare API to get commits between two SHAs
            var response = await _httpClient.GetAsync($"/api/v4/projects/{projectId.Id}/repository/compare?from={commitFromSha}&to={commitToSha}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var comparison = JsonSerializer.Deserialize<GitLabComparison>(content);
            return comparison?.Commits ?? new List<GitLabCommit>();
        }

        public async Task<List<Models.GitLabArtifact>> GetAssignedIssues(GitLabUserId userId)
        {
            var allArtifacts = new List<Models.GitLabArtifact>();
            int page = 1;
            bool hasMoreIssues;

            do
            {
                var response = await _httpClient.GetAsync($"/api/v4/issues?assignee_id={userId.Id}&state=opened&per_page={PAGE_LIMIT}&page={page}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var issues = JsonSerializer.Deserialize<List<GitLabIssueDto>>(content) ?? new List<GitLabIssueDto>();

                allArtifacts.AddRange(issues.Select(i => new Models.GitLabArtifact
                {
                    Id = i.Id,
                    Iid = i.Iid,
                    ProjectId = i.ProjectId,
                    Title = i.Title,
                    Description = i.Description,
                    Type = Models.GitLabArtifactType.Issue,
                    State = i.State,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    WebUrl = i.WebUrl,
                    AuthorUsername = i.Author.Username,
                    AssigneeUsernames = i.Assignees.Select(a => a.Username).ToList()
                }));

                hasMoreIssues = issues.Count == PAGE_LIMIT;
                page++;
            } while (hasMoreIssues);

            return allArtifacts;
        }

        public async Task<List<Models.GitLabArtifact>> GetAssignedMergeRequests(GitLabUserId userId)
        {
            var allArtifacts = new List<Models.GitLabArtifact>();
            int page = 1;
            bool hasMoreMergeRequests;

            do
            {
                var response = await _httpClient.GetAsync($"/api/v4/merge_requests?assignee_id={userId.Id}&state=opened&per_page={PAGE_LIMIT}&page={page}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var mergeRequests = JsonSerializer.Deserialize<List<GitLabMergeRequestDto>>(content) ?? new List<GitLabMergeRequestDto>();

                allArtifacts.AddRange(mergeRequests.Select(mr => new Models.GitLabArtifact
                {
                    Id = mr.Id,
                    Iid = mr.Iid,
                    ProjectId = mr.ProjectId,
                    Title = mr.Title,
                    Description = mr.Description,
                    Type = Models.GitLabArtifactType.MergeRequest,
                    State = mr.State,
                    CreatedAt = mr.CreatedAt,
                    UpdatedAt = mr.UpdatedAt,
                    WebUrl = mr.WebUrl,
                    AuthorUsername = mr.Author.Username,
                    AssigneeUsernames = mr.Assignees.Select(a => a.Username).ToList()
                }));

                hasMoreMergeRequests = mergeRequests.Count == PAGE_LIMIT;
                page++;
            } while (hasMoreMergeRequests);

            return allArtifacts;
        }

        public async Task<List<Models.GitLabArtifact>> GetCreatedMergeRequests(GitLabUserId userId)
        {
            var allArtifacts = new List<Models.GitLabArtifact>();
            int page = 1;
            bool hasMoreMergeRequests;

            do
            {
                var response = await _httpClient.GetAsync($"/api/v4/merge_requests?author_id={userId.Id}&state=opened&per_page={PAGE_LIMIT}&page={page}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var mergeRequests = JsonSerializer.Deserialize<List<GitLabMergeRequestDto>>(content) ?? new List<GitLabMergeRequestDto>();

                allArtifacts.AddRange(mergeRequests.Select(mr => new Models.GitLabArtifact
                {
                    Id = mr.Id,
                    Iid = mr.Iid,
                    ProjectId = mr.ProjectId,
                    Title = mr.Title,
                    Description = mr.Description,
                    Type = Models.GitLabArtifactType.MergeRequest,
                    State = mr.State,
                    CreatedAt = mr.CreatedAt,
                    UpdatedAt = mr.UpdatedAt,
                    WebUrl = mr.WebUrl,
                    AuthorUsername = mr.Author.Username,
                    AssigneeUsernames = mr.Assignees.Select(a => a.Username).ToList()
                }));

                hasMoreMergeRequests = mergeRequests.Count == PAGE_LIMIT;
                page++;
            } while (hasMoreMergeRequests);

            return allArtifacts;
        }
    }
    public class NullGitLabRestApiClient : IGitLabRestApiClient
    {
        public Task<GitLabUser> GetCurrentUser() => Task.FromResult(new GitLabUser(
            Id: 0,
            Username: "Not recognized",
            Email: string.Empty,
            Name: string.Empty,
            State: "inactive",
            AvatarUrl: null,
            WebUrl: string.Empty,
            CreatedAt: DateTimeOffset.MinValue,
            Bio: null,
            Location: null,
            PublicEmail: null,
            Skype: null,
            LinkedIn: null,
            Twitter: null,
            WebsiteUrl: null,
            Organization: null,
            LastSignInAt: null,
            ConfirmedAt: null,
            LastActivityOn: null,
            EmailVerified: null,
            ThemeId: null,
            ColorSchemeId: null,
            ProjectsLimit: null,
            CurrentSignInAt: null,
            Identities: null,
            CanCreateGroup: null,
            CanCreateProject: null,
            TwoFactorEnabled: null,
            External: null));

        public Task<List<GitLabEvent>> GetUserEvents(GitLabUserId userId, DateOnly startingFromDate)
            => Task.FromResult(new List<GitLabEvent>());

        public Task<GitLabProject> GetProject(GitLabProjectId projectId)
            => throw new NotSupportedException("GitLab is not configured");

        public Task<List<GitLabCommit>> GetCommits(GitLabProjectId projectId, GitLabRefName refName, DateOnly startingFromDate)
            => Task.FromResult(new List<GitLabCommit>());

        public Task<List<GitLabCommit>> GetCommitsByShaRange(GitLabProjectId projectId, string commitFromSha, string commitToSha)
            => Task.FromResult(new List<GitLabCommit>());

        public Task<List<Models.GitLabArtifact>> GetAssignedIssues(GitLabUserId userId)
            => Task.FromResult(new List<Models.GitLabArtifact>());

        public Task<List<Models.GitLabArtifact>> GetAssignedMergeRequests(GitLabUserId userId)
            => Task.FromResult(new List<Models.GitLabArtifact>());

        public Task<List<Models.GitLabArtifact>> GetCreatedMergeRequests(GitLabUserId userId)
            => Task.FromResult(new List<Models.GitLabArtifact>());
    }

    public class GitLabRestApiClientFactory
    {
        public static IGitLabRestApiClient Create(GitLabSettings settings, IHttpClientFactory httpClientFactory)
        {
            if (string.IsNullOrEmpty(settings.ApiUrl))
            {
                return new NullGitLabRestApiClient();
            }

            var httpClient = httpClientFactory.CreateClient("GitLab");
            return new GitLabRestApiClient(httpClient);
        }
    }

    public record GitLabUserId(long Id);

    public record GitLabEventId(long Id)
    {
        public static GitLabEventId None => new GitLabEventId(0);
    }

    public record GitLabUser(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
        [property: JsonPropertyName("web_url")] string WebUrl,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("bio")] string? Bio,
        [property: JsonPropertyName("location")] string? Location,
        [property: JsonPropertyName("public_email")] string? PublicEmail,
        [property: JsonPropertyName("skype")] string? Skype,
        [property: JsonPropertyName("linkedin")] string? LinkedIn,
        [property: JsonPropertyName("twitter")] string? Twitter,
        [property: JsonPropertyName("website_url")] string? WebsiteUrl,
        [property: JsonPropertyName("organization")] string? Organization,
        [property: JsonPropertyName("last_sign_in_at")] DateTimeOffset? LastSignInAt,
        [property: JsonPropertyName("confirmed_at")] DateTimeOffset? ConfirmedAt,
        [property: JsonPropertyName("last_activity_on")] DateTimeOffset? LastActivityOn,
        [property: JsonPropertyName("email_verified")] bool? EmailVerified,
        [property: JsonPropertyName("theme_id")] int? ThemeId,
        [property: JsonPropertyName("color_scheme_id")] int? ColorSchemeId,
        [property: JsonPropertyName("projects_limit")] int? ProjectsLimit,
        [property: JsonPropertyName("current_sign_in_at")] DateTimeOffset? CurrentSignInAt,
        [property: JsonPropertyName("identities")] List<GitLabIdentity>? Identities,
        [property: JsonPropertyName("can_create_group")] bool? CanCreateGroup,
        [property: JsonPropertyName("can_create_project")] bool? CanCreateProject,
        [property: JsonPropertyName("two_factor_enabled")] bool? TwoFactorEnabled,
        [property: JsonPropertyName("external")] bool? External);

    public record GitLabIdentity(
        [property: JsonPropertyName("provider")] string Provider,
        [property: JsonPropertyName("extern_uid")] string ExternUid);

    public record GitLabEventAuthor(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl);

    public record GitLabEvent(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("project_id")] long ProjectId,
        [property: JsonPropertyName("action_name")] string Action,
        [property: JsonPropertyName("target_type")] string TargetType,
        [property: JsonPropertyName("author")] GitLabEventAuthor Author,
        [property: JsonPropertyName("target_title")] string TargetTitle,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("push_data")] GitLabPushData? PushData = null,
        [property: JsonPropertyName("note")] GitLabNote? Note = null);

    public record GitLabPushData(
        [property: JsonPropertyName("commit_count")] int CommitCount,
        [property: JsonPropertyName("action")] string Action,
        [property: JsonPropertyName("ref_type")] string RefType,
        [property: JsonPropertyName("commit_from")] string? CommitFrom,
        [property: JsonPropertyName("commit_to")] string? CommitTo,
        [property: JsonPropertyName("ref")] string Ref);

    public record GitLabNote(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("noteable_type")] string NoteableType,
        [property: JsonPropertyName("noteable_id")] long NoteableId);

    public record GitLabProjectId(long Id);

    public record GitLabProject(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("default_branch")] string DefaultBranch,
        [property: JsonPropertyName("visibility")] string Visibility,
        [property: JsonPropertyName("ssh_url_to_repo")] string SshUrlToRepo,
        [property: JsonPropertyName("http_url_to_repo")] string HttpUrlToRepo,
        [property: JsonPropertyName("web_url")] string WebUrl,
        [property: JsonPropertyName("readme_url")] string? ReadmeUrl,
        [property: JsonPropertyName("tag_list")] List<string> TagList,
        [property: JsonPropertyName("owner")] GitLabUser Owner,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("name_with_namespace")] string NameWithNamespace,
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("path_with_namespace")] string PathWithNamespace,
        [property: JsonPropertyName("issues_enabled")] bool IssuesEnabled,
        [property: JsonPropertyName("open_issues_count")] int OpenIssuesCount,
        [property: JsonPropertyName("merge_requests_enabled")] bool MergeRequestsEnabled,
        [property: JsonPropertyName("jobs_enabled")] bool JobsEnabled,
        [property: JsonPropertyName("wiki_enabled")] bool WikiEnabled,
        [property: JsonPropertyName("snippets_enabled")] bool SnippetsEnabled,
        [property: JsonPropertyName("resolve_outdated_diff_discussions")] bool ResolveOutdatedDiffDiscussions,
        [property: JsonPropertyName("container_registry_enabled")] bool ContainerRegistryEnabled,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("last_activity_at")] DateTimeOffset LastActivityAt,
        [property: JsonPropertyName("creator_id")] long CreatorId,
        //[property: JsonPropertyName("namespace")] GitLabNameSpace Namespace,
        [property: JsonPropertyName("import_status")] string ImportStatus,
        [property: JsonPropertyName("archived")] bool Archived,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
        [property: JsonPropertyName("shared_runners_enabled")] bool SharedRunnersEnabled,
        [property: JsonPropertyName("forks_count")] int ForksCount,
        [property: JsonPropertyName("star_count")] int StarCount,
        [property: JsonPropertyName("runners_token")] string RunnersToken);

        public record GitLabRefName(string Name);

        public record GitLabCommit(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("short_id")] string ShortId,
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("author_name")] string AuthorName,
            [property: JsonPropertyName("author_email")] string AuthorEmail,
            [property: JsonPropertyName("authored_date")] DateTimeOffset AuthoredDate,
            [property: JsonPropertyName("committer_name")] string CommitterName,
            [property: JsonPropertyName("committer_email")] string CommitterEmail,
            [property: JsonPropertyName("committed_date")] DateTimeOffset CommittedDate,
            [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
            [property: JsonPropertyName("message")] string Message,
            [property: JsonPropertyName("parent_ids")] List<string> ParentIds,
            [property: JsonPropertyName("web_url")] string WebUrl);

        public record GitLabComparison(
            [property: JsonPropertyName("commit")] GitLabCommit? Commit,
            [property: JsonPropertyName("commits")] List<GitLabCommit> Commits,
            [property: JsonPropertyName("compare_timeout")] bool CompareTimeout,
            [property: JsonPropertyName("compare_same_ref")] bool CompareSameRef);

        internal record GitLabIssueDto(
            [property: JsonPropertyName("id")] long Id,
            [property: JsonPropertyName("iid")] long Iid,
            [property: JsonPropertyName("project_id")] long ProjectId,
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("description")] string? Description,
            [property: JsonPropertyName("state")] string State,
            [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
            [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
            [property: JsonPropertyName("author")] GitLabUser Author,
            [property: JsonPropertyName("assignees")] List<GitLabUser> Assignees,
            [property: JsonPropertyName("web_url")] string WebUrl);

        internal record GitLabMergeRequestDto(
            [property: JsonPropertyName("id")] long Id,
            [property: JsonPropertyName("iid")] long Iid,
            [property: JsonPropertyName("project_id")] long ProjectId,
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("description")] string? Description,
            [property: JsonPropertyName("state")] string State,
            [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
            [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
            [property: JsonPropertyName("author")] GitLabUser Author,
            [property: JsonPropertyName("assignees")] List<GitLabUser> Assignees,
            [property: JsonPropertyName("web_url")] string WebUrl);
}
