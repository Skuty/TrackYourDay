using System.Net.Http;
using System.Text.Json;

namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public interface IJiraRestApiClient
    {
        JiraUser GetCurrentUser();
        List<JiraIssue> GetUserIssues(string userId, DateTime startingFromDate);
    }

    public class JiraRestApiClient : IJiraRestApiClient
    {
        private readonly HttpClient httpClient;

        public JiraRestApiClient(string url, string apiKey)
        {
            this.httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
            this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public JiraUser GetCurrentUser()
        {
            var response = httpClient.GetAsync("/rest/api/3/myself").Result;
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            return JsonSerializer.Deserialize<JiraUser>(content);
        }

        public List<JiraIssue> GetUserIssues(string userId, DateTime startingFromDate)
        {
            var response = httpClient.GetAsync($"/rest/api/3/search?jql=assignee={userId} AND updated>={startingFromDate:yyyy-MM-dd}").Result;
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            return JsonSerializer.Deserialize<List<JiraIssue>>(content);
        }
    }

    public record JiraUser(string AccountId, string DisplayName);
    public record JiraIssue(string Key, string Summary, DateTime Updated);
}