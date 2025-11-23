namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public interface IMeetingRepository
    {
        void Save(EndedMeeting meeting);
        IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date);
        IReadOnlyCollection<EndedMeeting> GetMeetingsBetweenDates(DateOnly startDate, DateOnly endDate);
        void Clear();
        long GetDatabaseSizeInBytes();
        int GetTotalRecordCount();
    }
}
