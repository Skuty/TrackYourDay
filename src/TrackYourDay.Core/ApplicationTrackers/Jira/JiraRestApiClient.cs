using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Net;
using System.Text;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public interface IJiraRestApiClient
    {
        Task<JiraUser> GetCurrentUser();

        Task<List<JiraIssueResponse>> GetUserIssues(JiraUser jiraUser, DateTime startingFromDate);

        Task<List<JiraWorklogResponse>> GetIssueWorklogs(string issueKey, DateTime startingFromDate);

        Task<List<JiraIssue>> GetIssues(string? issueFilterName, string? rawJql);

        Task CreateIssueWorklog(string issueKey, DateTime startedAt, int timeSpentSeconds, string comment);
    }

    public class JiraRestApiClient : IJiraRestApiClient
    {
        private readonly HttpClient _httpClient;

        public JiraRestApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JiraUser> GetCurrentUser()
        {
            var response = await _httpClient.GetAsync("/rest/api/2/myself");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var userResponse = JsonSerializer.Deserialize<JiraMyselfResponse>(content, options);
            return new JiraUser(userResponse?.Name ?? "Unknown", userResponse?.DisplayName ?? "Unknown", userResponse?.AccountId);
        }

        public async Task<List<JiraIssueResponse>> GetUserIssues(JiraUser jiraUser, DateTime startingFromDate)
        {
            var accountId = jiraUser.AccountId ?? jiraUser.DisplayName;
            var jql = $"assignee=\"{accountId}\" AND updated>=\"{startingFromDate:yyyy-MM-dd}\" AND status NOT IN (\"Backlog\", \"Resolved\", \"Done\", \"Canceled\")";
            var encodedJql = Uri.EscapeDataString(jql);
            var url = $"/rest/api/2/search?jql={encodedJql}&expand=changelog";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JiraDateTimeOffsetConverter());

            var searchResult = JsonSerializer.Deserialize<JiraSearchResponse>(content, options);
            return searchResult?.Issues ?? new List<JiraIssueResponse>();
        }

        public async Task<List<JiraWorklogResponse>> GetIssueWorklogs(string issueKey, DateTime startingFromDate)
        {
            var response = await _httpClient.GetAsync($"/rest/api/2/issue/{issueKey}/worklog");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JiraDateTimeOffsetConverter());

            var worklogResult = JsonSerializer.Deserialize<JiraWorklogListResponse>(content, options);

            // Filter worklogs by date
            return worklogResult?.Worklogs?
                .Where(w => w.Started >= startingFromDate)
                .ToList() ?? new List<JiraWorklogResponse>();
        }

        public async Task<List<JiraIssue>> GetIssues(string? issueFilterName, string? rawJql)
        {
            var effectiveJql = await ResolveJqlForIssueLookup(issueFilterName, rawJql).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(effectiveJql))
            {
                throw new InvalidOperationException("No Jira issue query available. Configure Jira Filter Name or Raw JQL in Settings.");
            }

            var encodedJql = Uri.EscapeDataString(effectiveJql);
            var url = $"/rest/api/2/search?jql={encodedJql}&fields=summary,updated&maxResults=100";

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            await EnsureSuccessWithDetails(response, url).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JiraDateTimeOffsetConverter());

            var searchResult = JsonSerializer.Deserialize<JiraSearchResponse>(content, options);
            return searchResult?.Issues?
                .Select(issue => new JiraIssue(
                    issue.Key,
                    issue.Fields.Summary ?? string.Empty,
                    issue.Fields.Updated.LocalDateTime))
                .OrderByDescending(issue => issue.Updated)
                .ToList() ?? [];
        }

        public async Task CreateIssueWorklog(string issueKey, DateTime startedAt, int timeSpentSeconds, string comment)
        {
            if (string.IsNullOrWhiteSpace(issueKey))
            {
                throw new ArgumentException("Issue key is required.", nameof(issueKey));
            }

            if (timeSpentSeconds <= 0)
            {
                throw new ArgumentException("Worklog duration must be greater than zero seconds.", nameof(timeSpentSeconds));
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new ArgumentException("Worklog comment is required.", nameof(comment));
            }

            var url = $"/rest/api/2/issue/{Uri.EscapeDataString(issueKey)}/worklog";
            var payload = new JiraCreateWorklogRequest(
                comment.Trim(),
                new DateTimeOffset(startedAt).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                timeSpentSeconds);
            var requestContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, requestContent).ConfigureAwait(false);
            await EnsureSuccessWithDetails(response, url).ConfigureAwait(false);
        }

        private async Task<string?> ResolveJqlForIssueLookup(string? issueFilterName, string? rawJql)
        {
            if (!string.IsNullOrWhiteSpace(issueFilterName))
            {
                var resolved = await TryResolveJqlFromFilterName(issueFilterName.Trim()).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    return resolved;
                }
            }

            if (!string.IsNullOrWhiteSpace(rawJql))
            {
                return rawJql.Trim();
            }

            return null;
        }

        private async Task<string?> TryResolveJqlFromFilterName(string filterName)
        {
            var encodedFilterName = Uri.EscapeDataString(filterName);
            var url = $"/rest/api/2/filter/search?filterName={encodedFilterName}";

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var searchResponse = JsonSerializer.Deserialize<JiraFilterSearchResponse>(content, options);

                var exactFilter = searchResponse?.Values?
                    .FirstOrDefault(filter => string.Equals(filter.Name, filterName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(exactFilter?.Jql))
                {
                    return exactFilter.Jql;
                }

                return searchResponse?.Values?.FirstOrDefault()?.Jql;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static async Task EnsureSuccessWithDetails(HttpResponseMessage response, string requestPath)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new JiraApiException(
                response.StatusCode,
                response.ReasonPhrase,
                requestPath,
                body);
        }
    }

    public sealed class JiraApiException : Exception
    {
        public JiraApiException(HttpStatusCode statusCode, string? reasonPhrase, string requestPath, string responseBody)
            : base($"Jira API call failed ({(int)statusCode} {reasonPhrase ?? "Unknown"}). Request: {requestPath}. Response: {responseBody}")
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            RequestPath = requestPath;
            ResponseBody = responseBody;
        }

        public HttpStatusCode StatusCode { get; }
        public string? ReasonPhrase { get; }
        public string RequestPath { get; }
        public string ResponseBody { get; }
    }

    public record JiraMyselfResponse(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("accountId")] string? AccountId
    );

    public record JiraSearchResponse(
        [property: JsonPropertyName("issues")] List<JiraIssueResponse>? Issues,
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("startAt")] int StartAt,
        [property: JsonPropertyName("maxResults")] int MaxResults
    );

    public record JiraIssueResponse(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("fields")] JiraIssueFieldsResponse Fields,
        [property: JsonPropertyName("changelog")] JiraChangelogResponse? Changelog);

    public record JiraIssueFieldsResponse(
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("updated")] DateTimeOffset Updated,
        [property: JsonPropertyName("created")] DateTimeOffset? Created,
        [property: JsonPropertyName("status")] JiraStatusResponse? Status,
        [property: JsonPropertyName("assignee")] JiraUserResponse? Assignee,
        [property: JsonPropertyName("creator")] JiraUserResponse? Creator,
        [property: JsonPropertyName("issuetype")] JiraIssueTypeResponse? IssueType,
        [property: JsonPropertyName("project")] JiraProjectResponse? Project,
        [property: JsonPropertyName("parent")] JiraParentIssueResponse? Parent
    );

    public record JiraIssueTypeResponse(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("subtask")] bool IsSubtask
    );

    public record JiraProjectResponse(
        [property: JsonPropertyName("key")] string? Key,
        [property: JsonPropertyName("name")] string? Name
    );

    public record JiraParentIssueResponse(
        [property: JsonPropertyName("key")] string? Key,
        [property: JsonPropertyName("fields")] JiraParentFieldsResponse? Fields
    );

    public record JiraParentFieldsResponse(
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("issuetype")] JiraIssueTypeResponse? IssueType
    );

    public record JiraStatusResponse(
        [property: JsonPropertyName("name")] string? Name
    );

    public record JiraUserResponse(
        [property: JsonPropertyName("displayName")] string? DisplayName
    );

    public record JiraChangelogResponse(
        [property: JsonPropertyName("histories")] List<JiraHistoryResponse>? Histories
    );

    public record JiraHistoryResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("author")] JiraAuthorResponse? Author,
        [property: JsonPropertyName("created")] DateTimeOffset Created,
        [property: JsonPropertyName("items")] List<JiraChangeItemResponse>? Items
    );

    public record JiraAuthorResponse(
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("accountId")] string? AccountId
    );

    public record JiraChangeItemResponse(
        [property: JsonPropertyName("field")] string? Field,
        [property: JsonPropertyName("fieldtype")] string? FieldType,
        [property: JsonPropertyName("from")] string? From,
        [property: JsonPropertyName("fromString")] string? FromString,
        [property: JsonPropertyName("to")] string? To,
        [property: JsonPropertyName("toString")] string? ToValue
    );

    public record JiraWorklogListResponse(
        [property: JsonPropertyName("worklogs")] List<JiraWorklogResponse>? Worklogs
    );

    public record JiraWorklogResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("author")] JiraUserResponse? Author,
        [property: JsonPropertyName("comment")] string? Comment,
        [property: JsonPropertyName("started")] DateTimeOffset Started,
        [property: JsonPropertyName("timeSpent")] string? TimeSpent,
        [property: JsonPropertyName("timeSpentSeconds")] int TimeSpentSeconds
    );

    public record JiraFilterSearchResponse(
        [property: JsonPropertyName("values")] List<JiraFilterResponse>? Values);

    public record JiraFilterResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("jql")] string? Jql);

    public record JiraCreateWorklogRequest(
        [property: JsonPropertyName("comment")] string Comment,
        [property: JsonPropertyName("started")] string Started,
        [property: JsonPropertyName("timeSpentSeconds")] int TimeSpentSeconds);

    public class JiraDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
            {
                throw new JsonException("Date string cannot be null or empty");
            }

            // Try to parse the Jira format: 2025-02-19T17:29:40.000+0100
            if (DateTimeOffset.TryParseExact(dateString, "yyyy-MM-ddTHH:mm:ss.fffzzz",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            // Fallback to default parsing
            if (DateTimeOffset.TryParse(dateString, out var fallbackResult))
            {
                return fallbackResult;
            }

            throw new JsonException($"Unable to parse date: {dateString}");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
        }
    }
    public class JiraRestApiClientFactory
    {
        public static IJiraRestApiClient Create(JiraSettings settings, IHttpClientFactory httpClientFactory)
        {
            if (string.IsNullOrEmpty(settings.ApiUrl))
            {
                // Log that Jira is not configured so it's obvious in startup logs
                return new NullJiraRestApiClient();
            }

            var httpClient = httpClientFactory.CreateClient("Jira");
            return new JiraRestApiClient(httpClient);
        }
    }

    public class NullJiraRestApiClient : IJiraRestApiClient
    {
        public Task<JiraUser> GetCurrentUser() => Task.FromResult(new JiraUser("Not recognized", "Not recognized", null));

        public Task<List<JiraIssueResponse>> GetUserIssues(JiraUser jiraUser, DateTime startingFromDate)
            => Task.FromResult(new List<JiraIssueResponse>());

        public Task<List<JiraWorklogResponse>> GetIssueWorklogs(string issueKey, DateTime startingFromDate)
            => Task.FromResult(new List<JiraWorklogResponse>());

        public Task<List<JiraIssue>> GetIssues(string? issueFilterName, string? rawJql)
            => Task.FromResult(new List<JiraIssue>());

        public Task CreateIssueWorklog(string issueKey, DateTime startedAt, int timeSpentSeconds, string comment)
            => throw new InvalidOperationException("Jira integration is not configured.");
    }
}