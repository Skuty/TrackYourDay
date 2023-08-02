using MediatR;

namespace TrackYourDay.Core.Activities.Notifications
{
    internal record class PeriodicActivityEndedNotification(Guid Guid, EndedActivity StartedActivity) : INotification;
}