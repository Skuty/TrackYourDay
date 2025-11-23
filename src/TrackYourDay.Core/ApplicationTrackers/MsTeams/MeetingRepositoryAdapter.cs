using TrackYourDay.Core.Persistence;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class MeetingRepositoryAdapter : IMeetingRepository
    {
        private readonly IHistoricalDataRepository<EndedMeeting> repository;

        public MeetingRepositoryAdapter(IHistoricalDataRepository<EndedMeeting> repository)
        {
            this.repository = repository;
        }

        public void Save(EndedMeeting item) => repository.Save(item);

        public IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date) => repository.GetForDate(date);

        public IReadOnlyCollection<EndedMeeting> GetMeetingsBetweenDates(DateOnly startDate, DateOnly endDate) => 
            repository.GetBetweenDates(startDate, endDate);

        public IReadOnlyCollection<EndedMeeting> GetForDate(DateOnly date) => repository.GetForDate(date);

        public IReadOnlyCollection<EndedMeeting> GetBetweenDates(DateOnly startDate, DateOnly endDate) => 
            repository.GetBetweenDates(startDate, endDate);

        public void Clear() => repository.Clear();

        public long GetDatabaseSizeInBytes() => repository.GetDatabaseSizeInBytes();

        public int GetTotalRecordCount() => repository.GetTotalRecordCount();
    }
}
