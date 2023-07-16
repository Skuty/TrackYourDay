using MediatR;

using TrackYourDay.Core.Activities.Notifications;

namespace TrackYourDay.Core.Breaks.Notifications
{
    internal class ActivityEventNotificationHandler : INotificationHandler<ActivityEventRecognizedNotification>
    {
        private readonly BreakTracker breakTracker;

        public ActivityEventNotificationHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }
        public Task Handle(ActivityEventRecognizedNotification notification, CancellationToken cancellationToken)
        {
            //Inbox here?
            this.breakTracker.AddActivityEventToProcess(notification.ActivityEvent);
            this.breakTracker.ProcessActivityEvents();
            
            return Task.CompletedTask;
        }
    }
}
