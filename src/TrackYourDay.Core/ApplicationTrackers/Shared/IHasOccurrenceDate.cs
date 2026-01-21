namespace TrackYourDay.Core.ApplicationTrackers.Shared
{
    /// <summary>
    /// Marker interface for activities with occurrence timestamp.
    /// </summary>
    public interface IHasOccurrenceDate
    {
        /// <summary>
        /// Gets the timestamp when the activity occurred.
        /// </summary>
        DateTime OccurrenceDate { get; }
    }
}
