namespace TrackYourDay.Core.Activities.Repositories
{
    public interface IActivityEventRepository
    {
        void SaveEvent(ActivityEvent @event);
    }
}