using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Breaks
{
    public record class StartedBreak(DateTime BreakStartedAt, string BreakDescription)
    {
        public Guid BreakGuid { get; } = Guid.NewGuid();

        public EndedBreak EndBreak(DateTime breakEndedAt)
        {
            if (breakEndedAt < this.BreakStartedAt)
            {
                throw new ArgumentException("Break cannot end before it started", nameof(breakEndedAt));
            }

            if (breakEndedAt.Date != this.BreakStartedAt.Date)
            {
                throw new ArgumentException("Break cannot end on different day", nameof(breakEndedAt));
            }


            return new EndedBreak(this.BreakGuid, this.BreakStartedAt, breakEndedAt, this.BreakDescription);
        }
    }
}
