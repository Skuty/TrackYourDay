using MediatR;
using TrackYourDay.Tests.Activities;

namespace TrackYourDay.Tests.ActivityTracking
{
    internal record class PeriodicActivityEndedNotification(Guid Guid, EndedActivity StartedActivity) : INotification;
}