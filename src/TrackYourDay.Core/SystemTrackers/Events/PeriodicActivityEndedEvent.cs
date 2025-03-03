using MediatR;

namespace TrackYourDay.Core.SystemTrackers.Events
{
    public record class PeriodicActivityEndedEvent(Guid Guid, EndedActivity EndedActivity) : INotification;
}