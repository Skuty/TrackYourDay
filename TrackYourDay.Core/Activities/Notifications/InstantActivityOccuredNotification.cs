using MediatR;

namespace TrackYourDay.Core.Activities.Notifications
{
    public record class InstantActivityOccuredNotification(Guid Guid, InstantActivity InstantActivity) : INotification;
}