using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    /// <summary>
    /// Break-specific repository implementation using GenericDataRepository<T>.
    /// </summary>
    public class BreakRepository : GenericDataRepository<EndedBreak>, IBreakRepository
    {
        public BreakRepository(IClock clock, Func<IReadOnlyCollection<EndedBreak>>? getCurrentSessionDataProvider = null)
            : base(clock, getCurrentSessionDataProvider)
        {
        }

        public IReadOnlyCollection<EndedBreak> GetBreaksForDate(DateOnly date) => GetForDate(date);

        public IReadOnlyCollection<EndedBreak> GetBreaksBetweenDates(DateOnly startDate, DateOnly endDate) =>
            GetBetweenDates(startDate, endDate);
    }
}
