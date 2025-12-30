namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public interface IGitLabActivityService
    {
        List<GitLabActivity> GetTodayActivities();
        bool CheckConnection();
    }
}
