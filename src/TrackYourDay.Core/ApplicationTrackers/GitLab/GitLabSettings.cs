namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public record class GitLabSettings(string ApiUrl, string ApiKey)
    {
        public static GitLabSettings CreateDefaultSettings()
        {
            return new GitLabSettings(string.Empty, string.Empty);
        }
    }
}
