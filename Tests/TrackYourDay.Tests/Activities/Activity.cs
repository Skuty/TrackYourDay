using TrackYourDay.Core;

namespace TrackYourDay.Tests.Activities
{
    public record class StartedActivity(DateTime StartDate)
    {
        public static StartedActivity Start(DateTime startDate)
        {
            return new StartedActivity(startDate);
        }

        public EndedActivity End(DateTime endDate)
        {
            return new EndedActivity(this.StartDate, endDate);
        }

        public virtual TimeSpan GetDuration(IClock clock)
        {
            return clock.Now - StartDate;
        }
    }

    public record class EndedActivity(DateTime StartDate, DateTime EndDate)
    {
        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }
    }
}