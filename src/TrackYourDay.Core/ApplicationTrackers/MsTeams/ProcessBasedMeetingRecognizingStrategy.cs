using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Legacy meeting recognition strategy (hardcoded Polish exclusions).
/// </summary>
[Obsolete("Use ConfigurableMeetingDiscoveryStrategy for rule-based meeting recognition")]
public sealed class ProcessBasedMeetingRecognizingStrategy : IMeetingDiscoveryStrategy
{
    private readonly ILogger<ProcessBasedMeetingRecognizingStrategy> _logger;
    private readonly IProcessService _processService;

    public ProcessBasedMeetingRecognizingStrategy(
        ILogger<ProcessBasedMeetingRecognizingStrategy> logger,
        IProcessService processService)
    {
        _logger = logger;
        _processService = processService;
    }

    public (StartedMeeting? Meeting, Guid? MatchedRuleId) RecognizeMeeting(
        StartedMeeting? currentMeeting,
        Guid? currentMatchedRuleId)
    {
        var teamsProcesses = _processService.GetProcesses()
            .Where(p => p.ProcessName.Contains("ms-teams", StringComparison.InvariantCulture));

        foreach (var process in teamsProcesses)
        {
            _logger.LogInformation("Found Teams process! Name: {ProcessName}, Title: {WindowTitle}",
                process.ProcessName, process.MainWindowTitle);
        }

        foreach (var process in teamsProcesses)
        {
            if (IsWindowTitleMatchingTeamsMeeting(process.MainWindowTitle))
            {
                // If same meeting, return current
                if (currentMeeting?.Title == process.MainWindowTitle)
                {
                    return (currentMeeting, currentMatchedRuleId);
                }
                
                // New meeting
                return (new StartedMeeting(Guid.NewGuid(), DateTime.Now, process.MainWindowTitle), null);
            }
        }

        return (null, null);
    }

    private bool IsWindowTitleMatchingTeamsMeeting(string windowTitle)
    {
        if (string.IsNullOrEmpty(windowTitle))
            return false;

        if (!windowTitle.Contains("Microsoft Teams", StringComparison.InvariantCultureIgnoreCase))
            return false;

        // Exclude known non-meeting windows
        if (windowTitle.StartsWith("Czat |", StringComparison.InvariantCultureIgnoreCase) ||
            windowTitle.StartsWith("Aktywność |", StringComparison.InvariantCultureIgnoreCase) ||
            windowTitle == "Microsoft Teams")
            return false;

        return true;
    }
}
