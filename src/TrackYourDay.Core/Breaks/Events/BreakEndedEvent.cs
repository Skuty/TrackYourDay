using MediatR;

namespace TrackYourDay.Core.Breaks.Events
{
    public record class BreakEndedEvent(EndedBreak EndedBreak) : INotification;
}
