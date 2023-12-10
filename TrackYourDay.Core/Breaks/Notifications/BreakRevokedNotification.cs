using MediatR;

namespace TrackYourDay.Core.Breaks.Notifications
{
    public record class BreakRevokedNotification(RevokedBreak RevokedBreak) : INotification;
}
