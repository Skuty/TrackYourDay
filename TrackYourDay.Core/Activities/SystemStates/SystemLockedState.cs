namespace TrackYourDay.Core.Activities.SystemStates
{
    public record class SystemLockedState : SystemState
    {
        public SystemLockedState() : base("System locked")
        {
        }
    }
}