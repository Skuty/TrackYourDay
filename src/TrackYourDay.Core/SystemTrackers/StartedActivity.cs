using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers
{
    public record class StartedActivity(Guid Guid, DateTime StartDate, SystemState SystemState) : IActivityToProcess
    {
        public EndedActivity End(DateTime endDate)
        {
            return new EndedActivity(StartDate, endDate, SystemState);
        }

        public TimeSpan GetDuration(IClock clock)
        {
            return clock.Now - StartDate;
        }
    }
}