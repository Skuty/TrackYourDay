namespace TrackYourDay.Core.SystemTrackers.SystemStates
{
    public sealed record class LastInputState : SystemState
    {
        public LastInputState(DateTime lastInputDate) : base($"Last input occured at :{lastInputDate}")
        {
        }
    }
}
