namespace TrackYourDay.Core.Activities
{
    public record class MouseMovedActivityType : ActivityType
    {
        public MouseMovedActivityType(int XPosition, int YPosition) : base("Mouse moved")
        {
        }
    }
}