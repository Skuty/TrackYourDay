using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.MAUI.Handlers;

/// <summary>
/// Handler for MeetingEndedEvent.
/// No UI notification - description is captured in MeetingEndConfirmation dialog.
/// </summary>
internal sealed class ShowUINotificationWhenMeetingEndedEventHandler : INotificationHandler<MeetingEndedEvent>
{
    public Task Handle(MeetingEndedEvent _event, CancellationToken cancellationToken)
    {
        // No-op: Description is now captured in the confirmation dialog
        // This handler is kept for potential future use (e.g., analytics, logging)
        return Task.CompletedTask;
    }
}
