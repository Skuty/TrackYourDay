using MediatR;
using TrackYourDay.Tests.Activities;

namespace TrackYourDay.Tests.ActivityTracking
{
    internal record class PeriodicActivityStartedNotification(Guid Guid, StartedActivity StartedActivity) : INotification;
}