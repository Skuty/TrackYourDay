using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.SystemTrackers;
using System.Collections.Concurrent;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class ActivitiesAnalyser : IDisposable
    {
        private readonly ILogger<ActivitiesAnalyser> logger;
        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly ConcurrentBag<EndedActivity> _activities = new();
        private readonly ConcurrentBag<EndedBreak> _breaks = new();
        private readonly ConcurrentBag<EndedMeeting> _meetings = new();
        private readonly ConcurrentBag<UserTask> _userTasks = new();
        private ISummaryStrategy _summaryStrategy;

        public ActivitiesAnalyser(IClock clock, IPublisher publisher, ILogger<ActivitiesAnalyser> logger, ISummaryStrategy summaryStrategy)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _summaryStrategy = summaryStrategy ?? throw new ArgumentNullException(nameof(summaryStrategy));
        }

        public void SetSummaryStrategy(ISummaryStrategy strategy)
        {
            _summaryStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public void Analyse(EndedActivity endedActivity)
        {
            if (endedActivity == null) throw new ArgumentNullException(nameof(endedActivity));
            _activities.Add(endedActivity);
        }

        public void Analyse(EndedBreak endedBreak)
        {
            if (endedBreak == null) throw new ArgumentNullException(nameof(endedBreak));
            _breaks.Add(endedBreak);
        }

        public void Analyse(EndedMeeting endedMeeting)
        {
            if (endedMeeting == null) throw new ArgumentNullException(nameof(endedMeeting));
            _meetings.Add(endedMeeting);
        }

        public void Analyse(UserTask userTask)
        {
            if (userTask == null) throw new ArgumentNullException(nameof(userTask));
            if (userTask.IsCompleted)
            {
                _userTasks.Add(userTask);
            }
        }

        public IReadOnlyCollection<GroupedActivity> GetGroupedActivities()
        {
            var allItems = new List<TrackableItem>();
            allItems.AddRange(_activities);
            allItems.AddRange(_meetings);
            allItems.AddRange(_userTasks.Where(t => t.IsCompleted));

            var groupedActivities = _summaryStrategy.Generate(allItems);
            foreach (var endedBreak in _breaks)
            {
                var breakPeriod = TimePeriod.CreateFrom(endedBreak.BreakStartedAt, endedBreak.BreakEndedAt);
                foreach (var activity in groupedActivities)
                {
                    activity.ReduceBy(endedBreak.Guid, breakPeriod);
                }
            }
            return groupedActivities.ToList().AsReadOnly();
        }

        private Task Handle(BreakRevokedEvent notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var breakToRemove = _breaks.FirstOrDefault(b => b.Guid == notification.RevokedBreak.BreakGuid);
            if (breakToRemove != null)
            {
                _breaks.TryTake(out _);
                logger.LogInformation($"Break {notification.RevokedBreak.BreakGuid} was revoked and removed from analysis.");
            }
            else
            {
                logger.LogWarning($"Attempted to revoke break {notification.RevokedBreak.BreakGuid} which was not found in the collection.");
            }
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
            _summaryStrategy?.Dispose();
        }

        public string GetCurrentStrategyName()
        {
            return _summaryStrategy?.GetType().Name ?? "Unknown";
        }

        public static IReadOnlyList<ISummaryStrategy> GetAvailableStrategies(IServiceProvider serviceProvider)
        {
            var strategies = new List<ISummaryStrategy>
            {
                serviceProvider.GetRequiredService<MLNetSummaryStrategy>(),
                serviceProvider.GetRequiredService<JiraKeySummaryStrategy>(),
                serviceProvider.GetRequiredService<JiraEnrichedSummaryStrategy>(),
                serviceProvider.GetRequiredService<HybridContextualSummaryStrategy>(),
                serviceProvider.GetRequiredService<TimeBasedSummaryStrategy>(),
                serviceProvider.GetRequiredService<DurationBasedSummaryStrategy>(),
                serviceProvider.GetRequiredService<ContextBasedSummaryStrategy>(),
                serviceProvider.GetRequiredService<ActivityNameSummaryStrategy>()
            };
            return strategies;
        }
    }
}
