using MediatR;
using TrackYourDay.Core.Insights.Workdays;

namespace TrackYourDay.Core.Insights.Workdays.Events
{
    public record class WorkdayUpdatedEvent(Guid Guid, Workday Workday) : INotification;
}
