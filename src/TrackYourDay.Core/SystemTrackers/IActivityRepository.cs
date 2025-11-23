using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.SystemTrackers
{
    public interface IActivityRepository : IHistoricalDataRepository<EndedActivity>
    {
        IReadOnlyCollection<EndedActivity> GetActivitiesForDate(DateOnly date);
        IReadOnlyCollection<EndedActivity> GetActivitiesBetweenDates(DateOnly startDate, DateOnly endDate);
    }
}
