using MediatR;

namespace TrackYourDay.Core.SystemTrackers.Events
{
    public record class InstantActivityOccuredEvent(Guid Guid, InstantActivity InstantActivity) : INotification;
}