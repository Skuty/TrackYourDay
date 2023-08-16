namespace TrackYourDay.Core.Activities
{
    public record class StartedActivity(DateTime StartDate, ActivityType ActivityType) : IActivityToProcess
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