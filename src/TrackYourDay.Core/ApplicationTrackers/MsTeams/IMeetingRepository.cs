using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public interface IMeetingRepository : IHistoricalDataRepository<EndedMeeting>
    {
        IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date);
        IReadOnlyCollection<EndedMeeting> GetMeetingsBetweenDates(DateOnly startDate, DateOnly endDate);
    }
}
