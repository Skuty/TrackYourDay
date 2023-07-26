using MediatR;
using TrackYourDay.Core.Old.Activities.Notifications;

namespace TrackYourDay.Core.Old.Breaks.Notifications
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
            breakTracker.AddActivityEventToProcess(notification.ActivityEvent);
            breakTracker.ProcessActivityEvents();

            return Task.CompletedTask;
        }
    }
}
