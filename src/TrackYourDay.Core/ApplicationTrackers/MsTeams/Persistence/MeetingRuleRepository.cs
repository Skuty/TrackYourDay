using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;

/// <summary>
/// Persists meeting recognition rules using IGenericSettingsService.
/// Handles JSON serialization, default rule creation, and match count updates.
/// </summary>
public sealed class MeetingRuleRepository : IMeetingRuleRepository
{
    private const string SettingsKey = "MeetingRecognitionRules.v1";
    private readonly IGenericSettingsService _settingsService;
    private readonly ILogger<MeetingRuleRepository> _logger;

    public MeetingRuleRepository(
        IGenericSettingsService settingsService,
        ILogger<MeetingRuleRepository> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public IReadOnlyList<MeetingRecognitionRule> GetAllRules()
    {
        try
        {
            var rules = _settingsService.GetSetting<List<MeetingRecognitionRule>>(SettingsKey);
            
            if (rules is null || rules.Count == 0)
            {
                _logger.LogInformation("No rules found, creating default rule");
                var defaultRule = CreateDefaultRule();
                SaveRules([defaultRule]);
                return [defaultRule];
            }

            var sortedRules = rules.OrderBy(r => r.Priority).ToList();
            
            for (int i = 0; i < sortedRules.Count; i++)
            {
                var rule = sortedRules[i];
                var updatedRule = rule;

                if (rule.ProcessNamePattern?.MatchMode == PatternMatchMode.Regex && rule.ProcessNamePattern.CompiledRegex is null)
                {
                    var recompiled = PatternDefinition.CreateRegexPattern(rule.ProcessNamePattern.Pattern, rule.ProcessNamePattern.CaseSensitive);
                    updatedRule = updatedRule with { ProcessNamePattern = recompiled };
                }

                if (rule.WindowTitlePattern?.MatchMode == PatternMatchMode.Regex && rule.WindowTitlePattern.CompiledRegex is null)
                {
                    var recompiled = PatternDefinition.CreateRegexPattern(rule.WindowTitlePattern.Pattern, rule.WindowTitlePattern.CaseSensitive);
                    updatedRule = updatedRule with { WindowTitlePattern = recompiled };
                }

                if (rule.Exclusions.Any(e => e.MatchMode == PatternMatchMode.Regex && e.CompiledRegex is null))
                {
                    var recompiledExclusions = new List<PatternDefinition>();
                    foreach (var exclusion in rule.Exclusions)
                    {
                        if (exclusion.MatchMode == PatternMatchMode.Regex && exclusion.CompiledRegex is null)
                        {
                            recompiledExclusions.Add(PatternDefinition.CreateRegexPattern(exclusion.Pattern, exclusion.CaseSensitive));
                        }
                        else
                        {
                            recompiledExclusions.Add(exclusion);
                        }
                    }
                    updatedRule = updatedRule with { Exclusions = recompiledExclusions };
                }

                sortedRules[i] = updatedRule;
            }

            return sortedRules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rules, returning default rule");
            var defaultRule = CreateDefaultRule();
            return [defaultRule];
        }
    }

    public void SaveRules(IReadOnlyList<MeetingRecognitionRule> rules)
    {
        if (rules is null)
            throw new ArgumentNullException(nameof(rules));

        var priorities = rules.Select(r => r.Priority).ToList();
        if (priorities.Distinct().Count() != priorities.Count)
            throw new ArgumentException("Rule priorities must be unique", nameof(rules));

        foreach (var rule in rules)
        {
            rule.Validate();
        }

        _settingsService.SetSetting(SettingsKey, rules.ToList());
        _settingsService.PersistSettings();
        
        _logger.LogInformation("Saved {RuleCount} rules", rules.Count);
    }

    public void IncrementMatchCount(Guid ruleId, DateTime matchedAt)
    {
        var rules = GetAllRules().ToList();
        var ruleIndex = rules.FindIndex(r => r.Id == ruleId);
        
        if (ruleIndex == -1)
        {
            _logger.LogWarning("Rule {RuleId} not found for match count increment", ruleId);
            return;
        }

        rules[ruleIndex] = rules[ruleIndex].IncrementMatchCount(matchedAt);
        SaveRules(rules);
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
}
