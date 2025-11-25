using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.Events;

namespace TrackYourDay.Core.Persistence.EventHandlers
{
    /// <summary>
    /// Handles PeriodicActivityEndedEvent by persisting the ended activity to the database.
    /// </summary>
    public class PersistEndedActivityHandler : INotificationHandler<PeriodicActivityEndedEvent>
    {
        private readonly IHistoricalDataRepository<EndedActivity> repository;
        private readonly ILogger<PersistEndedActivityHandler> logger;

        public PersistEndedActivityHandler(
            IHistoricalDataRepository<EndedActivity> repository,
            ILogger<PersistEndedActivityHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public Task Handle(PeriodicActivityEndedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                repository.Save(notification.EndedActivity);
                logger.LogDebug("Persisted ended activity with Guid: {ActivityGuid}", notification.EndedActivity.Guid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist ended activity with Guid: {ActivityGuid}", notification.EndedActivity.Guid);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
