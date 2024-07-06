using MediatR;

namespace TrackYourDay.Core.Breaks.Events
{
    public record class BreakStartedEvent(StartedBreak StartedBreak) : INotification;
}
