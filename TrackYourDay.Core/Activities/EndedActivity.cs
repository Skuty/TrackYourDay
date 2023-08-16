namespace TrackYourDay.Core.Activities
{
    public record class EndedActivity(DateTime StartDate, DateTime EndDate, ActivityType ActivityType)
    {
        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }
    }
}