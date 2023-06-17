using TrackYourDay.Core;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Events
{
    public record class SystemEvent(Guid Id, DateTime EventDate, Activity Activity, string EventDescription)
    {
        public static SystemEvent CreateEvent(DateTime eventDate, Activity activity, string eventDescription)
        {
            return new SystemEvent(Guid.NewGuid(), eventDate, activity, eventDescription);
        }
    }
}
