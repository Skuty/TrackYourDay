using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers
{
    internal interface IActivityToProcess
    {
        Guid Guid { get; }

        DateTime StartDate { get; }
        
        SystemState SystemState { get; }
    }
}
