using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Core.Persistence
{
    public interface IHistoricalDataRepository<T> where T : class
    {
        void Save(T item);
        void Update(T item);
        IReadOnlyCollection<T> Find(ISpecification<T> specification);
        void Clear();
        long GetDatabaseSizeInBytes();
        int GetTotalRecordCount();

        /// <summary>
        /// Attempts to append an item if it doesn't already exist.
        /// </summary>
        /// <param name="item">The item to append.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the item was added, false if it already exists.</returns>
        Task<bool> TryAppendAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds items matching the specification asynchronously.
        /// </summary>
        /// <param name="specification">The query specification.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of matching items.</returns>
        Task<IReadOnlyCollection<T>> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    }
}
