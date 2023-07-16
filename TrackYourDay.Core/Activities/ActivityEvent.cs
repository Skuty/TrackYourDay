namespace TrackYourDay.Core.Activities
{
    public record class ActivityEvent(Guid Id, DateTime EventDate, Activity Activity)
    {
        public static ActivityEvent CreateEvent(DateTime eventDate, Activity activity)
        {
            return new ActivityEvent(Guid.NewGuid(), eventDate, activity);
        }
    }
}
