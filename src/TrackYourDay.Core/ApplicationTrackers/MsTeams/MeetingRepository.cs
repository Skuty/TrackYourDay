using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    /// <summary>
    /// Meeting-specific repository implementation using GenericDataRepository<T>.
    /// </summary>
    public class MeetingRepository : GenericDataRepository<EndedMeeting>, IMeetingRepository
    {
        public MeetingRepository(IClock clock, Func<IReadOnlyCollection<EndedMeeting>>? getCurrentSessionDataProvider = null)
            : base(clock, getCurrentSessionDataProvider)
        {
        }

        public IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date) => GetForDate(date);

        public IReadOnlyCollection<EndedMeeting> GetMeetingsBetweenDates(DateOnly startDate, DateOnly endDate) =>
            GetBetweenDates(startDate, endDate);
    }
}
