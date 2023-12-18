using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities
{
    public record class EndedActivity(DateTime StartDate, DateTime EndDate, SystemState ActivityType)
    {
        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }
    }
}