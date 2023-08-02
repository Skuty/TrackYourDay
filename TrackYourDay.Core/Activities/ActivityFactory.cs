using TrackYourDay.Core;

namespace TrackYourDay.Core.Activities
{
    public static class ActivityFactory
    {
        public static StartedActivity StartedActivity(DateTime startDate, ActivityType activityType)
        {
            return new StartedActivity(startDate, activityType);
        }


        public static StartedActivity StartedFocusOnApplicatoinActivity(DateTime startDate)
        {
            return new StartedActivity(startDate, ActivityTypeFactory.FocusOnApplicationActivityType("Not recognized application"));
        }

        public static EndedActivity EndedFocusOnApplicationActivity(DateTime startDate, DateTime endDate)
        {
            return new EndedActivity(startDate, endDate, ActivityTypeFactory.FocusOnApplicationActivityType("Not recognized application"));
        }

        public static InstantActivity MouseMovedActivity(DateTime occuranceDate)
        {
            return new InstantActivity(occuranceDate, ActivityTypeFactory.MouseMovedActivityType(0, 0));
        }
    }

    public record class StartedActivity(DateTime StartDate, ActivityType ActivityType)
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