using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities
{
    public record class StartedActivity(Guid Guid, DateTime StartDate, SystemState ActivityType) : IActivityToProcess
    {
        public EndedActivity End(DateTime endDate)
        {
            return new EndedActivity(StartDate, endDate, ActivityType);
        }

        public TimeSpan GetDuration(IClock clock)
        {
            return clock.Now - StartDate;
        }
    }
}