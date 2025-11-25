using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Core.Persistence
{
    public interface IHistoricalDataRepository<T> where T : class
    {
        void Save(T item);
        IReadOnlyCollection<T> Find(ISpecification<T> specification);
        void Clear();
        long GetDatabaseSizeInBytes();
        int GetTotalRecordCount();
    }
}
