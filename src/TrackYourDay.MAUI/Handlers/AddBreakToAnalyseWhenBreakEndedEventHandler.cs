using MediatR;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;
using TrackYourDay.Core.Insights.Analytics;

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
