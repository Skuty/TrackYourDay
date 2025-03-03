namespace TrackYourDay.Core.SystemTrackers.SystemStates
{
    public record class FocusOnApplicationState : SystemState
    {
        public FocusOnApplicationState(string ApplicationWindowTitle) : base($"Focus on application - {ApplicationWindowTitle}")
        {
        }
    }
}