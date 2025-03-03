using MediatR;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.SystemTrackers.Events;

namespace TrackYourDay.MAUI.BackgroundJobs.BreakTracking
{
    internal class AddActivityToProcessWhenInstantActivityOccuredEventHandler : INotificationHandler<InstantActivityOccuredEvent>
    {
        private readonly BreakTracker breakTracker;

        public AddActivityToProcessWhenInstantActivityOccuredEventHandler(BreakTracker breakTracker)
        {
            this.breakTracker = breakTracker;
        }

        public Task Handle(InstantActivityOccuredEvent _event, CancellationToken cancellationToken)
        {
            breakTracker.AddActivityToProcess(
                _event.InstantActivity.OccuranceDate,
                _event.InstantActivity.SystemState,
                _event.InstantActivity.Guid);
            return Task.CompletedTask;
        }
    }
}
