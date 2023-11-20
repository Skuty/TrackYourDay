using TrackYourDay.Core;
using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities
{
    public static class ActivityFactory
    {
        public static StartedActivity StartedActivity(DateTime startDate, SystemState activityType)
        {
            return new StartedActivity(Guid.NewGuid(), startDate, activityType);
        }

        public static StartedActivity StartedFocusOnApplicatoinActivity(DateTime startDate)
        {
            return new StartedActivity(Guid.NewGuid(), startDate, SystemStateFactory.FocusOnApplicationState("Not recognized application"));
        }

        public static EndedActivity EndedFocusOnApplicationActivity(DateTime startDate, DateTime endDate)
        {
            return new EndedActivity(startDate, endDate, SystemStateFactory.FocusOnApplicationState("Not recognized application"));
        }

        public static StartedActivity StartedSystemLockedActivity(DateTime startDate)
        {
            return new StartedActivity(Guid.Empty, startDate, SystemStateFactory.SystemLockedState());
        }

        public static EndedActivity EndedSystemLockedActivity(DateTime startDate, DateTime endDate)
        {
            return new EndedActivity(startDate, endDate, SystemStateFactory.SystemLockedState());
        }

        public static InstantActivity MouseMovedActivity(DateTime occuranceDate)
        {
            return new InstantActivity(occuranceDate, SystemStateFactory.MouseMouvedEvent(0, 0));
        }
    }
}