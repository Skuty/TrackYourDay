namespace TrackYourDay.Core.Activities
{
    internal interface IActivityToProcess
    {
        Guid Guid { get; }

        DateTime StartDate { get; }
        
        ActivityType ActivityType { get; }
    }
}
