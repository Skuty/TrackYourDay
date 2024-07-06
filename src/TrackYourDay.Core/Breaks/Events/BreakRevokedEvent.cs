using MediatR;

namespace TrackYourDay.Core.Breaks.Events
{
    public record class BreakRevokedEvent(RevokedBreak RevokedBreak) : INotification;
}
