using MediatR;

namespace TrackYourDay.Core.ApplicationTrackers.Jira.PublicEvents
{
    public record class JiraActivityDiscoveredEvent(Guid Guid, JiraActivity Activity) : INotification;
}
