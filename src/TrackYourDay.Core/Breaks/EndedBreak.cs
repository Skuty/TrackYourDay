﻿namespace TrackYourDay.Core.Breaks
{
    public record class EndedBreak(Guid Guid, DateTime BreakStartedAt, DateTime BreakEndedAt, string BreakDescription)
    {
        public TimeSpan BreakDuration => this.BreakEndedAt - BreakStartedAt;

        public RevokedBreak Revoke(DateTime breakRevokedDate)
        {
            return new RevokedBreak(this, breakRevokedDate);
        }

        public static EndedBreak CreateSampleForDate(DateTime breakStartedAt, DateTime breakEndedAt)
        {
            return new EndedBreak(Guid.NewGuid(), breakStartedAt, breakEndedAt, "Sample Break");
        }
    }
}
