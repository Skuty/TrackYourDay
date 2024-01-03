using MediatR;

namespace TrackYourDay.Core.Activities.Notifications
{
    public record class PeriodicActivityEndedNotification(Guid Guid, EndedActivity StartedActivity) : INotification;
}