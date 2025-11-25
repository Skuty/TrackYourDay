using System.Collections.Concurrent;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Persistence
{
    public class HistoricalDataService
    {
        private readonly IClock clock;
        private readonly ActivityTracker activityTracker;
        private readonly BreakTracker breakTracker;
        private readonly MsTeamsMeetingTracker meetingTracker;
        private readonly ConcurrentDictionary<Type, object> repositories = new();

        public HistoricalDataService(
            IClock clock,
            ActivityTracker activityTracker,
            BreakTracker breakTracker,
            MsTeamsMeetingTracker meetingTracker)
        {
            this.clock = clock;
            this.activityTracker = activityTracker;
            this.breakTracker = breakTracker;
            this.meetingTracker = meetingTracker;
        }

        /// <summary>
        /// Registers a repository for a specific entity type.
        /// </summary>
        public void RegisterRepository<T>(IHistoricalDataRepository<T> repository) where T : class
        {
            repositories[typeof(T)] = repository;
        }

        /// <summary>
        /// Gets historical data for a specific date. 
        /// If the date is today, returns from in-memory tracker.
        /// Otherwise, returns from persisted repository.
        /// </summary>
        public IReadOnlyCollection<T> GetForDate<T>(DateOnly date) where T : class
        {
            var entityType = typeof(T);
            var today = DateOnly.FromDateTime(clock.Now.Date);

            // If requesting today's data, get from tracker (in-memory)
            if (date == today)
            {
                return GetTodayDataFromTracker<T>();
            }

            // For historical data, get from repository
            if (repositories.TryGetValue(entityType, out var repoObj) && repoObj is IHistoricalDataRepository<T> repository)
            {
                return repository.GetForDate(date);
            }

            throw new InvalidOperationException($"No repository registered for type {entityType.Name}");
        }

        /// <summary>
        /// Gets historical data between two dates.
        /// </summary>
        public IReadOnlyCollection<T> GetBetweenDates<T>(DateOnly startDate, DateOnly endDate) where T : class
        {
            var entityType = typeof(T);
            var today = DateOnly.FromDateTime(clock.Now.Date);

            // If the range includes today, we need to combine repository + tracker data
            if (endDate >= today)
            {
                var historicalData = new List<T>();

                // Get historical data from repository (if start date is before today)
                if (startDate < today && repositories.TryGetValue(entityType, out var repoObj) && repoObj is IHistoricalDataRepository<T> repository)
                {
                    var repoEndDate = endDate >= today ? today.AddDays(-1) : endDate;
                    historicalData.AddRange(repository.GetBetweenDates(startDate, repoEndDate));
                }

                // Add today's data from tracker (if today is in range)
                if (endDate >= today)
                {
                    historicalData.AddRange(GetTodayDataFromTracker<T>());
                }

                return historicalData;
            }

            // All historical data - get from repository only
            if (repositories.TryGetValue(entityType, out var repositoryObj) && repositoryObj is IHistoricalDataRepository<T> repo)
            {
                return repo.GetBetweenDates(startDate, endDate);
            }

            throw new InvalidOperationException($"No repository registered for type {entityType.Name}");
        }

        /// <summary>
        /// Gets today's data from the appropriate tracker based on the type.
        /// Trackers are only aware of current session data.
        /// </summary>
        private IReadOnlyCollection<T> GetTodayDataFromTracker<T>() where T : class
        {
            return typeof(T) switch
            {
                Type t when t == typeof(EndedActivity) => (IReadOnlyCollection<T>)activityTracker.GetEndedActivities(),
                Type t when t == typeof(EndedBreak) => (IReadOnlyCollection<T>)breakTracker.GetEndedBreaks(),
                Type t when t == typeof(EndedMeeting) => (IReadOnlyCollection<T>)meetingTracker.GetEndedMeetings(),
                _ => throw new InvalidOperationException($"No tracker registered for type {typeof(T).Name}")
            };
        }
    }
}
