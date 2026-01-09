using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;

/// <summary>
/// Persists meeting recognition rules using IGenericSettingsService.
/// Handles JSON serialization, default rule creation, and match count updates.
/// Thread-safe with locking and caching. Implements write-behind pattern for match count updates.
/// </summary>
public sealed class MeetingRuleRepository : IMeetingRuleRepository, IDisposable
{
    private const string SettingsKey = "MeetingRecognitionRules.v1";
    private const int CacheTtlSeconds = 5;
    private const int FlushIntervalMilliseconds = 60_000;
    
    private readonly IGenericSettingsService _settingsService;
    private readonly ILogger<MeetingRuleRepository> _logger;
    private readonly object _lock = new();
    private readonly System.Timers.Timer _flushTimer;
    private readonly Dictionary<Guid, (long count, DateTime timestamp)> _pendingMatchUpdates = new();
    
    private IReadOnlyList<MeetingRecognitionRule>? _cachedRules;
    private DateTime _lastCacheTime = DateTime.MinValue;
    private bool _disposed;

    public MeetingRuleRepository(
        IGenericSettingsService settingsService,
        ILogger<MeetingRuleRepository> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        
        _flushTimer = new System.Timers.Timer(FlushIntervalMilliseconds);
        _flushTimer.Elapsed += (_, _) => FlushPendingMatchUpdates();
        _flushTimer.AutoReset = true;
        _flushTimer.Start();
    }

    public IReadOnlyList<MeetingRecognitionRule> GetAllRules()
    {
        lock (_lock)
        {
            if (_cachedRules is not null && (DateTime.UtcNow - _lastCacheTime).TotalSeconds < CacheTtlSeconds)
            {
                return _cachedRules;
            }

            try
            {
                var rules = _settingsService.GetSetting<List<MeetingRecognitionRule>>(SettingsKey);
                
                if (rules is null || rules.Count == 0)
                {
                    _logger.LogInformation("No rules found, creating default rule");
                    var defaultRule = CreateDefaultRule();
                    SaveRulesInternal([defaultRule]);
                    _cachedRules = [defaultRule];
                    _lastCacheTime = DateTime.UtcNow;
                    return _cachedRules;
                }

                var compiledRules = CompileRegexPatterns(rules);
                
                _cachedRules = compiledRules;
                _lastCacheTime = DateTime.UtcNow;
                return _cachedRules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load rules, returning default rule");
                var defaultRule = CreateDefaultRule();
                _cachedRules = [defaultRule];
                _lastCacheTime = DateTime.UtcNow;
                return _cachedRules;
            }
        }
    }

    private IReadOnlyList<MeetingRecognitionRule> CompileRegexPatterns(List<MeetingRecognitionRule> rules)
    {
        var compiledRules = new List<MeetingRecognitionRule>(rules.Count);
        
        foreach (var rule in rules)
        {
            var updatedRule = rule;

            if (rule.ProcessNamePattern?.MatchMode == PatternMatchMode.Regex && rule.ProcessNamePattern.CompiledRegex is null)
            {
                try
                {
                    var recompiled = PatternDefinition.CreateRegexPattern(rule.ProcessNamePattern.Pattern, rule.ProcessNamePattern.CaseSensitive);
                    updatedRule = updatedRule with { ProcessNamePattern = recompiled };
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Failed to compile process name regex for rule {RuleId}, pattern will not match", rule.Id);
                }
            }

            if (rule.WindowTitlePattern?.MatchMode == PatternMatchMode.Regex && rule.WindowTitlePattern.CompiledRegex is null)
            {
                try
                {
                    var recompiled = PatternDefinition.CreateRegexPattern(rule.WindowTitlePattern.Pattern, rule.WindowTitlePattern.CaseSensitive);
                    updatedRule = updatedRule with { WindowTitlePattern = recompiled };
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Failed to compile window title regex for rule {RuleId}, pattern will not match", rule.Id);
                }
            }

            if (rule.Exclusions.Any(e => e.MatchMode == PatternMatchMode.Regex && e.CompiledRegex is null))
            {
                var recompiledExclusions = new List<PatternDefinition>();
                foreach (var exclusion in rule.Exclusions)
                {
                    if (exclusion.MatchMode == PatternMatchMode.Regex && exclusion.CompiledRegex is null)
                    {
                        try
                        {
                            recompiledExclusions.Add(PatternDefinition.CreateRegexPattern(exclusion.Pattern, exclusion.CaseSensitive));
                        }
                        catch (ArgumentException ex)
                        {
                            _logger.LogWarning(ex, "Failed to compile exclusion regex for rule {RuleId}, pattern: {Pattern}", rule.Id, exclusion.Pattern);
                            recompiledExclusions.Add(exclusion);
                        }
                    }
                    else
                    {
                        recompiledExclusions.Add(exclusion);
                    }
                }
                updatedRule = updatedRule with { Exclusions = recompiledExclusions };
            }

            compiledRules.Add(updatedRule);
        }

        return compiledRules.OrderBy(r => r.Priority).ToList();
    }

