using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.PublicEvents;

namespace TrackYourDay.Core.Persistence.EventHandlers
{
    /// <summary>
    /// Handles MeetingEndedEvent by persisting the ended meeting to the database.
    /// </summary>
    public class PersistEndedMeetingHandler : INotificationHandler<MeetingEndedEvent>
    {
        private readonly IHistoricalDataRepository<EndedMeeting> repository;
        private readonly ILogger<PersistEndedMeetingHandler> logger;

        public PersistEndedMeetingHandler(
            IHistoricalDataRepository<EndedMeeting> repository,
            ILogger<PersistEndedMeetingHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public Task Handle(MeetingEndedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                repository.Save(notification.EndedMeeting);
                logger.LogDebug("Persisted ended meeting with Guid: {MeetingGuid}", notification.EndedMeeting.Guid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist ended meeting with Guid: {MeetingGuid}", notification.EndedMeeting.Guid);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
