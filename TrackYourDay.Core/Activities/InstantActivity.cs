namespace TrackYourDay.Core.Activities
{
    public record class InstantActivity(DateTime StartDate, ActivityType ActivityType) : IActivityToProcess
    {
        public TimeSpan GetDuration()
        {
            return TimeSpan.Zero;
        }
    }
}