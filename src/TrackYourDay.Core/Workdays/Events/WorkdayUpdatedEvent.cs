using MediatR;

namespace TrackYourDay.Core.Workdays.Events
{
    public record class WorkdayUpdatedEvent(Guid Guid, Workday Workday) : INotification;
}
