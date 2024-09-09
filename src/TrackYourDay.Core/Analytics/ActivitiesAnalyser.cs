using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Breaks.Events;

namespace TrackYourDay.Core.Analytics
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
            this.groupedActivities = new List<GroupedActivity>();
        }

        public void Analyse(EndedActivity endedActivity)
        {
            var existingGroupedActivity = this.groupedActivities.FirstOrDefault(g => g.Description == endedActivity.GetDescription());
            if (existingGroupedActivity != null)
            {
                existingGroupedActivity.Include(endedActivity.Guid, new TimePeriod(endedActivity.StartDate, endedActivity.EndDate));
            }
            else
            {
                var newGroupedActivity = GroupedActivity.CreateEmptyWithDescriptionForDate(DateOnly.FromDateTime(clock.Now), endedActivity.GetDescription());
                this.groupedActivities.Add(newGroupedActivity);
            }
        }

        public void Analyse(EndedBreak endedBreak)
        {
            // TODO: Probably could be done better
            foreach (var groupedActivity in this.groupedActivities)
            {
                groupedActivity.ReduceBy(endedBreak.Guid, TimePeriod.CreateFrom(endedBreak.BreakStartedAt, endedBreak.BreakEndedAt));
            }
        }

        public IReadOnlyCollection<GroupedActivity> GetGroupedActivities()
        {
            return this.groupedActivities.ToImmutableArray();
        }

        private Task Handle(BreakRevokedEvent notification, CancellationToken cancellationToken)
        {
            this.logger.LogError("Break Revoked Event should be handled but there is no implementation for it.");

            //Remove it after tests
            throw new NotImplementedException("Sorry, BreakRevokedEvents are not handled by ActivitiesAnalyser");

            return Task.CompletedTask;
        }
    }
}
