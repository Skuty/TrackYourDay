namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public interface IGitLabActivityService
    {
        Task<List<GitLabActivity>> GetTodayActivitiesAsync(CancellationToken cancellationToken = default);
        Task<bool> CheckConnection();
    }
}
