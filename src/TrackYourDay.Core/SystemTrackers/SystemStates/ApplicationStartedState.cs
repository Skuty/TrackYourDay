namespace TrackYourDay.Core.SystemTrackers.SystemStates
{
    public record class ApplicationStartedState : SystemState
    {
        public ApplicationStartedState(string ApplicationName) : base($"Application started - {ApplicationName}")
        {
        }
    }
}