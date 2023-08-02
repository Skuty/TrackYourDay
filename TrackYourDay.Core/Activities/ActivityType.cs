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

    public abstract record class ActivityType(string ActivityDescription);

    public record class ApplicationStartedActivityType : ActivityType
    {
        public ApplicationStartedActivityType(string ApplicationName) : base($"Application started - {ApplicationName}")
        {
        }
    }

    public record class FocusOnApplicationActivityType : ActivityType
    {
        public FocusOnApplicationActivityType(string ApplicationWindowTitle) : base($"Focus on application - {ApplicationWindowTitle}")
        {
        }
    }

    public record class SystemLockedActivityType : ActivityType
    {
        public SystemLockedActivityType() : base("System locked")
        {
        }
    }

    public record class MouseMovedActivityType : ActivityType
    {
        public MouseMovedActivityType(int XPosition, int YPosition) : base("Mouse moved")
        {
        }
    }
}