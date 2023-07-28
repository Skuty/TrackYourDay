namespace TrackYourDay.Tests.Activities
{
    public abstract record class ActivityType(string ActivityDescription);

    public record class FocusOnApplicationActivityType : ActivityType
    {
        public FocusOnApplicationActivityType(string ApplicationWindowTitle) : base("Focus on application")
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