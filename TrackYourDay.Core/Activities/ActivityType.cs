namespace TrackYourDay.Core.Activities
{
    public static class ActivityTypeFactory
    {
        public static FocusOnApplicationActivityType FocusOnApplicationActivityType(string applicationWindowTitle)
        {
            return new FocusOnApplicationActivityType(applicationWindowTitle);
        }

        public static MouseMovedActivityType MouseMovedActivityType(int xPosition, int yPosition)
        {
            return new MouseMovedActivityType(xPosition, yPosition);
        }

        public static SystemLockedActivityType SystemLockedActivityType()
        {
            return new SystemLockedActivityType();
        }
    }

    public abstract record class ActivityType(string ActivityDescription);

    public record class FocusOnApplicationActivityType : ActivityType
    {
        public FocusOnApplicationActivityType(string ApplicationWindowTitle) : base($"Focus on application - {ApplicationWindowTitle}")
        {
        }
    }

    public record class MouseMovedActivityType : ActivityType
    {
        public MouseMovedActivityType(int XPosition, int YPosition) : base("Mouse moved")
        {
        }
    }

    public record class SystemLockedActivityType : ActivityType
    {
        public SystemLockedActivityType() : base("System locked")
        {
        }
    }
}