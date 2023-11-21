namespace TrackYourDay.Core.Breaks
{
    public record class EndedBreak(DateTime BreakStartedAt, DateTime BreakEndedAt, string BreakDescription)
    {
        public TimeSpan BreakDuration => this.BreakEndedAt - BreakStartedAt;
    }
}
