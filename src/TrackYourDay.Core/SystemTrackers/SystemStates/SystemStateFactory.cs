namespace TrackYourDay.Core.SystemTrackers.SystemStates
{
    public static class SystemStateFactory
    {
        public static ApplicationStartedState ApplicationStartedEvent(string applicationName)
        {
            return new ApplicationStartedState(applicationName);
        }

        public static FocusOnApplicationState FocusOnApplicationState(string applicationWindowTitle)
        {
            return new FocusOnApplicationState(applicationWindowTitle);
        }

        public static SystemLockedState SystemLockedState()
        {
            return new SystemLockedState();
        }

        public static MousePositionState MouseMouvedEvent(int xPosition, int yPosition)
        {
            return new MousePositionState(xPosition, yPosition);
        }

        public static LastInputState LastInputState(DateTime lastInputDate)
        {
            return new LastInputState(lastInputDate);
        }
    }
}