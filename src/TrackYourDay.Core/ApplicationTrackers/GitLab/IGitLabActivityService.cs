namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public interface IGitLabActivityService
    {
        Task<List<GitLabActivity>> GetActivitiesUpdatedAfter(DateTime startDate, CancellationToken cancellationToken = default);
        Task<bool> CheckConnection();
    }
}
