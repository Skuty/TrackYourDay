using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;

namespace TrackYourDay.Core.Persistence.EventHandlers
{
    /// <summary>
    /// Handles BreakEndedEvent by persisting the ended break to the database.
    /// </summary>
    public class PersistEndedBreakHandler : INotificationHandler<BreakEndedEvent>
    {
        private readonly IHistoricalDataRepository<EndedBreak> repository;
        private readonly ILogger<PersistEndedBreakHandler> logger;

        public PersistEndedBreakHandler(
            IHistoricalDataRepository<EndedBreak> repository,
            ILogger<PersistEndedBreakHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public Task Handle(BreakEndedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                repository.Save(notification.EndedBreak);
                logger.LogDebug("Persisted ended break with Guid: {BreakGuid}", notification.EndedBreak.Guid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist ended break with Guid: {BreakGuid}", notification.EndedBreak.Guid);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
