namespace TrackYourDay.Core.Persistence
{
    public interface IHistoricalDataRepository<T> where T : class
    {
        void Save(T item);
        IReadOnlyCollection<T> GetForDate(DateOnly date);
        IReadOnlyCollection<T> GetBetweenDates(DateOnly startDate, DateOnly endDate);
        void Clear();
        long GetDatabaseSizeInBytes();
        int GetTotalRecordCount();
    }
}
