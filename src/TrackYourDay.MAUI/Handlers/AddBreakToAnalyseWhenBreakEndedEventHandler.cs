using MediatR;
using TrackYourDay.Core.Analytics;
using TrackYourDay.Core.Breaks.Events;

namespace TrackYourDay.MAUI.Handlers
{
    internal class AddBreakToAnalyseWhenBreakEndedEventHandler : INotificationHandler<BreakEndedEvent>
    {
        private readonly ActivitiesAnalyser activitiesAnalyser;

        public AddBreakToAnalyseWhenBreakEndedEventHandler(ActivitiesAnalyser activitiesAnalyser)
        {
            this.activitiesAnalyser = activitiesAnalyser;
        }

        public Task Handle(BreakEndedEvent notification, CancellationToken cancellationToken)
        {
            this.activitiesAnalyser.Analyse(notification.EndedBreak);

            return Task.CompletedTask;
        }
    }
}
