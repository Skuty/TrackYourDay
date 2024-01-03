namespace TrackYourDay.Core.Breaks
{
    public record class CanceledBreak(StartedBreak StartedBreak, DateTime CanceledAt);
}