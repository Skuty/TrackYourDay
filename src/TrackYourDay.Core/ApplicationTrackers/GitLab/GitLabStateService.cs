using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab;

/// <summary>
/// Service for managing GitLab state snapshots.
/// </summary>
public sealed class GitLabStateService : IGitLabStateService
{
    private readonly IGitLabRestApiClient _apiClient;
    private readonly IGitLabStateRepository _repository;
    private readonly IClock _clock;
    private readonly ILogger<GitLabStateService> _logger;

    public GitLabStateService(
        IGitLabRestApiClient apiClient,
        IGitLabStateRepository repository,
        IClock clock,
        ILogger<GitLabStateService> logger)
    {
        _apiClient = apiClient;
        _repository = repository;
        _clock = clock;
        _logger = logger;
    }

    public async Task<GitLabStateSnapshot> CaptureCurrentStateAsync(CancellationToken cancellationToken = default)
    {
        var user = await _apiClient.GetCurrentUser().ConfigureAwait(false);
        var userId = new GitLabUserId(user.Id);

        var allArtifacts = new List<GitLabArtifact>();

        var assignedIssues = await _apiClient.GetAssignedIssues(userId).ConfigureAwait(false);
        allArtifacts.AddRange(assignedIssues);

        var assignedMergeRequests = await _apiClient.GetAssignedMergeRequests(userId).ConfigureAwait(false);
        allArtifacts.AddRange(assignedMergeRequests);

        var createdMergeRequests = await _apiClient.GetCreatedMergeRequests(userId).ConfigureAwait(false);
        allArtifacts.AddRange(createdMergeRequests);

        var snapshot = new GitLabStateSnapshot
        {
            Guid = Guid.NewGuid(),
            CapturedAt = _clock.Now,
            Artifacts = allArtifacts
        };

        await _repository.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Captured GitLab state: {ArtifactCount} artifacts",
            snapshot.Artifacts.Count);

        return snapshot;
    }

    public async Task<GitLabStateSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetLatestAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<GitLabStateSnapshot>> GetSnapshotsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
    }
}
