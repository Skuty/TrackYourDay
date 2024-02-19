using MediatR;
using TrackYourDay.Core.Activities.Notifications;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.MAUI.BackgroundJobs.BreakTracking
{
    internal class AddActivityToProcessWhenInstantActivityOccuredNotificationHandler : INotificationHandler<InstantActivityOccuredNotification>
    {
        private readonly BreakTracker breakTracker;

        public AddActivityToProcessWhenInstantActivityOccuredNotificationHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }

        public Task Handle(InstantActivityOccuredNotification notification, CancellationToken cancellationToken)
        {
            breakTracker.AddActivityToProcess(
                notification.InstantActivity.OccuranceDate,
                notification.InstantActivity.SystemState,
                notification.InstantActivity.Guid);
            return Task.CompletedTask;
        }
    }
}
