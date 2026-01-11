namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Represents a meeting detected as ended but awaiting user confirmation.
/// </summary>
public sealed class PendingEndMeeting
{
    public required StartedMeeting Meeting { get; init; }
    public required DateTime DetectedAt { get; init; }
}
