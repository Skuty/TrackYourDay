namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public interface IBreakRepository
    {
        void Save(EndedBreak endedBreak);
        IReadOnlyCollection<EndedBreak> GetBreaksForDate(DateOnly date);
        IReadOnlyCollection<EndedBreak> GetBreaksBetweenDates(DateOnly startDate, DateOnly endDate);
        void Clear();
        long GetDatabaseSizeInBytes();
        int GetTotalRecordCount();
    }
}
