using MediatR;

namespace TrackYourDay.Core.Activities.Events
{
    public record class InstantActivityOccuredEvent(Guid Guid, InstantActivity InstantActivity) : INotification;
}