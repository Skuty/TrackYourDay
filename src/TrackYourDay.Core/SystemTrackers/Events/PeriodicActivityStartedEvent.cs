using MediatR;

namespace TrackYourDay.Core.SystemTrackers.Events
{
    public record class PeriodicActivityStartedEvent(Guid Guid, StartedActivity StartedActivity) : INotification;
}