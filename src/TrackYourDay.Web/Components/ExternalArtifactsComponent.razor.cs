using Microsoft.AspNetCore.Components;
using MudBlazor;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.Web.Components;

public partial class ExternalArtifactsComponent : IDisposable
{
    [Inject] private IJiraIssueRepository? JiraRepository { get; set; }
    [Inject] private IGitLabStateRepository? GitLabStateRepository { get; set; }
    [Inject] private IJiraSettingsService? JiraSettingsService { get; set; }
    [Inject] private IClock? Clock { get; set; }

    private List<JiraIssueState> _jiraIssues = [];
    private List<GitLabArtifact> _gitLabArtifacts = [];
    private bool _isLoading = true;
    private System.Threading.Timer? _refreshTimer;
    private string? _jiraBaseUrl;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
        
        // Refresh every 30 minutes
        _refreshTimer = new System.Threading.Timer(
            async _ => await InvokeAsync(async () =>
            {
                await LoadDataAsync();
                StateHasChanged();
            }),
            null,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(30));
    }

    private async Task RefreshAsync()
    {
        _isLoading = true;
        StateHasChanged();
        await LoadDataAsync();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        
        try
        {
            // Load Jira issues
            if (JiraRepository != null)
            {
                var jiraIssues = await JiraRepository.GetCurrentIssuesAsync(CancellationToken.None);
                _jiraIssues = jiraIssues.ToList();
            }

            // Load GitLab artifacts
            if (GitLabStateRepository != null)
            {
                var snapshot = await GitLabStateRepository.GetLatestAsync(CancellationToken.None);
                _gitLabArtifacts = snapshot?.Artifacts ?? [];
            }

            // Load Jira base URL for constructing issue links
            if (JiraSettingsService != null)
            {
                var settings = JiraSettingsService.GetSettings();
                _jiraBaseUrl = settings?.ApiUrl;
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static Color GetStatusColor(string status) => status.ToLowerInvariant() switch
    {
        "done" or "closed" or "resolved" => Color.Success,
        "in progress" or "in review" => Color.Info,
        "to do" or "open" or "backlog" => Color.Default,
        "blocked" => Color.Error,
        _ => Color.Default
    };

    private static Color GetGitLabStateColor(string state) => state.ToLowerInvariant() switch
    {
        "merged" or "closed" => Color.Success,
        "opened" or "open" => Color.Info,
        "locked" => Color.Warning,
        _ => Color.Default
    };

    private static string GetGitLabIcon(GitLabArtifactType type) => type switch
    {
        GitLabArtifactType.MergeRequest => Icons.Material.Filled.MergeType,
        GitLabArtifactType.Issue => Icons.Material.Filled.ReportProblem,
        _ => Icons.Material.Filled.Description
    };

    private string FormatTimeAgo(DateTimeOffset dateTime)
    {
        if (Clock == null) return dateTime.ToString("g");
        
        var now = Clock.Now;
        var diff = now - dateTime;

        return diff.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{(int)diff.TotalMinutes}m ago",
            < 1440 => $"{(int)diff.TotalHours}h ago",
            < 43200 => $"{(int)diff.TotalDays}d ago",
            _ => dateTime.ToString("MMM d, yyyy")
        };
    }

    private string GetJiraIssueUrl(string issueKey)
    {
        // If Jira base URL is configured, construct the full URL
        // ApiUrl typically ends with /rest/api/3, so we need to extract base
        if (!string.IsNullOrEmpty(_jiraBaseUrl))
        {
            var baseUrl = _jiraBaseUrl.Replace("/rest/api/3", "")
                                       .Replace("/rest/api/2", "")
                                       .TrimEnd('/');
            return $"{baseUrl}/browse/{issueKey}";
        }
        
        // Fallback: returns placeholder anchor link (won't navigate)
        return $"#";
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
