using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents
{
    public record class GitLabActivityDiscoveredEvent(Guid Guid, GitLabActivity Activity) : INotification;
}
