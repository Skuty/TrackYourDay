namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.RuleEngine;

/// <summary>
/// Process information extracted by IProcessService.
/// Immutable snapshot—does not track process lifecycle.
/// </summary>
public sealed record ProcessInfo(string ProcessName, string MainWindowTitle);
