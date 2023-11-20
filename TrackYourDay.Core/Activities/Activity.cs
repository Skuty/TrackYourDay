using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities
{
    internal interface IActivityToProcess
    {
        Guid Guid { get; }

        DateTime StartDate { get; }
        
        SystemState SystemState { get; }
    }
}
