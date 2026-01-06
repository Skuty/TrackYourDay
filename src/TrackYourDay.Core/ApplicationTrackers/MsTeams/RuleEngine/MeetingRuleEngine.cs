using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Configuration;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

/// <summary>
/// Evaluates meeting recognition rules against running processes.
/// Rules are evaluated in priority order (ascending).
/// First matching rule wins (AC1, AC5).
/// </summary>
public sealed class MeetingRuleEngine : IMeetingRuleEngine
{
    private readonly IClock _clock;
    private readonly ILogger<MeetingRuleEngine> _logger;

    public MeetingRuleEngine(IClock clock, ILogger<MeetingRuleEngine> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public MeetingMatch? EvaluateRules(
        IReadOnlyList<MeetingRecognitionRule> rules,
        IEnumerable<ProcessInfo> processes,
        Guid? ongoingMeetingRuleId)
    {
        if (rules is null)
            throw new ArgumentNullException(nameof(rules));
        if (processes is null)
            throw new ArgumentNullException(nameof(processes));

        var processList = processes.ToList();
        if (processList.Count == 0)
        {
            _logger.LogDebug("No processes to evaluate");
            return null;
        }

        foreach (var rule in rules)
        {
            var match = EvaluateRule(rule, processList);
            if (match is not null)
            {
                _logger.LogInformation("Rule {RuleId} (Priority {Priority}) matched process: {ProcessName}, window: {WindowTitle}",
                    rule.Id, rule.Priority, match.ProcessName, match.WindowTitle);
                return match;
            }
        }

        _logger.LogDebug("No rules matched {ProcessCount} processes", processList.Count);
        return null;
    }

    private MeetingMatch? EvaluateRule(MeetingRecognitionRule rule, List<ProcessInfo> processes)
    {
        foreach (var process in processes)
        {
            if (RuleMatches(rule, process))
            {
                return new MeetingMatch
                {
                    MatchedRuleId = rule.Id,
                    ProcessName = process.ProcessName,
                    WindowTitle = process.MainWindowTitle,
                    MatchedAt = _clock.Now
                };
            }
        }

        return null;
    }

    private bool RuleMatches(MeetingRecognitionRule rule, ProcessInfo process)
    {
        var inclusionMatches = rule.Criteria switch
        {
            MatchingCriteria.ProcessNameOnly => MatchesProcessName(rule, process),
            MatchingCriteria.WindowTitleOnly => MatchesWindowTitle(rule, process),
            MatchingCriteria.Both => MatchesProcessName(rule, process) && MatchesWindowTitle(rule, process),
            _ => false
        };

        if (!inclusionMatches)
            return false;

        foreach (var exclusion in rule.Exclusions)
        {
            var targetString = exclusion.MatchMode == PatternMatchMode.Regex || 
                             rule.Criteria == MatchingCriteria.WindowTitleOnly
                ? process.MainWindowTitle
                : process.ProcessName;

            if (exclusion.Matches(targetString, _logger))
            {
                _logger.LogDebug("Rule {RuleId} excluded by pattern: {Pattern}", rule.Id, exclusion.Pattern);
                return false;
            }
        }

        return true;
    }

    private bool MatchesProcessName(MeetingRecognitionRule rule, ProcessInfo process)
    {
        if (rule.ProcessNamePattern is null)
            return false;

        return rule.ProcessNamePattern.Matches(process.ProcessName, _logger);
    }

    private bool MatchesWindowTitle(MeetingRecognitionRule rule, ProcessInfo process)
    {
        if (rule.WindowTitlePattern is null)
            return false;

        return rule.WindowTitlePattern.Matches(process.MainWindowTitle, _logger);
    }
}
