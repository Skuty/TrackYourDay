﻿namespace TrackYourDay.Core.Breaks
{
    public record class EndedBreak(DateTime BreakStartedAt, DateTime BreakEndedAt)
    {
        public TimeSpan BreakDuration => this.BreakEndedAt - BreakStartedAt;
    }
}