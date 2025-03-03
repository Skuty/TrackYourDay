namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public record class StartedBreak(DateTime BreakStartedAt, string BreakDescription)
    {
        public Guid BreakGuid { get; } = Guid.NewGuid();

        public EndedBreak EndBreak(DateTime breakEndedAt)
        {
            if (breakEndedAt < BreakStartedAt)
            {
                throw new ArgumentException("Break cannot end before it started", nameof(breakEndedAt));
            }

            if (breakEndedAt.Date != BreakStartedAt.Date)
            {
                throw new ArgumentException("Break cannot end on different day", nameof(breakEndedAt));
            }


            return new EndedBreak(BreakGuid, BreakStartedAt, breakEndedAt, BreakDescription);
        }
    }
}
