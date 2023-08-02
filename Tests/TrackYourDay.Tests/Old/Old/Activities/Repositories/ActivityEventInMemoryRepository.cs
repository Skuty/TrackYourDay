namespace TrackYourDay.Core.Old.Activities.Repositories
{
    public class ActivityEventInMemoryRepository : IActivityEventRepository
    {
        private List<ActivityEvent> Events;

        public ActivityEventInMemoryRepository()
        {
            Events = new List<ActivityEvent>();
        }

        public void SaveEvent(ActivityEvent @event)
        {
            Events.Add(@event);
        }
    }
}
