using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Persistence
{
    public class HistoricalDataService
    {
        private readonly IActivityRepository activityRepository;
        private readonly IBreakRepository breakRepository;
        private readonly IMeetingRepository meetingRepository;
        private readonly ActivityTracker activityTracker;
        private readonly BreakTracker breakTracker;
        private readonly MsTeamsMeetingTracker meetingTracker;
        private readonly IClock clock;

        public HistoricalDataService(
            IActivityRepository activityRepository,
            IBreakRepository breakRepository,
            IMeetingRepository meetingRepository,
            ActivityTracker activityTracker,
            BreakTracker breakTracker,
            MsTeamsMeetingTracker meetingTracker,
            IClock clock)
        {
            this.activityRepository = activityRepository;
            this.breakRepository = breakRepository;
            this.meetingRepository = meetingRepository;
            this.activityTracker = activityTracker;
            this.breakTracker = breakTracker;
            this.meetingTracker = meetingTracker;
            this.clock = clock;
        }

        public IReadOnlyCollection<EndedActivity> GetActivitiesForDate(DateOnly date)
        {
            // If requesting today's data, get from tracker (in-memory)
            if (date == DateOnly.FromDateTime(clock.Now.Date))
            {
                return activityTracker.GetEndedActivities();
            }

            // For historical data, get from repository
            return activityRepository.GetActivitiesForDate(date);
        }

        public IReadOnlyCollection<EndedBreak> GetBreaksForDate(DateOnly date)
        {
            // If requesting today's data, get from tracker (in-memory)
            if (date == DateOnly.FromDateTime(clock.Now.Date))
            {
                return breakTracker.GetEndedBreaks();
            }

            // For historical data, get from repository
            return breakRepository.GetBreaksForDate(date);
        }

        public IReadOnlyCollection<EndedMeeting> GetMeetingsForDate(DateOnly date)
        {
            // If requesting today's data, get from tracker (in-memory)
            if (date == DateOnly.FromDateTime(clock.Now.Date))
            {
                return meetingTracker.GetEndedMeetings();
            }

            // For historical data, get from repository
            return meetingRepository.GetMeetingsForDate(date);
        }
    }
}
