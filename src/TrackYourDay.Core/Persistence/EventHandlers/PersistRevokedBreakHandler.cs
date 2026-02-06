using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;

namespace TrackYourDay.Core.Persistence.EventHandlers
{
    /// <summary>
    /// Handles BreakRevokedEvent by updating the persisted break with RevokedAt timestamp.
    /// </summary>
    public class PersistRevokedBreakHandler : INotificationHandler<BreakRevokedEvent>
    {
        private readonly IHistoricalDataRepository<EndedBreak> repository;
        private readonly ILogger<PersistRevokedBreakHandler> logger;

        public PersistRevokedBreakHandler(
            IHistoricalDataRepository<EndedBreak> repository,
            ILogger<PersistRevokedBreakHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public Task Handle(BreakRevokedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var revokedBreak = notification.RevokedBreak;
                var updatedEndedBreak = revokedBreak.EndedBreak.MarkAsRevoked(revokedBreak.BreakRevokedAt);
                
                repository.Update(updatedEndedBreak);
                logger.LogDebug("Persisted revoked break with Guid: {BreakGuid}, RevokedAt: {RevokedAt}", 
                    revokedBreak.BreakGuid, revokedBreak.BreakRevokedAt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist revoked break with Guid: {BreakGuid}", 
                    notification.RevokedBreak.BreakGuid);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
