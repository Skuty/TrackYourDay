namespace TrackYourDay.Core.Old.Activities.Repositories
{
    public interface IActivityEventRepository
    {
        void SaveEvent(ActivityEvent @event);
    }
}