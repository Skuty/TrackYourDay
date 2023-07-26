namespace TrackYourDay.Core.Old.Breaks
{
    public record class EndedBreak(DateTime BreakStartedOn, DateTime BreakEndedOn)
    {
        public TimeSpan BreakDuration => BreakEndedOn - BreakStartedOn;
    }
}
