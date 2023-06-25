namespace TrackYourDay.Core.Breaks
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
}
