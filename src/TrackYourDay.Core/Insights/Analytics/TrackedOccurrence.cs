namespace TrackYourDay.Core.Insights.Analytics
{
    /// <summary>
    /// Immutable value object representing a single occurrence of a tracked event.
    /// Used to correlate event identifiers with their time periods in grouped activities.
    /// </summary>
    public sealed record TrackedOccurrence
    {
        /// <summary>
        /// Unique identifier of the source event (activity, meeting, task, break).
        /// </summary>
        public Guid EventId { get; init; }

        /// <summary>
        /// Time period during which the event occurred.
        /// </summary>
        public TimePeriod Period { get; init; }

        public TrackedOccurrence(Guid eventId, TimePeriod period)
        {
            if (eventId == Guid.Empty)
                throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));

            EventId = eventId;
            Period = period ?? throw new ArgumentNullException(nameof(period));
        }

        /// <summary>
        /// Checks if this occurrence overlaps with another in time.
        /// </summary>
        public bool OverlapsWith(TrackedOccurrence other)
        {
            ArgumentNullException.ThrowIfNull(other);
            return Period.IsOverlappingWith(other.Period);
        }

        /// <summary>
        /// Checks if this occurrence happened during a specific time range.
        /// </summary>
        public bool OccurredDuring(DateTime start, DateTime end)
        {
            return Period.StartDate < end && Period.EndDate > start;
        }

        public override string ToString()
        {
            return $"Event {EventId:N} ({Period.StartDate:HH:mm}-{Period.EndDate:HH:mm})";
        }
    }
}
