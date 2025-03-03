using MediatR;
using TrackYourDay.Core.ApplicationTrackers.Breaks;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks.Events
{
    public record class BreakRevokedEvent(RevokedBreak RevokedBreak) : INotification;
}
