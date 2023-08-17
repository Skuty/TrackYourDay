namespace TrackYourDay.Core.Activities
{
    public record class InstantActivity(DateTime OccuranceDate, ActivityType ActivityType)
    {
        public TimeSpan GetDuration()
        {
            return TimeSpan.Zero;
        }
    }
}