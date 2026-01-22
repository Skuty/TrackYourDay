namespace TrackYourDay.Core.ApplicationTrackers.GitLab.Models;

/// <summary>
/// Represents a snapshot of GitLab state at a specific point in time.
/// </summary>
public record GitLabStateSnapshot
{
    public required Guid Guid { get; init; }
    public required DateTime CapturedAt { get; init; }
    public required List<GitLabArtifact> Artifacts { get; init; }
}
