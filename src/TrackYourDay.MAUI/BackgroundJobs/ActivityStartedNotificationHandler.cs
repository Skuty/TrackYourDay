using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.Notifications;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.MAUI.BackgroundJobs
{
    public class ActivityStartedNotificationHandler : INotificationHandler<PeriodicActivityStartedNotification>
    {
        private readonly BreakTracker breakTracker;

        public ActivityStartedNotificationHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }

        public Task Handle(PeriodicActivityStartedNotification notification, CancellationToken cancellationToken)
        {
            this.breakTracker.AddActivityToProcess(notification.StartedActivity.StartDate, notification.StartedActivity.ActivityType, notification.StartedActivity.Guid);

            return Task.CompletedTask;
        }
    }
}
