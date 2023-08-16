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

        public static StartedActivity StartedSystemLockedActivity(DateTime startDate)
        {
            return new StartedActivity(startDate, ActivityTypeFactory.SystemLockedActivityType());
        }

        public static EndedActivity EndedSystemLockedActivity(DateTime startDate, DateTime endDate)
        {
            return new EndedActivity(startDate, endDate, ActivityTypeFactory.SystemLockedActivityType());
        }

        public static InstantActivity MouseMovedActivity(DateTime occuranceDate)
        {
            return new InstantActivity(occuranceDate, ActivityTypeFactory.MouseMovedActivityType(0, 0));
        }
    }
}