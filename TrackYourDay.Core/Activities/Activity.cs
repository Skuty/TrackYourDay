namespace TrackYourDay.Core.Activities
{
    internal interface IActivityToProcess
    {
        DateTime StartDate { get; }
        ActivityType ActivityType { get; }
    }
}
