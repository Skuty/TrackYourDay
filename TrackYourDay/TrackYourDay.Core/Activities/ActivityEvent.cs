namespace TrackYourDay.Core.Activities
{
    public record class ActivityEvent(Guid Id, DateTime EventDate, Activity Activity, string EventDescription)
    {
        public static ActivityEvent CreateEvent(DateTime eventDate, Activity activity, string eventDescription)
        {
            return new ActivityEvent(Guid.NewGuid(), eventDate, activity, eventDescription);
        }
    }
}
