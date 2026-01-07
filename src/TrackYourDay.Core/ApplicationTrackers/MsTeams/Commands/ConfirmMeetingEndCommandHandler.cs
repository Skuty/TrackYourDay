using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams.Commands;

/// <summary>
/// Handles confirmation of a pending meeting end.
/// </summary>
public sealed class ConfirmMeetingEndCommandHandler : IRequestHandler<ConfirmMeetingEndCommand>
{
    private readonly IMeetingStateCache _stateCache;
    private readonly IPublisher _publisher;
    private readonly IClock _clock;
    private readonly ILogger<ConfirmMeetingEndCommandHandler> _logger;

    public ConfirmMeetingEndCommandHandler(
        IMeetingStateCache stateCache,
        IPublisher publisher,
        IClock clock,
        ILogger<ConfirmMeetingEndCommandHandler> logger)
    {
        _stateCache = stateCache;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(ConfirmMeetingEndCommand request, CancellationToken cancellationToken)
    {
        var pending = _stateCache.GetPendingEndMeeting();

        if (pending == null || pending.Meeting.Guid != request.MeetingGuid)
        {
            _logger.LogWarning("No pending meeting found for Guid: {MeetingGuid}", request.MeetingGuid);
            return;
        }

        var endedMeeting = pending.Meeting.End(_clock.Now);
        _stateCache.ClearMeetingState();

        await _publisher.Publish(new MeetingEndedEvent(Guid.NewGuid(), endedMeeting), cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Meeting confirmed ended: {MeetingTitle}", endedMeeting.Title);
    }
}
