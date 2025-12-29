namespace TrackYourDay.Core.Insights.Analytics
{
    public interface ITrackableItem
    {
        Guid Guid { get; }
        DateTime StartDate { get; }
        DateTime EndDate { get; }
        string GetDescription();
    }
}
