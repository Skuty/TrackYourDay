namespace TrackYourDay.Core.Activities
{
    public class ActivityEventRepository
    {
        private List<ActivityEvent> Events;

        public ActivityEventRepository()
        {
            Events = new List<ActivityEvent>();
        }

        public void SaveEvent(ActivityEvent @event)
        {
            Events.Add(@event);
        }
    }
}
