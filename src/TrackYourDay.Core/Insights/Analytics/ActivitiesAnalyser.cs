using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    public class ActivitiesAnalyser 
    {
        private readonly ILogger<ActivitiesAnalyser> logger;
        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly IList<GroupedActivity> groupedActivities;
        // TODO: wait what happens and change to concurrent collections

        public ActivitiesAnalyser(IClock clock, IPublisher publisher, ILogger<ActivitiesAnalyser> logger)
        {
            this.clock = clock;
            this.publisher = publisher;
            this.logger = logger;
            groupedActivities = new List<GroupedActivity>();
        }

        public void Analyse(EndedActivity endedActivity)
        {
            var existingGroupedActivity = groupedActivities.FirstOrDefault(g => g.Description == endedActivity.GetDescription());
            if (existingGroupedActivity != null)
            {
                existingGroupedActivity.Include(endedActivity.Guid, new TimePeriod(endedActivity.StartDate, endedActivity.EndDate));
            }
            else
            {
                var newGroupedActivity = GroupedActivity.CreateEmptyWithDescriptionForDate(DateOnly.FromDateTime(clock.Now), endedActivity.GetDescription());
                groupedActivities.Add(newGroupedActivity);
            }
        }

        public void Analyse(EndedBreak endedBreak)
        {
            // TODO: Probably could be done better
            foreach (var groupedActivity in groupedActivities)
            {
                groupedActivity.ReduceBy(endedBreak.Guid, TimePeriod.CreateFrom(endedBreak.BreakStartedAt, endedBreak.BreakEndedAt));
            }
        }

        public IReadOnlyCollection<GroupedActivity> GetGroupedActivities()
        {
            return groupedActivities.ToImmutableArray();
        }

        private Task Handle(BreakRevokedEvent notification, CancellationToken cancellationToken)
        {
            logger.LogError("Break Revoked Event should be handled but there is no implementation for it.");

            //Remove it after tests
            throw new NotImplementedException("Sorry, BreakRevokedEvents are not handled by ActivitiesAnalyser");

            return Task.CompletedTask;
        }
    }
}
