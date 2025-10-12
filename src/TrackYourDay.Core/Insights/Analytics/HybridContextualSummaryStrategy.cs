using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    /// <summary>
    /// Hybrid strategy that combines multiple techniques for intelligent activity grouping:
    /// 1. Jira key extraction (highest priority)
    /// 2. Semantic similarity clustering
    /// 3. Temporal proximity analysis (work sessions)
    /// 4. Context-based categorization
    /// 5. Activity pattern recognition
    /// </summary>
    public class HybridContextualSummaryStrategy : ISummaryStrategy
    {
        private readonly ILogger<HybridContextualSummaryStrategy> _logger;
        private static readonly Regex JiraKeyRegex = new(@"[A-Z][A-Z0-9]+-\d+", RegexOptions.Compiled);
        
        // Time gap threshold for session detection (in minutes)
        private const int SessionGapMinutes = 15;
        
        // Minimum similarity to group activities together
        private const float MinimumSemanticSimilarity = 0.35f;
        
        private static readonly Dictionary<string, string[]> ContextKeywords = new()
        {
            { "Development", new[] { "code", "coding", "develop", "implement", "programming", "refactor", "debug" } },
            { "Bug Fixing", new[] { "bug", "fix", "error", "issue", "defect", "crash", "exception" } },
            { "Testing", new[] { "test", "testing", "qa", "verify", "validation", "assert", "unit test" } },
            { "Code Review", new[] { "review", "pull request", "pr", "merge", "approve", "feedback" } },
            { "Meeting", new[] { "meeting", "sync", "standup", "call", "conference", "discussion" } },
            { "Documentation", new[] { "doc", "documentation", "readme", "wiki", "comment", "notes" } },
            { "Research", new[] { "research", "investigate", "explore", "study", "learn", "stackoverflow" } },
            { "Planning", new[] { "plan", "planning", "design", "architecture", "estimate", "sprint" } },
            { "Communication", new[] { "email", "slack", "teams", "chat", "message", "respond" } }
        };

        public HybridContextualSummaryStrategy(ILogger<HybridContextualSummaryStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string StrategyName => "Hybrid Contextual Groups";

        public IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<EndedActivity> activities)
        {
            if (activities == null) throw new ArgumentNullException(nameof(activities));
            
            var activitiesList = activities.ToList();
            if (!activitiesList.Any())
            {
                _logger.LogInformation("No activities to generate summary for.");
                return Array.Empty<GroupedActivity>();
            }

            // Group activities by date
            var activitiesByDate = activitiesList
                .GroupBy(a => DateOnly.FromDateTime(a.StartDate.Date))
                .OrderBy(g => g.Key);

            var result = new List<GroupedActivity>();

            foreach (var dailyActivities in activitiesByDate)
            {
                var date = dailyActivities.Key;
                var sortedActivities = dailyActivities.OrderBy(a => a.StartDate).ToList();
                
                var groups = ProcessActivitiesWithHybridApproach(sortedActivities, date);
                result.AddRange(groups);
            }

            return result.AsReadOnly();
        }

        private List<GroupedActivity> ProcessActivitiesWithHybridApproach(
            List<EndedActivity> activities,
            DateOnly date)
        {
            var activityGroups = new List<ActivityGroup>();

            foreach (var activity in activities)
            {
                var description = activity.GetDescription();
                
                // Step 1: Check for Jira key (highest priority)
                var jiraKey = ExtractJiraKey(description);
                
                if (jiraKey != null)
                {
                    AddToJiraGroup(activityGroups, activity, jiraKey, date);
                }
                else
                {
                    // Step 2: Try to merge with existing groups using multiple criteria
                    var matchedGroup = FindBestMatchingGroup(activityGroups, activity);
                    
                    if (matchedGroup != null)
                    {
                        matchedGroup.AddActivity(activity);
                        _logger.LogDebug("Merged activity '{Description}' into existing group '{Group}'",
                            description, matchedGroup.Description);
                    }
                    else
                    {
                        // Step 3: Create new group
                        var context = DetermineContext(description);
                        var groupDescription = CreateGroupDescription(description, context);
                        activityGroups.Add(new ActivityGroup(groupDescription, activity, date));
                    }
                }
            }

            // Convert ActivityGroups to GroupedActivities
            return activityGroups.Select(ag => ag.ToGroupedActivity()).ToList();
        }

        private void AddToJiraGroup(
            List<ActivityGroup> groups,
            EndedActivity activity,
            string jiraKey,
            DateOnly date)
        {
            var existingGroup = groups.FirstOrDefault(g => g.JiraKey == jiraKey);
            
            if (existingGroup != null)
            {
                existingGroup.AddActivity(activity);
            }
            else
            {
                var description = $"{jiraKey}: {activity.GetDescription()}";
                groups.Add(new ActivityGroup(description, activity, date, jiraKey));
            }
        }

        private ActivityGroup? FindBestMatchingGroup(List<ActivityGroup> groups, EndedActivity activity)
        {
            if (!groups.Any())
                return null;

            var description = activity.GetDescription();
            var candidates = new List<(ActivityGroup Group, float Score)>();

            foreach (var group in groups)
            {
                // Skip Jira groups - they only match exact keys
                if (group.JiraKey != null)
                    continue;

                float score = 0f;

                // Criterion 1: Semantic similarity
                var semanticScore = CalculateSemanticSimilarity(description, group.Description);
                score += semanticScore * 0.4f;

                // Criterion 2: Temporal proximity (session detection)
                var temporalScore = CalculateTemporalProximity(activity, group);
                score += temporalScore * 0.3f;

                // Criterion 3: Context similarity
                var contextScore = CalculateContextSimilarity(description, group.Description);
                score += contextScore * 0.3f;

                if (score > MinimumSemanticSimilarity)
                {
                    candidates.Add((group, score));
                }
            }

            // Return the best match
            return candidates
                .OrderByDescending(c => c.Score)
                .FirstOrDefault()
                .Group;
        }

        private float CalculateSemanticSimilarity(string description1, string description2)
        {
            var words1 = Tokenize(description1);
            var words2 = Tokenize(description2);

            if (!words1.Any() || !words2.Any())
                return 0f;

            var commonWords = words1.Intersect(words2).Count();
            
            if (commonWords == 0)
                return 0f;

            // Dice coefficient
            return (2.0f * commonWords) / (words1.Count + words2.Count);
        }

        private float CalculateTemporalProximity(EndedActivity activity, ActivityGroup group)
        {
            // Check if this activity is close in time to the last activity in the group
            var lastActivity = group.GetLastActivity();
            if (lastActivity == null)
                return 0f;

            var timeDiff = (activity.StartDate - lastActivity.EndDate).TotalMinutes;
            
            // Activities must be in chronological order and within session gap
            if (timeDiff < 0 || timeDiff > SessionGapMinutes)
                return 0f;

            // Score decreases as time gap increases
            return 1.0f - (float)(timeDiff / SessionGapMinutes);
        }

        private float CalculateContextSimilarity(string description1, string description2)
        {
            var context1 = DetermineContext(description1);
            var context2 = DetermineContext(description2);

            return context1 == context2 ? 1.0f : 0f;
        }

        private string? ExtractJiraKey(string description)
        {
            var match = JiraKeyRegex.Match(description);
            return match.Success ? match.Value : null;
        }

        private string DetermineContext(string description)
        {
            var lowerDescription = description.ToLowerInvariant();

            foreach (var kvp in ContextKeywords)
            {
                if (kvp.Value.Any(keyword => lowerDescription.Contains(keyword)))
                {
                    return kvp.Key;
                }
            }

            return "General Work";
        }

        private string CreateGroupDescription(string originalDescription, string context)
        {
            // If description is very generic, use context
            if (originalDescription.Length < 20)
            {
                return $"{context}: {originalDescription}";
            }

            return originalDescription;
        }

        private HashSet<string> Tokenize(string text)
        {
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '-', '_', ':', '|', '/', '\\' }, 
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .Where(w => !IsStopWord(w))
                .ToHashSet();
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string> 
            { 
                "the", "and", "for", "with", "from", "this", "that", "are", "was", "were", 
                "been", "have", "has", "had", "will", "can", "could", "would", "should"
            };
            return stopWords.Contains(word);
        }

        public void Dispose()
        {
            // No resources to dispose
        }

        /// <summary>
        /// Internal class to track activity groups during processing
        /// </summary>
        private class ActivityGroup
        {
            private readonly List<EndedActivity> _activities = new();
            private readonly DateOnly _date;

            public string Description { get; private set; }
            public string? JiraKey { get; }

            public ActivityGroup(string description, EndedActivity firstActivity, DateOnly date, string? jiraKey = null)
            {
                Description = description;
                JiraKey = jiraKey;
                _date = date;
                _activities.Add(firstActivity);
            }

            public void AddActivity(EndedActivity activity)
            {
                _activities.Add(activity);
                
                // Update description to be more representative
                // Keep the longest or most detailed description
                if (activity.GetDescription().Length > Description.Length && JiraKey == null)
                {
                    Description = activity.GetDescription();
                }
            }

            public EndedActivity? GetLastActivity()
            {
                return _activities.LastOrDefault();
            }

            public GroupedActivity ToGroupedActivity()
            {
                var grouped = GroupedActivity.CreateEmptyWithDescriptionForDate(_date, Description);
                
                foreach (var activity in _activities)
                {
                    grouped.Include(activity.Guid, new TimePeriod(activity.StartDate, activity.EndDate));
                }
                
                return grouped;
            }
        }
    }
}
