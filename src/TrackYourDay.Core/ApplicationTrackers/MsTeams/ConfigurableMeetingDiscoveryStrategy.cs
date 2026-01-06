using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.Persistence;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Discovers ongoing meetings using configurable pattern-based rules.
/// Replaces ProcessBasedMeetingRecognizingStrategy with dynamic rule resolution.
/// </summary>
public sealed class ConfigurableMeetingDiscoveryStrategy : IMeetingDiscoveryStrategy
{
    private readonly IMeetingRuleEngine _ruleEngine;
    private readonly IMeetingRuleRepository _ruleRepository;
    private readonly IProcessService _processService;
    private readonly IMeetingStateCache _stateCache;
    private readonly IClock _clock;
    private readonly ILogger<ConfigurableMeetingDiscoveryStrategy> _logger;

    public ConfigurableMeetingDiscoveryStrategy(
        IMeetingRuleEngine ruleEngine,
        IMeetingRuleRepository ruleRepository,
        IProcessService processService,
        IMeetingStateCache stateCache,
        IClock clock,
        ILogger<ConfigurableMeetingDiscoveryStrategy> logger)
    {
        _ruleEngine = ruleEngine;
        _ruleRepository = ruleRepository;
        _processService = processService;
        _stateCache = stateCache;
        _clock = clock;
        _logger = logger;
    }

    public StartedMeeting? RecognizeMeeting()
    {
        var rules = _ruleRepository.GetAllRules();

        if (rules.Count == 0)
        {
            _logger.LogWarning("No meeting recognition rules configured. Meeting tracking disabled.");
            return null;
        }

        var processes = _processService.GetProcesses()
            .Select(p => new ProcessInfo(p.ProcessName, p.MainWindowTitle))
            .ToList();

        var ongoingMeetingRuleId = _stateCache.GetMatchedRuleId();
        var match = _ruleEngine.EvaluateRules(rules, processes, ongoingMeetingRuleId);

        if (match != null)
        {
            if (ongoingMeetingRuleId != match.MatchedRuleId)
            {
                _ruleRepository.IncrementMatchCount(match.MatchedRuleId, match.MatchedAt);
                _stateCache.SetMatchedRuleId(match.MatchedRuleId);
            }

            var ongoingMeeting = _stateCache.GetOngoingMeeting();
            if (ongoingMeeting != null && ongoingMeetingRuleId == match.MatchedRuleId)
            {
                return ongoingMeeting;
            }

            return new StartedMeeting(Guid.NewGuid(), match.MatchedAt, match.WindowTitle);
        }

        if (ongoingMeetingRuleId != null)
        {
            _stateCache.SetMatchedRuleId(null);
        }

        return null;
    }
}
