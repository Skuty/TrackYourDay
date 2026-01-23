using MediatR;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.MAUI.Handlers;

/// <summary>
/// Handles manual meeting end request from UI by delegating to existing confirmation flow.
/// </summary>
internal sealed class ShowManualMeetingEndDialogHandler 
    : INotificationHandler<RequestManualMeetingEndEvent>
{
    private readonly MsTeamsMeetingTracker _meetingTracker;
    private readonly IPublisher _publisher;

    public ShowManualMeetingEndDialogHandler(
        MsTeamsMeetingTracker meetingTracker,
        IPublisher publisher)
    {
        _meetingTracker = meetingTracker;
        _publisher = publisher;
    }

    public async Task Handle(RequestManualMeetingEndEvent notification, CancellationToken cancellationToken)
    {
        var ongoingMeeting = _meetingTracker.GetOngoingMeeting();
        
        if (ongoingMeeting == null)
        {
            return;
        }

        // Reuse existing confirmation dialog handler
        await _publisher.Publish(
            new MeetingEndConfirmationRequestedEvent(ongoingMeeting.Guid, ongoingMeeting.Title),
            cancellationToken);
    }
}
