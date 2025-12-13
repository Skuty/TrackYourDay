namespace TrackYourDay.Core.ApplicationTrackers.GitLab
{
    public record class DiscoveredGitLabActivity(Guid Guid, DateTime OccuranceDate, string Description);
}
