namespace TrackYourDay.Core.Activities
{
    public static class ActivityTypeFactory
    {
        public static ApplicationStartedActivityType ApplicationStartedActivityType(string applicationName)
        {
            return new ApplicationStartedActivityType(applicationName);
        }

        public static FocusOnApplicationActivityType FocusOnApplicationActivityType(string applicationWindowTitle)
        {
            return new FocusOnApplicationActivityType(applicationWindowTitle);
        }

        public static SystemLockedActivityType SystemLockedActivityType()
        {
            return new SystemLockedActivityType();
        }

        public static MouseMovedActivityType MouseMovedActivityType(int xPosition, int yPosition)
        {
            return new MouseMovedActivityType(xPosition, yPosition);
        }
    }
}