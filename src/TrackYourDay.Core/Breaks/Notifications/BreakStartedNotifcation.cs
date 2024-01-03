using MediatR;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Breaks.Notifications
{
    public record class BreakStartedNotifcation(StartedBreak StartedBreak) : INotification;
}
