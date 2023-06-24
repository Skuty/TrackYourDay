namespace TrackYourDay.Core.Tasks
{
    public record class StartedBreak(DateTime BreakStartedOn)

    {
        public EndedBreak EndBreak(DateTime breakEndedOn)
        {
            if (breakEndedOn < this.BreakStartedOn)
            {
                throw new ArgumentException("Break cannot end before it started", nameof(breakEndedOn));
            }

            if (breakEndedOn.Date != this.BreakStartedOn.Date)
            {
                throw new ArgumentException("Break cannot end on different day", nameof(breakEndedOn));
            }


            return new EndedBreak(BreakStartedOn, breakEndedOn);
        }
    }

    public record class EndedBreak(DateTime BreakStartedOn, DateTime BreakEndedOn)
    {
        public TimeSpan BreakDuration => BreakEndedOn - BreakStartedOn;
    }
}
