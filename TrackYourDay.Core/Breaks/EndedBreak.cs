namespace TrackYourDay.Core.Breaks
{
    public record class EndedBreak(Guid BreakGuid, DateTime BreakStartedAt, DateTime BreakEndedAt, string BreakDescription)
    {
        public TimeSpan BreakDuration => this.BreakEndedAt - BreakStartedAt;

        public RevokedBreak Revoke(DateTime breakRevokedDate)
        {
            return new RevokedBreak(this, breakRevokedDate);
        }
    }
}
