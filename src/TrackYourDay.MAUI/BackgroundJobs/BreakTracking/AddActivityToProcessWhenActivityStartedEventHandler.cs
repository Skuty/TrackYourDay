using MediatR;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.SystemTrackers.Events;

namespace TrackYourDay.MAUI.BackgroundJobs.BreakTracking
{
    public class AddActivityToProcessWhenActivityStartedEventHandler : INotificationHandler<PeriodicActivityStartedEvent>
    {
        private readonly BreakTracker breakTracker;

        public AddActivityToProcessWhenActivityStartedEventHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }

        public Task Handle(PeriodicActivityStartedEvent _event, CancellationToken cancellationToken)
        {
            breakTracker.AddActivityToProcess(_event.StartedActivity.StartDate, _event.StartedActivity.SystemState, _event.StartedActivity.Guid);
            return Task.CompletedTask;
        }
    }
}
