namespace TrackYourDay.Core.ApplicationTrackers.GitLab.Models;

/// <summary>
/// Represents a unified GitLab artifact (issue or merge request).
/// </summary>
public record GitLabArtifact
{
    public required long Id { get; init; }
    public required long Iid { get; init; }
    public required long ProjectId { get; init; }
    public required string Title { get; init; }
    public required string? Description { get; init; }
    public required GitLabArtifactType Type { get; init; }
    public required string State { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string WebUrl { get; init; }
    public required string AuthorUsername { get; init; }
    public required List<string> AssigneeUsernames { get; init; }
}

public enum GitLabArtifactType
{
    Issue,
    MergeRequest
}

