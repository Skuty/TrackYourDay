// src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.LlmPrompts;

public class LlmPromptService(
    IGenericSettingsRepository settingsRepository,
    IHistoricalDataRepository<JiraActivity> jiraActivityRepository,
    IHistoricalDataRepository<GitLabActivity> gitLabActivityRepository,
    IJiraIssueRepository jiraIssueRepository,
    IGitLabStateRepository gitLabStateRepository,
    ILogger<LlmPromptService> logger) : ILlmPromptService
{
    private const int AverageRowBytes = 100;
    private const string KeyPrefix = "llm_template:";

    public async Task<string> GeneratePrompt(string templateKey, DateOnly date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var template = GetTemplateByKey(templateKey)
            ?? throw new InvalidOperationException($"Template '{templateKey}' not found");

        var prompt = template.SystemPrompt;

        if (prompt.Contains(LlmPromptTemplate.JiraActivitiesPlaceholder, StringComparison.Ordinal))
        {
            var jiraMarkdown = await GetJiraActivitiesMarkdown(date).ConfigureAwait(false);
            prompt = prompt.Replace(LlmPromptTemplate.JiraActivitiesPlaceholder, jiraMarkdown);
        }

        if (prompt.Contains(LlmPromptTemplate.GitLabActivitiesPlaceholder, StringComparison.Ordinal))
        {
            var gitLabMarkdown = await GetGitLabActivitiesMarkdown(date).ConfigureAwait(false);
            prompt = prompt.Replace(LlmPromptTemplate.GitLabActivitiesPlaceholder, gitLabMarkdown);
        }

        if (prompt.Contains(LlmPromptTemplate.CurrentlyAssignedIssuesPlaceholder, StringComparison.Ordinal))
        {
            var assignedMarkdown = await GetCurrentlyAssignedIssuesMarkdown().ConfigureAwait(false);
            prompt = prompt.Replace(LlmPromptTemplate.CurrentlyAssignedIssuesPlaceholder, assignedMarkdown);
        }

        logger.LogInformation("Generated prompt for {TemplateKey} on {Date}: {CharCount} characters",
            templateKey, date, prompt.Length);

        return prompt;
    }

    public IReadOnlyList<LlmPromptTemplate> GetActiveTemplates()
    {
        var templates = new List<LlmPromptTemplate>();
        var keys = settingsRepository.GetAllKeys()
            .Where(k => k.StartsWith(KeyPrefix, StringComparison.Ordinal));

        foreach (var key in keys)
        {
            var json = settingsRepository.GetSetting(key);
            if (json != null)
            {
                var template = JsonConvert.DeserializeObject<LlmPromptTemplate>(json);
                if (template != null && template.IsActive)
                {
                    templates.Add(template);
                }
            }
        }

        return templates.OrderBy(t => t.DisplayOrder).ToList();
    }

    private async Task<string> GetJiraActivitiesMarkdown(DateOnly date)
    {
        try
        {
            var specification = new DateRangeSpecification<JiraActivity>(date, date);
            var activities = await jiraActivityRepository.FindAsync(specification, CancellationToken.None)
                .ConfigureAwait(false);

            if (activities.Count == 0)
                return "No data available";

            var sb = new StringBuilder(activities.Count * AverageRowBytes + 200);
            sb.AppendLine("| Time | Activity Description |");
            sb.AppendLine("|------|----------------------|");

            foreach (var activity in activities.OrderBy(a => a.OccurrenceDate))
            {
                var time = activity.OccurrenceDate.ToString("HH:mm");
                var description = EscapeMarkdown(activity.Description);
                sb.AppendLine($"| {time} | {description} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Jira activities for {Date}", date);
            return "No data available";
        }
    }

    private async Task<string> GetGitLabActivitiesMarkdown(DateOnly date)
    {
        try
        {
            var specification = new DateRangeSpecification<GitLabActivity>(date, date);
            var activities = await gitLabActivityRepository.FindAsync(specification, CancellationToken.None)
                .ConfigureAwait(false);

            if (activities.Count == 0)
                return "No data available";

            var sb = new StringBuilder(activities.Count * AverageRowBytes + 200);
            sb.AppendLine("| Time | Activity Description |");
            sb.AppendLine("|------|----------------------|");

            foreach (var activity in activities.OrderBy(a => a.OccuranceDate))
            {
                var time = activity.OccuranceDate.ToString("HH:mm");
                var description = EscapeMarkdown(activity.Description);
                sb.AppendLine($"| {time} | {description} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch GitLab activities for {Date}", date);
            return "No data available";
        }
    }

    private async Task<string> GetCurrentlyAssignedIssuesMarkdown()
    {
        try
        {
            var sb = new StringBuilder(2048);
            bool hasAnyData = false;

            var jiraIssues = await jiraIssueRepository.GetCurrentIssuesAsync(CancellationToken.None)
                .ConfigureAwait(false);

            if (jiraIssues.Count > 0)
            {
                if (!hasAnyData)
                {
                    hasAnyData = true;
                }

                sb.AppendLine("| Key | Summary | Status | Project | Updated |");
                sb.AppendLine("|-----|---------|--------|---------|---------|");

                foreach (var issue in jiraIssues.OrderByDescending(i => i.Updated))
                {
                    var summary = EscapeMarkdown(issue.Summary);
                    var key = issue.BrowseUrl != "#" ? $"[{issue.Key}]({issue.BrowseUrl})" : issue.Key;
                    sb.AppendLine($"| {key} | {summary} | {issue.Status} | {issue.ProjectKey} | {issue.Updated:yyyy-MM-dd HH:mm} |");
                }

                sb.AppendLine();
            }

            var gitLabSnapshot = await gitLabStateRepository.GetLatestAsync(CancellationToken.None)
                .ConfigureAwait(false);

            if (gitLabSnapshot?.Artifacts.Count > 0)
            {
                if (!hasAnyData)
                {
                    hasAnyData = true;
                }
                else
                {
                    sb.AppendLine();
                }

                sb.AppendLine("| Type | Title | State | Updated |");
                sb.AppendLine("|------|-------|-------|---------|");

                foreach (var artifact in gitLabSnapshot.Artifacts.OrderByDescending(a => a.UpdatedAt))
                {
                    var title = EscapeMarkdown(artifact.Title);
                    var type = artifact.Type == GitLabArtifactType.MergeRequest ? "MR" : "Issue";
                    var titleWithLink = $"[{title}]({artifact.WebUrl})";
                    sb.AppendLine($"| {type} | {titleWithLink} | {artifact.State} | {artifact.UpdatedAt:yyyy-MM-dd HH:mm} |");
                }

                sb.AppendLine();
            }

            return hasAnyData ? sb.ToString() : "No data available";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch currently assigned issues");
            return "No data available";
        }
    }

    private static string EscapeMarkdown(string text)
        => text.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");

    private LlmPromptTemplate? GetTemplateByKey(string templateKey)
    {
        var key = GetStorageKey(templateKey);
        var json = settingsRepository.GetSetting(key);
        return json != null ? JsonConvert.DeserializeObject<LlmPromptTemplate>(json) : null;
    }

    private static string GetStorageKey(string templateKey) => $"{KeyPrefix}{templateKey}";
}
