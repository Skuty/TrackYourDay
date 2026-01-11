using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Discovers ongoing meetings using configurable pattern-based rules.
/// Stateless strategy - caller maintains meeting continuity state.
/// </summary>
public sealed class ConfigurableMeetingDiscoveryStrategy : IMeetingDiscoveryStrategy
{
    private readonly IMeetingRuleEngine _ruleEngine;
    private readonly IMeetingRuleRepository _ruleRepository;
    private readonly IProcessService _processService;
    private readonly ILogger<ConfigurableMeetingDiscoveryStrategy> _logger;

    public ConfigurableMeetingDiscoveryStrategy(
        IMeetingRuleEngine ruleEngine,
        IMeetingRuleRepository ruleRepository,
        IProcessService processService,
        ILogger<ConfigurableMeetingDiscoveryStrategy> logger)
    {
        _ruleEngine = ruleEngine;
        _ruleRepository = ruleRepository;
        _processService = processService;
        _logger = logger;
    }

    public (StartedMeeting? Meeting, Guid? MatchedRuleId) RecognizeMeeting(
        StartedMeeting? currentMeeting,
        Guid? currentMatchedRuleId)
    {
        var rules = _ruleRepository.GetAllRules();

        if (rules.Count == 0)
        {
            _logger.LogWarning("No meeting recognition rules configured. Meeting tracking disabled.");
            return (null, null);
        }

        var processes = _processService.GetProcesses()
            .Select(p => new ProcessInfo(p.ProcessName, p.MainWindowTitle))
            .ToList();
        
        var match = _ruleEngine.EvaluateRules(rules, processes, currentMatchedRuleId);

        if (match is not null)
        {
            // Same rule still matches - continue current meeting
            if (currentMeeting is not null && currentMatchedRuleId == match.MatchedRuleId)
            {
                return (currentMeeting, currentMatchedRuleId);
            }
            
            // Different rule matched - start new meeting
            if (currentMatchedRuleId != match.MatchedRuleId)
            {
                _ruleRepository.IncrementMatchCount(match.MatchedRuleId, match.MatchedAt);
            }

            var newMeeting = new StartedMeeting(Guid.NewGuid(), match.MatchedAt, match.WindowTitle);
            return (newMeeting, match.MatchedRuleId);
        }

        // No match - clear state
        return (null, null);
    }
}
