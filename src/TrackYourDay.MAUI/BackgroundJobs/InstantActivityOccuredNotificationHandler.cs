using MediatR;
using TrackYourDay.Core.Activities.Notifications;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.MAUI.BackgroundJobs
{
    internal class InstantActivityOccuredNotificationHandler : INotificationHandler<InstantActivityOccuredNotification>
    {
        private readonly BreakTracker breakTracker;

        public InstantActivityOccuredNotificationHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }

        public Task Handle(InstantActivityOccuredNotification notification, CancellationToken cancellationToken)
        {
            this.breakTracker.AddActivityToProcess(
                notification.InstantActivity.OccuranceDate, 
                notification.InstantActivity.SystemState, 
                notification.InstantActivity.Guid);
            return Task.CompletedTask;
        }
    }
}
