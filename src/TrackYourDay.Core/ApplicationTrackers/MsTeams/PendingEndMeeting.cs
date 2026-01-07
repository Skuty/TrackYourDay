namespace TrackYourDay.Core.ApplicationTrackers.MsTeams;

/// <summary>
/// Represents a meeting detected as ended but awaiting user confirmation.
/// </summary>
public sealed class PendingEndMeeting
{
    private static readonly TimeSpan DefaultConfirmationWindow = TimeSpan.FromMinutes(2);

    public required StartedMeeting Meeting { get; init; }
    public required DateTime DetectedAt { get; init; }
    public TimeSpan ConfirmationWindow { get; init; } = DefaultConfirmationWindow;

    /// <summary>
    /// Determines if the confirmation window has expired.
    /// </summary>
    public bool IsExpired(IClock clock)
    {
        return clock.Now >= DetectedAt.Add(ConfirmationWindow);
    }
}
