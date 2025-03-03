namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public record class CanceledBreak(StartedBreak StartedBreak, DateTime CanceledAt);
}