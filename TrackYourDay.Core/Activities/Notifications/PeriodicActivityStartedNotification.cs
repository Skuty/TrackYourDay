using MediatR;

namespace TrackYourDay.Core.Activities.Notifications
{
    public record class PeriodicActivityStartedNotification(Guid Guid, StartedActivity StartedActivity) : INotification;
}