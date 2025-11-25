using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.SystemTrackers
{
    /// <summary>
    /// Activity-specific repository implementation using GenericDataRepository<T>.
    /// </summary>
    public class ActivityRepository : GenericDataRepository<EndedActivity>, IActivityRepository
    {
        public ActivityRepository(IClock clock, Func<IReadOnlyCollection<EndedActivity>>? getCurrentSessionDataProvider = null)
            : base(clock, getCurrentSessionDataProvider)
        {
        }

        public IReadOnlyCollection<EndedActivity> GetActivitiesForDate(DateOnly date) => GetForDate(date);

        public IReadOnlyCollection<EndedActivity> GetActivitiesBetweenDates(DateOnly startDate, DateOnly endDate) =>
            GetBetweenDates(startDate, endDate);
    }
}
