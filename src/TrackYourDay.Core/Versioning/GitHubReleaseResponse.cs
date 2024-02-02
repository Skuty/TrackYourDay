namespace TrackYourDay.Core.Versioning
{
    internal class GitHubReleaseResponse
    {
        public string name { get; set; }

        public DateTime published_at { get; set; }

        public bool prerelease { get; set; }
    }
}