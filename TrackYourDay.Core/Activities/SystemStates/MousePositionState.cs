namespace TrackYourDay.Core.Activities.SystemStates
{
    public record class MousePositionState : SystemState
    {
        public MousePositionState(int XPosition, int YPosition) : base("Mouse moved")
        {
        }
    }
}