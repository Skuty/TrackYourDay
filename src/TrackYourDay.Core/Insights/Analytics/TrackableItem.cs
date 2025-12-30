namespace TrackYourDay.Core.Insights.Analytics
{
    /// <summary>
    /// Base class for all trackable items (activities, meetings, tasks).
    /// Provides common time-tracking functionality.
    /// </summary>
    public abstract class TrackableItem
    {
        public Guid Guid { get; init; }
        public DateTime StartDate { get; init; }
        
        /// <summary>
        /// End date of the item. Virtual to allow nullable override (e.g., ongoing tasks).
        /// </summary>
        public virtual DateTime EndDate { get; init; }
        
        /// <summary>
        /// Gets the human-readable description of this item.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract string GetDescription();
        
        /// <summary>
        /// Calculates the duration between StartDate and EndDate.
        /// Can be overridden for custom duration logic (e.g., ongoing tasks).
        /// </summary>
        public virtual TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }
        
        /// <summary>
        /// Gets the date portion of when this item occurred.
        /// Useful for grouping by date.
        /// </summary>
        public DateOnly GetDate() => DateOnly.FromDateTime(StartDate.Date);
        
        /// <summary>
        /// Checks if this item overlaps with another item in time.
        /// </summary>
        public bool OverlapsWith(TrackableItem other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return StartDate < other.EndDate && EndDate > other.StartDate;
        }
        
        /// <summary>
        /// Checks if this item occurred within a specific time period.
        /// </summary>
        public bool OccurredDuring(DateTime periodStart, DateTime periodEnd)
        {
            return StartDate < periodEnd && EndDate > periodStart;
        }
        
        protected TrackableItem()
        {
            Guid = Guid.NewGuid();
        }
        
        protected TrackableItem(Guid guid, DateTime startDate, DateTime endDate)
        {
            Guid = guid;
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
