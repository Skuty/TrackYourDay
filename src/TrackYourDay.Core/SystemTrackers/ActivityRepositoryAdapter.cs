using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.SystemTrackers
{
    public class ActivityRepositoryAdapter : IActivityRepository
    {
        private readonly IHistoricalDataRepository<EndedActivity> repository;

        public ActivityRepositoryAdapter(IHistoricalDataRepository<EndedActivity> repository)
        {
            this.repository = repository;
        }

        public void Save(EndedActivity item) => repository.Save(item);

        public IReadOnlyCollection<EndedActivity> GetActivitiesForDate(DateOnly date) => repository.GetForDate(date);

        public IReadOnlyCollection<EndedActivity> GetActivitiesBetweenDates(DateOnly startDate, DateOnly endDate) => 
            repository.GetBetweenDates(startDate, endDate);

        public IReadOnlyCollection<EndedActivity> GetForDate(DateOnly date) => repository.GetForDate(date);

        public IReadOnlyCollection<EndedActivity> GetBetweenDates(DateOnly startDate, DateOnly endDate) => 
            repository.GetBetweenDates(startDate, endDate);

        public void Clear() => repository.Clear();

        public long GetDatabaseSizeInBytes() => repository.GetDatabaseSizeInBytes();

        public int GetTotalRecordCount() => repository.GetTotalRecordCount();
    }
}
