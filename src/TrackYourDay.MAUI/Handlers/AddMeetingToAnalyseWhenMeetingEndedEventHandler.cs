using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.MAUI.Handlers
{
    internal class AddMeetingToAnalyseWhenMeetingEndedEventHandler : INotificationHandler<MeetingEndedEvent>
    {
        private readonly ActivitiesAnalyser activitiesAnalyser;

        public AddMeetingToAnalyseWhenMeetingEndedEventHandler(ActivitiesAnalyser activitiesAnalyser)
        {
            this.activitiesAnalyser = activitiesAnalyser;
        }

        public Task Handle(MeetingEndedEvent notification, CancellationToken cancellationToken)
        {
            this.activitiesAnalyser.Analyse(notification.EndedMeeting);

            return Task.CompletedTask;
        }
    }
}
