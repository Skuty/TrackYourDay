using MediatR;

namespace TrackYourDay.Core.Activities.Events
{
    public record class PeriodicActivityStartedEvent(Guid Guid, StartedActivity StartedActivity) : INotification;
}