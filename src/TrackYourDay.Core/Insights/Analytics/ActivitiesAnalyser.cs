using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;
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
        private readonly SummaryGenerator _summaryGenerator;

        public ActivitiesAnalyser(IClock clock, IPublisher publisher, ILogger<ActivitiesAnalyser> logger, ILogger<SummaryGenerator> summaryGeneratorLogger)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _summaryGenerator = new SummaryGenerator(clock, summaryGeneratorLogger);
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

        public IReadOnlyCollection<GroupedActivity> GetGroupedActivities()
        {
            // First, get the grouped activities from the summary generator
            var groupedActivities = _summaryGenerator.Generate(_activities.ToList());
            
            // Apply breaks to the grouped activities
            foreach (var endedBreak in _breaks)
            {
                var breakPeriod = TimePeriod.CreateFrom(endedBreak.BreakStartedAt, endedBreak.BreakEndedAt);
                foreach (var activity in groupedActivities)
                {
                    // This is a simplified approach - in a real implementation, you'd want to 
                    // properly handle the time period overlaps with more sophisticated logic
                    activity.ReduceBy(endedBreak.Guid, breakPeriod);
                }
            }
            
            return groupedActivities.ToList().AsReadOnly();
        }

        private Task Handle(BreakRevokedEvent notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            
            // Remove the break from our collection if it exists
            var breakToRemove = _breaks.FirstOrDefault(b => b.Guid == notification.BreakId);
            if (breakToRemove != null)
            {
                _breaks.TryTake(out _);
                logger.LogInformation($"Break {notification.BreakId} was revoked and removed from analysis.");
            }
            else
            {
                logger.LogWarning($"Attempted to revoke break {notification.BreakId} which was not found in the collection.");
            }
            
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
            _summaryGenerator?.Dispose();
        }
    }
}
