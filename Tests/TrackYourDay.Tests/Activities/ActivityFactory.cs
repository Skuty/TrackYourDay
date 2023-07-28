using TrackYourDay.Core;

namespace TrackYourDay.Tests.Activities
{
    public static class ActivityFactory
    {
        public static StartedActivity StartedActivity(DateTime startDate)
        {
            return new StartedActivity(startDate);
        }

        public static EndedActivity EndedActivity(DateTime startDate, DateTime endDate)
        {
            return new EndedActivity(startDate, endDate);
        }

        public static InstantActivity InstantActivity(DateTime occuranceDate)
        {
            return new InstantActivity(occuranceDate);
        }
    }

    public record class StartedActivity(DateTime StartDate, ActivityType ActivityType)
    {
        public EndedActivity End(DateTime endDate)
        {
            return new EndedActivity(this.StartDate, endDate, this.ActivityType);
        }

        public TimeSpan GetDuration(IClock clock)
        {
            return clock.Now - StartDate;
        }
    }

    public record class EndedActivity(DateTime StartDate, DateTime EndDate, ActivityType ActivityType)
    {
        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }
    }

    public record class InstantActivity(DateTime OccuranceDate, ActivityType ActivityType)
    {
        public TimeSpan GetDuration()
        {
            return TimeSpan.Zero;
        }
    }
}