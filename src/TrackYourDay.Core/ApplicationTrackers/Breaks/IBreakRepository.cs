using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public interface IBreakRepository : IHistoricalDataRepository<EndedBreak>
    {
        IReadOnlyCollection<EndedBreak> GetBreaksForDate(DateOnly date);
        IReadOnlyCollection<EndedBreak> GetBreaksBetweenDates(DateOnly startDate, DateOnly endDate);
    }
}
