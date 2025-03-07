using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.Insights.Workdays.Events;

namespace TrackYourDay.Web.Events
{
    /// <summary>
    /// This class is used to wrap the Notification Event from MediatR and Apply them on Razor Components.
    /// It is used to decouple the MediatR Notification from the Razor Components due to the 
    /// fact that initialisation of Razor Components is managed by the Blazor framework and.
    /// </summary>
    /// <remarks>
    /// Each Component should have its own set of methods and actions to handle the events.
    /// In the end of day, every Component should have its own class.
    /// </remarks>
    public class EventWrapperForComponents 
    {
        public event Action<WorkdayUpdatedEvent>? OperationalBarOnWorkdayUpdatedAction;
        public void OperationalBarOnWorkdayUpdated(WorkdayUpdatedEvent workdayUpdatedEvent)
        {
            OperationalBarOnWorkdayUpdatedAction?.Invoke(workdayUpdatedEvent);
        }

        public event Action<MeetingStartedEvent>? OperationalBarOnMeetingStartedAction;
        public void OperationalBarOnMeetingStarted(MeetingStartedEvent meetingStartedEvent)
        {
            OperationalBarOnMeetingStartedAction?.Invoke(meetingStartedEvent);
        }

        public event Action<MeetingEndedEvent>? OperationalBarOnMeetingEndedAction;
        public void OperationalBarOnMeetingEnded(MeetingEndedEvent meetingEndedEvent)
        {
            OperationalBarOnMeetingEndedAction?.Invoke(meetingEndedEvent);
        }
    }
}
