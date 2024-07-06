using MediatR;

namespace TrackYourDay.Core.Activities.Events
{
    public record class PeriodicActivityEndedEvent(Guid Guid, EndedActivity EndedActivity) : INotification;
}