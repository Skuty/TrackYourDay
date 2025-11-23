using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public class BreakRepositoryAdapter : IBreakRepository
    {
        private readonly IHistoricalDataRepository<EndedBreak> repository;

        public BreakRepositoryAdapter(IHistoricalDataRepository<EndedBreak> repository)
        {
            this.repository = repository;
        }

        public void Save(EndedBreak item) => repository.Save(item);

        public IReadOnlyCollection<EndedBreak> GetBreaksForDate(DateOnly date) => repository.GetForDate(date);

        public IReadOnlyCollection<EndedBreak> GetBreaksBetweenDates(DateOnly startDate, DateOnly endDate) => 
            repository.GetBetweenDates(startDate, endDate);

        public IReadOnlyCollection<EndedBreak> GetForDate(DateOnly date) => repository.GetForDate(date);

        public IReadOnlyCollection<EndedBreak> GetBetweenDates(DateOnly startDate, DateOnly endDate) => 
            repository.GetBetweenDates(startDate, endDate);

        public void Clear() => repository.Clear();

        public long GetDatabaseSizeInBytes() => repository.GetDatabaseSizeInBytes();

        public int GetTotalRecordCount() => repository.GetTotalRecordCount();
    }
}
