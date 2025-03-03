using MediatR;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Tests.Old.Old.Activities.Notifications;

namespace TrackYourDay.Tests.Old.Old.Breaks.Notifications
{
    internal class ActivityEventEventHandler : INotificationHandler<ActivityEventRecognizedEvent>
    {
        private readonly BreakTracker breakTracker;

        public ActivityEventEventHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }
        public Task Handle(ActivityEventRecognizedEvent Event, CancellationToken cancellationToken)
        {
            //Inbox here?
            //breakTracker.AddActivityToProcess(notification.ActivityEvent);
            breakTracker.ProcessActivities();

            return Task.CompletedTask;
        }
    }
}
