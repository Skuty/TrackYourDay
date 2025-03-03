using MediatR;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.SystemTrackers.Events;

namespace TrackYourDay.MAUI.Handlers
{
    internal class AddActivityToAnalyseWhenPeriodicActivityEndedEventHandler : INotificationHandler<PeriodicActivityEndedEvent>
    {
        private readonly ActivitiesAnalyser activitiesAnalyser;

        public AddActivityToAnalyseWhenPeriodicActivityEndedEventHandler(ActivitiesAnalyser activitiesAnalyser)
        {
            this.activitiesAnalyser = activitiesAnalyser;
        }

        public Task Handle(PeriodicActivityEndedEvent notification, CancellationToken cancellationToken)
        {
            this.activitiesAnalyser.Analyse(notification.EndedActivity);

            return Task.CompletedTask;
        }
    }

}
