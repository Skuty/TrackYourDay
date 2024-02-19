using MediatR;
using TrackYourDay.Core.Activities.Notifications;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.MAUI.BackgroundJobs.BreakTracking
{
    public class AddActivityToProcessWhenActivityStartedNotificationHandler : INotificationHandler<PeriodicActivityStartedNotification>
    {
        private readonly BreakTracker breakTracker;

        public AddActivityToProcessWhenActivityStartedNotificationHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }

        public Task Handle(PeriodicActivityStartedNotification notification, CancellationToken cancellationToken)
        {
            breakTracker.AddActivityToProcess(notification.StartedActivity.StartDate, notification.StartedActivity.SystemState, notification.StartedActivity.Guid);
            return Task.CompletedTask;
        }
    }
}
