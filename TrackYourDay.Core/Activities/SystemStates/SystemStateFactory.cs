namespace TrackYourDay.Core.Activities.SystemStates
{
    public static class SystemStateFactory
    {
        public static ApplicationStartedState ApplicationStartedActivityType(string applicationName)
        {
            return new ApplicationStartedState(applicationName);
        }

        public static FocusOnApplicationState FocusOnApplicationActivityType(string applicationWindowTitle)
        {
            return new FocusOnApplicationState(applicationWindowTitle);
        }

        public static SystemLockedState SystemLockedActivityType()
        {
            return new SystemLockedState();
        }

        public static MousePositionState MouseMovedActivityType(int xPosition, int yPosition)
        {
            return new MousePositionState(xPosition, yPosition);
        }
    }
}