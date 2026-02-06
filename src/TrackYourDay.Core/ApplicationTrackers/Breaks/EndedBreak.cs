namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public record class EndedBreak(Guid Guid, DateTime BreakStartedAt, DateTime BreakEndedAt, string BreakDescription)
    {
        /// <summary>
        /// Timestamp when the break was revoked. Null indicates the break is active (not revoked).
        /// </summary>
        public DateTime? RevokedAt { get; init; }

        public TimeSpan BreakDuration => BreakEndedAt - BreakStartedAt;

        public RevokedBreak Revoke(DateTime breakRevokedDate)
        {
            return new RevokedBreak(this, breakRevokedDate);
        }

        /// <summary>
        /// Creates a new EndedBreak instance marked as revoked with the specified timestamp.
        /// </summary>
        public EndedBreak MarkAsRevoked(DateTime revokedAt)
        {
            return this with { RevokedAt = revokedAt };
        }

        public static EndedBreak CreateSampleForDate(DateTime breakStartedAt, DateTime breakEndedAt)
        {
            return new EndedBreak(Guid.NewGuid(), breakStartedAt, breakEndedAt, "Sample Break");
        }
    }
}
