using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    /// <summary>
    /// Strategy that enriches activity grouping with Jira issue information.
    /// Matches activities to Jira issues assigned to the user by:
    /// - Direct Jira key detection in activity descriptions
    /// - Semantic matching between activity descriptions and issue summaries
    /// - Temporal correlation between activities and issue updates
    /// </summary>
    public class JiraEnrichedSummaryStrategy : ISummaryStrategy
    {
        private readonly ILogger<JiraEnrichedSummaryStrategy> _logger;
        private readonly JiraTracker _jiraTracker;
        private static readonly Regex JiraKeyRegex = new(@"[A-Z][A-Z0-9]+-\d+", RegexOptions.Compiled);
        
        // Temporal window for matching activities to Jira updates (in minutes)
        private const int TemporalWindowMinutes = 30;
        
        // Minimum word overlap score to consider activity related to Jira issue
        private const float MinimumSimilarityThreshold = 0.3f;

        public JiraEnrichedSummaryStrategy(
            JiraTracker jiraTracker,
            ILogger<JiraEnrichedSummaryStrategy> logger)
        {
            _jiraTracker = jiraTracker ?? throw new ArgumentNullException(nameof(jiraTracker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string StrategyName => "Jira-Enriched Activity Groups";

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<TrackedActivity> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            
            var itemsList = items.ToList();
            if (!itemsList.Any())
            {
                _logger.LogInformation("No items to generate summary for.");
                return Array.Empty<GroupedActivity>();
            }

            // Get Jira activities for enrichment
            // Note: This fetches and caches results from JiraTracker asynchronously
            var jiraActivities = _jiraTracker.GetJiraActivitiesAsync().GetAwaiter().GetResult().ToList();
            _logger.LogInformation("Retrieved {Count} Jira activities for enrichment", jiraActivities.Count);

            // Group items by date
            var itemsByDate = itemsList
                .GroupBy(a => DateOnly.FromDateTime(a.StartDate.Date))
                .OrderBy(g => g.Key);

            var result = new List<GroupedActivity>();

            foreach (var dailyItems in itemsByDate)
            {
                var date = dailyItems.Key;
                var dailyJiraActivities = jiraActivities
                    .Where(ja => DateOnly.FromDateTime(ja.OccurrenceDate) == date)
                    .ToList();

                var groups = ProcessDailyItems(dailyItems.ToList(), dailyJiraActivities, date);
                result.AddRange(groups.Values);
            }

            return result.AsReadOnly();
        }

        private Dictionary<string, GroupedActivity> ProcessDailyItems(
            List<TrackedActivity> items,
            List<JiraActivity> jiraActivities,
            DateOnly date)
        {
            var groups = new Dictionary<string, GroupedActivity>();
            var unmatchedItems = new List<TrackedActivity>();

            // First pass: Match items with explicit Jira keys
            foreach (var item in items)
            {
                var description = item.GetDescription();
                var match = JiraKeyRegex.Match(description);
                
                if (match.Success)
                {
                    var jiraKey = match.Value;
                    var enrichedDescription = GetEnrichedDescription(jiraKey, description, jiraActivities);
                    
                    if (!groups.TryGetValue(jiraKey, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, enrichedDescription);
                        groups[jiraKey] = group;
                    }
                    group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
                }
                else
                {
                    unmatchedItems.Add(item);
                }
            }

            // Second pass: Try to match unmatched items using semantic similarity and temporal proximity
            foreach (var item in unmatchedItems)
            {
                var matchedJiraKey = FindBestJiraMatch(item, jiraActivities);
                
                if (matchedJiraKey != null)
                {
                    var enrichedDescription = GetEnrichedDescription(matchedJiraKey, item.GetDescription(), jiraActivities);
                    
                    if (!groups.TryGetValue(matchedJiraKey, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, enrichedDescription);
                        groups[matchedJiraKey] = group;
                    }
                    group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
                    
                    _logger.LogDebug("Matched item '{Description}' to Jira {Key} via similarity/temporal matching",
                        item.GetDescription(), matchedJiraKey);
                }
                else
                {
                    // No Jira match found - group under original description
                    var key = $"Other: {item.GetDescription()}";
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = GroupedActivity.CreateEmptyWithDescriptionForDate(date, item.GetDescription());
                        groups[key] = group;
                    }
                    group.Include(item.Guid, new TimePeriod(item.StartDate, item.EndDate));
                }
            }

            return groups;
        }

        private string? FindBestJiraMatch(TrackedActivity item, List<JiraActivity> jiraActivities)
        {
            if (!jiraActivities.Any())
                return null;

            var itemDescription = item.GetDescription();
            var bestMatch = jiraActivities
                .Select(ja =>
                {
                    // Extract Jira key from Jira activity description
                    var jiraKey = ExtractJiraKeyFromJiraActivity(ja.Description);
                    if (jiraKey == null)
                        return (JiraKey: (string?)null, Score: 0f);

                    // Calculate semantic similarity
                    var semanticScore = CalculateSemanticSimilarity(itemDescription, ja.Description);
                    
                    // Calculate temporal proximity score
                    var timeDiff = Math.Abs((item.StartDate - ja.OccurrenceDate).TotalMinutes);
                    var temporalScore = timeDiff <= TemporalWindowMinutes 
                        ? 1.0f - (float)(timeDiff / TemporalWindowMinutes) 
                        : 0f;
                    
                    // Combined score (weighted average)
                    var combinedScore = (semanticScore * 0.7f) + (temporalScore * 0.3f);
                    
                    return (JiraKey: jiraKey, Score: combinedScore);
                })
                .Where(m => m.JiraKey != null && m.Score > MinimumSimilarityThreshold)
                .OrderByDescending(m => m.Score)
                .FirstOrDefault();

            return bestMatch.JiraKey;
        }

        private string? ExtractJiraKeyFromJiraActivity(string jiraActivityDescription)
        {
            // Jira activities have format: "Jira Issue Updated - KEY-123: Summary | ..."
            var match = JiraKeyRegex.Match(jiraActivityDescription);
            return match.Success ? match.Value : null;
        }

        private float CalculateSemanticSimilarity(string activityDescription, string jiraDescription)
        {
            if (string.IsNullOrWhiteSpace(activityDescription) || string.IsNullOrWhiteSpace(jiraDescription))
                return 0f;

            // Normalize and tokenize
            var activityWords = Tokenize(activityDescription);
            var jiraWords = Tokenize(jiraDescription);

            if (!activityWords.Any() || !jiraWords.Any())
                return 0f;

            // Count common words
            var commonWords = activityWords.Intersect(jiraWords).Count();
            
            if (commonWords == 0)
                return 0f;

            // Use Dice coefficient for similarity
            var diceCoefficient = (2.0f * commonWords) / (activityWords.Count + jiraWords.Count);
            
            return diceCoefficient;
        }

        private HashSet<string> Tokenize(string text)
        {
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '-', '_', ':', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2) // Filter out very short words
                .Where(w => !IsCommonStopWord(w))
                .ToHashSet();
        }

        private bool IsCommonStopWord(string word)
        {
            var stopWords = new HashSet<string> 
            { 
                "the", "and", "for", "with", "from", "this", "that", "jira", "issue", "updated" 
            };
            return stopWords.Contains(word);
        }

        private string GetEnrichedDescription(string jiraKey, string originalDescription, List<JiraActivity> jiraActivities)
        {
            // Try to find the Jira activity with this key to get full summary
            var jiraActivity = jiraActivities.FirstOrDefault(ja => ja.Description.Contains(jiraKey));
            
            if (jiraActivity != null)
            {
                // Extract summary from Jira activity description
                // Format: "Jira Issue Updated - KEY-123: Summary | Updated: ... | Issue ID: ..."
                var summaryMatch = Regex.Match(jiraActivity.Description, $@"{jiraKey}:\s*(.+?)\s*\|");
                if (summaryMatch.Success && summaryMatch.Groups.Count > 1)
                {
                    var summary = summaryMatch.Groups[1].Value.Trim();
                    return $"{jiraKey}: {summary}";
                }
            }
            
            // Fallback to original if no Jira info found
            return originalDescription.Contains(jiraKey) ? originalDescription : $"{jiraKey}: {originalDescription}";
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
