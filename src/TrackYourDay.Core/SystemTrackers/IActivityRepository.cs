namespace TrackYourDay.Core.SystemTrackers
{
    public interface IActivityRepository
    {
        void Save(EndedActivity activity);
        IReadOnlyCollection<EndedActivity> GetActivitiesForDate(DateOnly date);
        IReadOnlyCollection<EndedActivity> GetActivitiesBetweenDates(DateOnly startDate, DateOnly endDate);
        void Clear();
        long GetDatabaseSizeInBytes();
        int GetTotalRecordCount();
    }
}
