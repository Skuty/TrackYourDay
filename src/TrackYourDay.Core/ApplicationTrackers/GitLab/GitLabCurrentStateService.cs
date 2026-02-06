using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.GitLab.Models;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab;

/// <summary>
/// Synchronizes current state of assigned GitLab work items.
/// </summary>
public sealed class GitLabCurrentStateService : IGitLabCurrentStateService
{
    private readonly IGitLabRestApiClient _apiClient;
    private readonly IGitLabStateRepository _repository;
    private readonly IClock _clock;
    private readonly ILogger<GitLabCurrentStateService> _logger;

    public GitLabCurrentStateService(
        IGitLabRestApiClient apiClient,
        IGitLabStateRepository repository,
        IClock clock,
        ILogger<GitLabCurrentStateService> logger)
    {
        _apiClient = apiClient;
        _repository = repository;
        _clock = clock;
        _logger = logger;
    }

    public async Task SyncStateFromRemoteService(CancellationToken cancellationToken)
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
            "Synchronized {ArtifactCount} GitLab artifacts for user {UserId}",
            snapshot.Artifacts.Count,
            userId.Id);
    }
}
