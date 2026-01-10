using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Web.Services;

namespace TrackYourDay.Web.Events
{
    public class MeetingEndedEventHandler : INotificationHandler<MeetingEndedEvent>
    {
        private readonly EventWrapperForComponents eventWrapperForComponents;
        private readonly IRecentMeetingsCache recentMeetingsCache;

        public MeetingEndedEventHandler(
            EventWrapperForComponents eventWrapperForComponents,
            IRecentMeetingsCache recentMeetingsCache)
        {
            this.eventWrapperForComponents = eventWrapperForComponents;
            this.recentMeetingsCache = recentMeetingsCache;
        }

        public Task Handle(MeetingEndedEvent notification, CancellationToken cancellationToken)
        {
            // Cache the meeting for UI popup lookup
            recentMeetingsCache.Add(notification.EndedMeeting);
            
            this.eventWrapperForComponents.OperationalBarOnMeetingEnded(notification);

            return Task.CompletedTask;
        }
    }
}
