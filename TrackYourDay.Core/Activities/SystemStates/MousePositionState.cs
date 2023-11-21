namespace TrackYourDay.Core.Activities.SystemStates
{
    public sealed record class MousePositionState : SystemState
    {
        public MousePositionState(int XPosition, int YPosition) : base($"Mouse position X:{XPosition}, Y:{YPosition}")
        {
        }
    }
}