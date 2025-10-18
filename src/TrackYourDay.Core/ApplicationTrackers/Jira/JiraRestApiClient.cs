using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public interface IJiraRestApiClient
    {
        JiraUser GetCurrentUser();

        List<JiraIssueResponse> GetUserIssues(JiraUser jiraUser, DateTime startingFromDate);
    }

    public class JiraRestApiClient : IJiraRestApiClient
    {
        private readonly HttpClient httpClient;

        public JiraRestApiClient(string url, string personalAccessToken)
        {
            this.httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
            this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {personalAccessToken}");
        }

        public JiraUser GetCurrentUser()
        {
            var response = httpClient.GetAsync("/rest/api/2/myself").Result;
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            return JsonSerializer.Deserialize<JiraUser>(content);
        }

        public List<JiraIssueResponse> GetUserIssues(JiraUser jiraUser, DateTime startingFromDate)
        {
            var response = httpClient.GetAsync($"/rest/api/2/search?jql=assignee=alalak AND updated>={startingFromDate:yyyy-MM-dd}&expand=changelog").Result;
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
                        
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JiraDateTimeOffsetConverter());
            
            var searchResult = JsonSerializer.Deserialize<JiraSearchResponse>(content, options);
            return searchResult?.Issues ?? new List<JiraIssueResponse>();
        }
    }

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
        [property: JsonPropertyName("status")] JiraStatusResponse? Status,
        [property: JsonPropertyName("assignee")] JiraUserResponse? Assignee
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
        public static IJiraRestApiClient Create(JiraSettings settings)
        {
            if (string.IsNullOrEmpty(settings.ApiUrl))
            {
                return new NullJiraRestApiClient();
            }

            return new JiraRestApiClient(settings.ApiUrl, settings.ApiKey);
        }
    }

    public class NullJiraRestApiClient : IJiraRestApiClient
    {
        public JiraUser GetCurrentUser() => new JiraUser("Not recognized", "Not recognized");

        public List<JiraIssueResponse> GetUserIssues(JiraUser jiraUser, DateTime startingFromDate)
            => new List<JiraIssueResponse>();
    }
}