namespace TrackYourDay.Core.SystemTrackers.SystemStates
{
    public record class SystemLockedState : SystemState
    {
        public SystemLockedState() : base("System locked")
        {
        }
    }
}