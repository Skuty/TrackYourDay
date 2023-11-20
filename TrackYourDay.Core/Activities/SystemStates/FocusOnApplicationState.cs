namespace TrackYourDay.Core.Activities.SystemStates
{
    public record class FocusOnApplicationState : SystemState
    {
        public FocusOnApplicationState(string ApplicationWindowTitle) : base($"Focus on application - {ApplicationWindowTitle}")
        {
        }
    }
}