    public void SaveRules(IReadOnlyList<MeetingRecognitionRule> rules)
    {
        lock (_lock)
        {
            SaveRulesInternal(rules);
            _cachedRules = null;
        }
    }

    private void SaveRulesInternal(IReadOnlyList<MeetingRecognitionRule> rules)
    {
        if (rules is null)
            throw new ArgumentNullException(nameof(rules));

        var priorities = rules.Select(r => r.Priority).ToList();
        if (priorities.Distinct().Count() != priorities.Count)
            throw new ArgumentException("Rule priorities must be unique", nameof(rules));

        var ids = rules.Select(r => r.Id).ToList();
        if (ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("Rule IDs must be unique", nameof(rules));

        foreach (var rule in rules)
        {
            rule.Validate();
        }

        var sortedRules = rules.OrderBy(r => r.Priority).ToList();
        _settingsService.SetSetting(SettingsKey, sortedRules);
        _settingsService.PersistSettings();
        
        _logger.LogInformation("Saved {RuleCount} rules", rules.Count);
    }

    public void IncrementMatchCount(Guid ruleId, DateTime matchedAt)
    {
        lock (_lock)
        {
            if (_pendingMatchUpdates.ContainsKey(ruleId))
            {
                var existing = _pendingMatchUpdates[ruleId];
                _pendingMatchUpdates[ruleId] = (existing.count + 1, matchedAt);
            }
            else
            {
                _pendingMatchUpdates[ruleId] = (1, matchedAt);
            }
        }
    }

    private void FlushPendingMatchUpdates()
    {
        lock (_lock)
        {
            if (_pendingMatchUpdates.Count == 0)
                return;

            try
            {
                var rules = GetAllRules().ToList();
                var hasChanges = false;

                foreach (var (ruleId, update) in _pendingMatchUpdates)
                {
                    var ruleIndex = rules.FindIndex(r => r.Id == ruleId);
                    
                    if (ruleIndex == -1)
                    {
                        _logger.LogWarning("Rule {RuleId} not found for match count flush", ruleId);
                        continue;
                    }

                    var currentRule = rules[ruleIndex];
                    rules[ruleIndex] = currentRule with
                    {
                        MatchCount = currentRule.MatchCount + update.count,
                        LastMatchedAt = update.timestamp
                    };
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    SaveRulesInternal(rules);
                    _pendingMatchUpdates.Clear();
                    _logger.LogDebug("Flushed match count updates for {UpdateCount} rules", _pendingMatchUpdates.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush pending match count updates");
            }
        }
    }

    public MeetingRecognitionRule CreateDefaultRule()
    {
        return new MeetingRecognitionRule
        {
            Id = Guid.NewGuid(),
            Priority = 1,
            Criteria = MatchingCriteria.Both,
            ProcessNamePattern = PatternDefinition.CreateStringPattern("ms-teams", PatternMatchMode.Contains, caseSensitive: false),
            WindowTitlePattern = PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Contains, caseSensitive: false),
            Exclusions =
            [
                PatternDefinition.CreateStringPattern("Czat |", PatternMatchMode.StartsWith, caseSensitive: false),
                PatternDefinition.CreateStringPattern("Aktywność |", PatternMatchMode.StartsWith, caseSensitive: false),
                PatternDefinition.CreateStringPattern("Microsoft Teams", PatternMatchMode.Exact, caseSensitive: false)
            ],
            MatchCount = 0,
            LastMatchedAt = null
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            _flushTimer?.Stop();
            FlushPendingMatchUpdates();
            _flushTimer?.Dispose();
            _disposed = true;
        }
    }
}
